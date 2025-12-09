using MediatR;
using UnaProject.Application.Models.Requests.Security;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security
{
    public class CreateLoginCommand : IRequest<Result<CreateLoginResponse>>
    {
        public CreateLoginRequest Request;
        public CreateLoginCommand(CreateLoginRequest request)
        {
            Request = request;
        }
    }
}
