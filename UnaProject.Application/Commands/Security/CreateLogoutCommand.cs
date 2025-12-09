using MediatR;
using UnaProject.Application.Models.Requests.Security;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security
{
    public class CreateLogoutCommand : IRequest<Result<CreateLogoutResponse>>
    {
        public CreateLogoutRequest Request;

        public CreateLogoutCommand(CreateLogoutRequest request)
        {
            Request = request;
        }
    }
}
