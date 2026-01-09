using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Requests.Security;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Entities.Security;
using UnaProject.Domain.Security;

namespace UnaProject.Application.Services
{
    public class SocialAuthService : ISocialAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AccessManager _accessManager;
        private readonly ILogger<SocialAuthService> _logger;

        public SocialAuthService(
            IUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            AccessManager accessManager,
            ILogger<SocialAuthService> logger)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _accessManager = accessManager;
            _logger = logger;
        }

        public async Task<SocialAuthResponse> ProcessSocialUserAsync(SocialUserInfo socialUser)
        {
            try
            {
                _logger.LogInformation("Processing social authentication for provider.: {Provider}, ProviderId: {ProviderId}",
                    socialUser.Provider, socialUser.ProviderId);

                // 1. Check if user exists using social ID.
                var existingUserBySocial = await _userRepository.GetBySocialIdAsync(socialUser.Provider, socialUser.ProviderId);

                ApplicationUser user;
                bool isNewUser = false;

                if (existingUserBySocial != null)
                {
                    // User already exists with this social ID
                    user = existingUserBySocial;
                    await UpdateSocialLoginAsync(user, socialUser);
                    _logger.LogInformation("Existing user found by social ID: {UserId}", user.Id);
                }
                else
                {
                    // 2. Check if user exists by email
                    var existingUserByEmail = await _userManager.FindByEmailAsync(socialUser.Email);

                    if (existingUserByEmail != null)
                    {
                        // Link social account to existing user
                        user = await LinkSocialAccountAsync(existingUserByEmail, socialUser);
                        _logger.LogInformation("Social account linked to existing user: {UserId}", user.Id);
                    }
                    else
                    {
                        // 3. Create new user
                        user = await CreateUserFromSocialAsync(socialUser);
                        isNewUser = true;
                        _logger.LogInformation("New user created: {UserId}", user.Id);
                    }
                }

                // 4. Generate JWT token
                var jwtToken = await _accessManager.GenerateToken(user);

                // 5. Return response
                var response = new SocialAuthResponse
                {
                    JwtToken = jwtToken,
                    Expiration = DateTime.Now.AddHours(1),
                    IsNewUser = isNewUser,
                    Message = isNewUser ? "Account successfully created via " + socialUser.Provider : "Login successful",
                    User = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName ?? "",
                        Email = user.Email ?? "",
                        ProfilePicture = user.ProfilePicture
                    }
                };

                _logger.LogInformation("Social authentication completed successfully for user: {UserId}", user.Id);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during social authentication processing");
                throw;
            }
        }

        public async Task<ApplicationUser> CreateUserFromSocialAsync(SocialUserInfo socialUser)
        {
            try
            {
                var user = new ApplicationUser
                {
                    UserName = GenerateUserNameFromEmail(socialUser.Email),
                    Email = socialUser.Email,
                    EmailConfirmed = true,
                    EmailVerified = true,
                    ProfilePicture = socialUser.Picture,
                    LastSocialLogin = DateTime.UtcNow
                };

                // Set social ID based on provider
                switch (socialUser.Provider.ToLower())
                {
                    case "google":
                        user.GoogleId = socialUser.ProviderId;
                        break;
                    case "facebook":
                        user.FacebookId = socialUser.ProviderId;
                        break;
                }

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Error creating user: {errors}");
                }

                // Add default role
                await _userManager.AddToRoleAsync(user, "User");

                _logger.LogInformation("User successfully created from social data: {Email}", socialUser.Email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user from social data");
                throw;
            }
        }

        public async Task<ApplicationUser> LinkSocialAccountAsync(ApplicationUser user, SocialUserInfo socialUser)
        {
            try
            {
                // Link social ID to existing user
                switch (socialUser.Provider.ToLower())
                {
                    case "google":
                        user.GoogleId = socialUser.ProviderId;
                        break;
                    case "facebook":
                        user.FacebookId = socialUser.ProviderId;
                        break;
                }

                // Update user data
                if (!string.IsNullOrEmpty(socialUser.Picture) && string.IsNullOrEmpty(user.ProfilePicture))
                    user.ProfilePicture = socialUser.Picture;

                user.EmailVerified = true;
                user.LastSocialLogin = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Error linking social account: {errors}");
                }

                _logger.LogInformation("Social account {Provider} linked to user: {UserId}", socialUser.Provider, user.Id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking social account");
                throw;
            }
        }

        public async Task UpdateSocialLoginAsync(ApplicationUser user, SocialUserInfo socialUser)
        {
            try
            {
                user.LastSocialLogin = DateTime.UtcNow;

                // Update picture if not exists
                if (!string.IsNullOrEmpty(socialUser.Picture) && string.IsNullOrEmpty(user.ProfilePicture))
                    user.ProfilePicture = socialUser.Picture;

                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Social login data updated for user: {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating social login data");
                throw;
            }
        }

        public async Task<bool> ValidateProviderTokenAsync(string provider, string token, string providerId)
        {
            try
            {
                // Future implementation to validate tokens with external APIs
                // For now, returns true assuming the OAuth middleware has already validated
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating provider token {Provider}", provider);
                return false;
            }
        }

        private string GenerateUserNameFromEmail(string email)
        {
            // Generate unique username based on email
            var username = email.Split('@')[0];
            var random = new Random();
            return $"{username}_{random.Next(1000, 9999)}";
        }
    }
}