using MediatR;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;
using UnaProject.Domain.Security;

namespace UnaProject.Application.Commands.Security.Handlers
{
    public class CreateLogoutCommandHandler : IRequestHandler<CreateLogoutCommand, Result<CreateLogoutResponse>>
    {
        private readonly AccessManager _accessManager;

        public CreateLogoutCommandHandler(AccessManager accessManager)
        {
            _accessManager = accessManager;
        }

        public async Task<Result<CreateLogoutResponse>> Handle(CreateLogoutCommand request, CancellationToken cancellationToken)
        {
            var result = new Result<CreateLogoutResponse>();

            await _accessManager.InvalidateToken(request.Request.Token);

            var response = new CreateLogoutResponse
            {
                Message = "Logout successful."
            };

            result.Value = response;
            result.Count = 1;
            return result;
        }
    }
}
