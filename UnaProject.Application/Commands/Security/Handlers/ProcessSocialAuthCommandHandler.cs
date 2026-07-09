using MediatR;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security.Handlers
{
    public class ProcessSocialAuthCommandHandler : IRequestHandler<ProcessSocialAuthCommand, Result<SocialAuthResponse>>
    {
        private readonly ISocialAuthService _socialAuthService;
        private readonly ISocialTokenValidator _socialTokenValidator;
        private readonly ILogger<ProcessSocialAuthCommandHandler> _logger;

        public ProcessSocialAuthCommandHandler(
            ISocialAuthService socialAuthService,
            ISocialTokenValidator socialTokenValidator,
            ILogger<ProcessSocialAuthCommandHandler> logger)
        {
            _socialAuthService = socialAuthService;
            _socialTokenValidator = socialTokenValidator;
            _logger = logger;
        }

        public async Task<Result<SocialAuthResponse>> Handle(ProcessSocialAuthCommand request, CancellationToken cancellationToken)
        {
            var result = new Result<SocialAuthResponse>();

            try
            {
                if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.AccessToken))
                {
                    result.WithError("Provider and access token are required.");
                    return result;
                }

                // 1) Valida o accessToken NO PROVEDOR (server-side) e obtém o perfil real.
                var validation = await _socialTokenValidator.ValidateAsync(request.Provider, request.AccessToken, cancellationToken);
                if (!validation.HasSuccess || validation.Value == null)
                {
                    _logger.LogWarning("Social token validation failed for provider {Provider}", request.Provider);
                    result.WithError(validation.ErrorMessage ?? "Invalid social access token.");
                    return result;
                }

                // 2) Fluxo de associação/criação de conta usa SOMENTE dados validados.
                var socialAuthResponse = await _socialAuthService.ProcessSocialUserAsync(validation.Value);

                result.Value = socialAuthResponse;
                result.Count = 1;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during social authentication for provider {Provider}", request.Provider);
                result.WithError("Error during social authentication.");
                return result;
            }
        }
    }
}
