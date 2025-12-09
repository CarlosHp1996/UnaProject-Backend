using MediatR;
using UnaProject.Application.Models.Requests.Security;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security
{
    public class CreateUserCommand : IRequest<Result<CreateUserResponse>>
    {
        public CreateUserRequest Request;
        public CreateUserCommand(CreateUserRequest request)
        {
            Request = request;
        }
    }
}
