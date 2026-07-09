using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using UnaProject.Application.Commands.Security;
using UnaProject.Application.Models.Requests.Security;

namespace UnaProject.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SocialAuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SocialAuthController> _logger;

        public SocialAuthController(IMediator mediator, ILogger<SocialAuthController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Processes authentication via Google OAuth. The client sends only the accessToken;
        /// the profile is derived by validating the token server-side.
        /// </summary>
        [HttpPost("google")]
        [AllowAnonymous]
        public Task<IActionResult> GoogleLogin([FromBody] SocialAuthRequest request)
            => ProcessLogin("google", request);

        /// <summary>
        /// Processes authentication via Facebook OAuth. The client sends only the accessToken;
        /// the profile is derived by validating the token server-side.
        /// </summary>
        [HttpPost("facebook")]
        [AllowAnonymous]
        public Task<IActionResult> FacebookLogin([FromBody] SocialAuthRequest request)
            => ProcessLogin("facebook", request);

        private async Task<IActionResult> ProcessLogin(string provider, SocialAuthRequest request)
        {
            try
            {
                _logger.LogInformation("Starting social authentication for provider: {Provider}", provider);

                if (request == null || string.IsNullOrWhiteSpace(request.AccessToken))
                {
                    return BadRequest(new { Message = "accessToken is required." });
                }

                var command = new ProcessSocialAuthCommand(provider, request.AccessToken);
                var result = await _mediator.Send(command);

                if (result.HasSuccess)
                {
                    _logger.LogInformation("{Provider} authentication successful", provider);
                    return Ok(result.Value);
                }

                _logger.LogWarning("{Provider} authentication failed: {Error}", provider, result.ErrorMessage);
                return BadRequest(new { Message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {Provider} authentication", provider);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Callback for Google authentication (optional for web flow)
        /// </summary>
        [HttpGet("google/callback")]
        [AllowAnonymous]
        public IActionResult GoogleCallback()
        {
            return Ok(new
            {
                Message = "Use the POST /api/socialauth/google endpoint for token-based authentication",
                FlowType = "Mobile/SPA",
                Documentation = "Send the access_token obtained from the Google OAuth SDK"
            });
        }

        /// <summary>
        /// Callback for Facebook authentication (optional for web flow)
        /// </summary>
        [HttpGet("facebook/callback")]
        [AllowAnonymous]
        public IActionResult FacebookCallback()
        {
            return Ok(new
            {
                Message = "Use the POST /api/socialauth/facebook endpoint for token-based authentication",
                FlowType = "Mobile/SPA",
                Documentation = "Send the access_token obtained from the Facebook OAuth SDK"
            });
        }

        /// <summary>
        /// Links a social account to the authenticated user. The provider comes from the route
        /// and the accessToken is validated server-side before linking.
        /// </summary>
        [HttpPost("link/{provider}")]
        [Authorize]
        public async Task<IActionResult> LinkSocialAccount(string provider, [FromBody] SocialAuthRequest request)
        {
            try
            {
                _logger.LogInformation("Linking social account {Provider} to authenticated user", provider);

                var userIdClaim = User.FindFirst("Id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Invalid user ID in token");
                    return Unauthorized(new { Message = "Invalid user token" });
                }

                if (request == null || string.IsNullOrWhiteSpace(request.AccessToken))
                {
                    return BadRequest(new { Message = "accessToken is required." });
                }

                var command = new ProcessSocialAuthCommand(provider, request.AccessToken);
                var result = await _mediator.Send(command);

                if (result.HasSuccess)
                {
                    _logger.LogInformation("Social account {Provider} linked successfully to user {UserId}", provider, userId);
                    return Ok(new { Message = $"Social account {provider} linked successfully", User = result.Value?.User });
                }

                _logger.LogWarning("Failed to link social account {Provider}: {Error}", provider, result.ErrorMessage);
                return BadRequest(new { Message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during linking social account");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }
    }
}
