using MediatR;
using UnaProject.Application.Models.Requests.Security;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security
{
    public class UpdateUserCommand : IRequest<Result<UpdateUserResponse>>
    {
        public Guid Id;
        public UpdateUserRequest Request { get; set; }

        public UpdateUserCommand(Guid id, UpdateUserRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
