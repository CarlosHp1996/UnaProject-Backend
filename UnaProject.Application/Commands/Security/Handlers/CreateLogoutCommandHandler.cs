using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security.Handlers
{
    public class CreateLogoutCommandHandler : IRequestHandler<CreateLogoutCommand, Result<CreateLogoutResponse>>
    {
        private readonly ITokenBlacklistService _tokenBlacklist;

        public CreateLogoutCommandHandler(ITokenBlacklistService tokenBlacklist)
        {
            _tokenBlacklist = tokenBlacklist;
        }

        public async Task<Result<CreateLogoutResponse>> Handle(CreateLogoutCommand request, CancellationToken cancellationToken)
        {
            var result = new Result<CreateLogoutResponse>();

            await _tokenBlacklist.BlacklistTokenAsync(request.Request.Token, cancellationToken);

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
