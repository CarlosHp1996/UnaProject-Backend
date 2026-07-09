using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Models.Requests.Security;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Services
{
    /// <summary>
    /// Validação server-side de tokens de login social. Faz chamadas HTTP ao provedor
    /// para confirmar a autenticidade do accessToken e extrair o perfil real do usuário.
    /// </summary>
    public class SocialTokenValidator : ISocialTokenValidator
    {
        private const string GoogleUserInfoUrl = "https://www.googleapis.com/oauth2/v3/userinfo";
        private const string FacebookGraphBaseUrl = "https://graph.facebook.com";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SocialTokenValidator> _logger;

        public SocialTokenValidator(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<SocialTokenValidator> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ResultValue<SocialUserInfo>> ValidateAsync(string provider, string accessToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                var result = new ResultValue<SocialUserInfo>();
                result.WithError("Access token is required.");
                return result;
            }

            return (provider ?? string.Empty).ToLowerInvariant() switch
            {
                "google" => await ValidateGoogleAsync(accessToken, cancellationToken),
                "facebook" => await ValidateFacebookAsync(accessToken, cancellationToken),
                _ => Fail("Unsupported social provider.")
            };
        }

        private async Task<ResultValue<SocialUserInfo>> ValidateGoogleAsync(string accessToken, CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, GoogleUserInfoUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                using var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Google token validation failed with status {StatusCode}", (int)response.StatusCode);
                    return Fail("Invalid Google access token.");
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var sub = GetString(root, "sub");
                var email = GetString(root, "email");
                var emailVerified = GetBool(root, "email_verified");

                if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Google userinfo missing required fields (sub/email).");
                    return Fail("Invalid Google access token.");
                }

                if (emailVerified != true)
                {
                    _logger.LogWarning("Google account email is not verified for sub {Sub}", sub);
                    return Fail("Google account email is not verified.");
                }

                var user = new SocialUserInfo
                {
                    Provider = "google",
                    ProviderId = sub,
                    Email = email,
                    Name = GetString(root, "name") ?? string.Empty,
                    Picture = GetString(root, "picture")
                };

                return ResultValue<SocialUserInfo>.Success(user);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "Network error validating Google access token.");
                return Fail("Could not validate Google access token. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error validating Google access token.");
                return Fail("Could not validate Google access token.");
            }
        }

        private async Task<ResultValue<SocialUserInfo>> ValidateFacebookAsync(string accessToken, CancellationToken cancellationToken)
        {
            var appId = _configuration["Authentication:Facebook:AppId"];
            var appSecret = _configuration["Authentication:Facebook:AppSecret"];

            if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret))
            {
                _logger.LogError("Facebook AppId/AppSecret not configured; cannot validate token.");
                return Fail("Facebook login is not configured.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                // 1) Debug do token: confirma validade e que pertence ao NOSSO app (impede token de outro app).
                var appAccessToken = $"{appId}|{appSecret}";
                var debugUrl = $"{FacebookGraphBaseUrl}/debug_token?input_token={Uri.EscapeDataString(accessToken)}&access_token={Uri.EscapeDataString(appAccessToken)}";

                using var debugResponse = await client.GetAsync(debugUrl, cancellationToken);
                if (!debugResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Facebook debug_token failed with status {StatusCode}", (int)debugResponse.StatusCode);
                    return Fail("Invalid Facebook access token.");
                }

                var debugBody = await debugResponse.Content.ReadAsStringAsync(cancellationToken);
                using var debugDoc = JsonDocument.Parse(debugBody);
                if (!debugDoc.RootElement.TryGetProperty("data", out var data))
                {
                    _logger.LogWarning("Facebook debug_token response missing 'data'.");
                    return Fail("Invalid Facebook access token.");
                }

                var isValid = GetBool(data, "is_valid") == true;
                var tokenAppId = GetString(data, "app_id");

                if (!isValid || !string.Equals(tokenAppId, appId, StringComparison.Ordinal))
                {
                    _logger.LogWarning("Facebook token invalid or belongs to a different app. is_valid={IsValid}", isValid);
                    return Fail("Invalid Facebook access token.");
                }

                // 2) Perfil do usuário (id/name/email) derivado do token validado.
                var meUrl = $"{FacebookGraphBaseUrl}/me?fields=id,name,email&access_token={Uri.EscapeDataString(accessToken)}";
                using var meResponse = await client.GetAsync(meUrl, cancellationToken);
                if (!meResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Facebook /me failed with status {StatusCode}", (int)meResponse.StatusCode);
                    return Fail("Invalid Facebook access token.");
                }

                var meBody = await meResponse.Content.ReadAsStringAsync(cancellationToken);
                using var meDoc = JsonDocument.Parse(meBody);
                var meRoot = meDoc.RootElement;

                var id = GetString(meRoot, "id");
                var email = GetString(meRoot, "email");

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Facebook /me response missing 'id'.");
                    return Fail("Invalid Facebook access token.");
                }

                if (string.IsNullOrEmpty(email))
                {
                    // Sem email não é possível associar/criar conta com segurança.
                    _logger.LogWarning("Facebook account did not return an email for id {Id}", id);
                    return Fail("Facebook account has no email available.");
                }

                var user = new SocialUserInfo
                {
                    Provider = "facebook",
                    ProviderId = id,
                    Email = email,
                    Name = GetString(meRoot, "name") ?? string.Empty,
                    Picture = null
                };

                return ResultValue<SocialUserInfo>.Success(user);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "Network error validating Facebook access token.");
                return Fail("Could not validate Facebook access token. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error validating Facebook access token.");
                return Fail("Could not validate Facebook access token.");
            }
        }

        private static ResultValue<SocialUserInfo> Fail(string message)
        {
            var result = new ResultValue<SocialUserInfo>();
            result.WithError(message);
            return result;
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
            return null;
        }

        private static bool? GetBool(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var prop))
                return null;

            return prop.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                // Google/Facebook às vezes serializam booleanos como string ("true"/"false").
                JsonValueKind.String => bool.TryParse(prop.GetString(), out var b) ? b : (bool?)null,
                _ => null
            };
        }
    }
}
