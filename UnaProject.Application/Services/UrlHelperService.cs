using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using UnaProject.Application.Services.Interfaces;

namespace UnaProject.Application.Services
{
    public class UrlHelperService : IUrlHelperService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public UrlHelperService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public string GenerateImageUrl(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return string.Empty;

            if (relativePath.StartsWith("http"))
                return relativePath;

            // Normalize the path by removing leading bars to standardize it
            relativePath = relativePath.TrimStart('/');

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                var baseUrl = $"{request.Scheme}://{request.Host}";
                return $"{baseUrl}/imagens/{relativePath}";
            }

            // Fallback for when there is no HTTP context
            // Use the appsettings configuration or environment variable
            var fallbackBaseUrl = _configuration["FileStorage:BaseUrl"] ?? "https://procksuplementos.com.br";

            // Remove trailing slash from base URL if present
            fallbackBaseUrl = fallbackBaseUrl.TrimEnd('/');

            return $"{fallbackBaseUrl}/imagens/{relativePath}";
        }
    }
}
