using MediatR;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security
{
    public class ForgoutPasswordCommand : IRequest<Result<ForgoutPasswordResponse>>
    {
        public string Email { get; set; }

        public ForgoutPasswordCommand(string email)
        {
            Email = email;
        }
    }
}
