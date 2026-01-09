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
        /// It processes authentication via Google OAuth.
        /// </summary>
        /// <param name="request">Social user data for authentication</param>
        /// <returns>Response with JWT token and user data</returns>
        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] SocialAuthRequest request)
        {
            try
            {
                _logger.LogInformation("Starting authentication via Google for provider: {Provider}", request.SocialUser.Provider);

                if (request.SocialUser.Provider?.ToLower() != "google")
                {
                    _logger.LogWarning("Invalid provider received: {Provider}", request.SocialUser.Provider);
                    return BadRequest(new { Message = "Provider must be 'google'" });
                }

                var command = new ProcessSocialAuthCommand(request.SocialUser);
                var result = await _mediator.Send(command);

                if (result.HasSuccess)
                {
                    _logger.LogInformation("Google authentication successful for user: {Email}", request.SocialUser.Email);
                    return Ok(result.Value);
                }

                _logger.LogWarning("Google authentication failed: {Error}", result.ErrorMessage);
                return BadRequest(new { Message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google authentication");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Process authentication via Facebook OAuth
        /// </summary>
        /// <param name="request">Social user data for authentication</param>
        /// <returns>Response with JWT token and user data</returns>
        [HttpPost("facebook")]
        [AllowAnonymous]
        public async Task<IActionResult> FacebookLogin([FromBody] SocialAuthRequest request)
        {
            try
            {
                _logger.LogInformation("Starting authentication via Facebook for provider: {Provider}", request.SocialUser.Provider);

                if (request.SocialUser.Provider?.ToLower() != "facebook")
                {
                    _logger.LogWarning("Invalid provider received: {Provider}", request.SocialUser.Provider);
                    return BadRequest(new { Message = "Provider must be 'facebook'" });
                }

                var command = new ProcessSocialAuthCommand(request.SocialUser);
                var result = await _mediator.Send(command);

                if (result.HasSuccess)
                {
                    _logger.LogInformation("Facebook authentication successful for user: {Email}", request.SocialUser.Email);
                    return Ok(result.Value);
                }

                _logger.LogWarning("Facebook authentication failed: {Error}", result.ErrorMessage);
                return BadRequest(new { Message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Facebook authentication");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        /// <summary>
        /// Callback for Google authentication (optional for web flow)
        /// </summary>
        /// <returns>Redirect to frontend with tokens</returns>
        [HttpGet("google/callback")]
        [AllowAnonymous]
        public IActionResult GoogleCallback()
        {
            // This endpoint can be used for traditional web flow
            // For now, we return information about the mobile/SPA flow
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
        /// <returns>Redirect to frontend with tokens</returns>
        [HttpGet("facebook/callback")]
        [AllowAnonymous]
        public IActionResult FacebookCallback()
        {
            // This endpoint can be used for traditional web flow
            // For now, we return information about the mobile/SPA flow
            return Ok(new
            {
                Message = "Use the POST /api/socialauth/facebook endpoint for token-based authentication",
                FlowType = "Mobile/SPA",
                Documentation = "Send the access_token obtained from the Facebook OAuth SDK"
            });
        }

        /// <summary>
        /// Links a social account to an existing user
        /// </summary>
        /// <param name="request">Social account data to link</param>
        /// <returns>Confirmation of the link</returns>
        [HttpPost("link")]
        [Authorize]
        public async Task<IActionResult> LinkSocialAccount([FromBody] SocialAuthRequest request)
        {
            try
            {
                _logger.LogInformation("Linking social account {Provider} to authenticated user", request.SocialUser.Provider);
                // Get the current user's ID from the JWT token
                var userIdClaim = User.FindFirst("Id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Invalid user ID in token: {UserIdClaim}", userIdClaim);
                    return Unauthorized(new { Message = "Invalid user token" });
                }

                var command = new ProcessSocialAuthCommand(request.SocialUser);
                var result = await _mediator.Send(command);

                if (result.HasSuccess)
                {
                    _logger.LogInformation("Social account {Provider} linked successfully to user {UserId}", request.SocialUser.Provider, userId);
                    return Ok(new { Message = $"Social account {request.SocialUser.Provider} linked successfully", User = result.Value?.User });
                }

                _logger.LogWarning("Failed to link social account {Provider}: {Error}", request.SocialUser.Provider, result.ErrorMessage);
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