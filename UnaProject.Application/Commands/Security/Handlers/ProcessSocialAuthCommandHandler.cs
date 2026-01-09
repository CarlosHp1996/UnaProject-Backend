using MediatR;
using Microsoft.AspNetCore.Identity;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Entities.Security;
using UnaProject.Domain.Helpers;
using UnaProject.Domain.Security;

namespace UnaProject.Application.Commands.Security.Handlers
{
    public class ProcessSocialAuthCommandHandler : IRequestHandler<ProcessSocialAuthCommand, Result<SocialAuthResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly AccessManager _accessManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISocialAuthService _socialAuthService;

        public ProcessSocialAuthCommandHandler(
            IUserRepository userRepository,
            AccessManager accessManager,
            UserManager<ApplicationUser> userManager,
            ISocialAuthService socialAuthService)
        {
            _userRepository = userRepository;
            _accessManager = accessManager;
            _userManager = userManager;
            _socialAuthService = socialAuthService;
        }

        public async Task<Result<SocialAuthResponse>> Handle(ProcessSocialAuthCommand request, CancellationToken cancellationToken)
        {
            var result = new Result<SocialAuthResponse>();

            try
            {
                // Basic validation
                if (string.IsNullOrEmpty(request.SocialUser.Email) ||
                    string.IsNullOrEmpty(request.SocialUser.ProviderId) ||
                    string.IsNullOrEmpty(request.SocialUser.Provider))
                {
                    result.WithError("Invalid social user data.");
                    return result;
                }

                // Process social authentication
                var socialAuthResponse = await _socialAuthService.ProcessSocialUserAsync(request.SocialUser);

                result.Value = socialAuthResponse;
                result.Count = 1;
                return result;
            }
            catch (Exception ex)
            {
                result.WithError($"Error during social authentication: {ex.Message}");
                return result;
            }
        }
    }
}