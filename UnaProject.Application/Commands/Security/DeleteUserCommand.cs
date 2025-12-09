using MediatR;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security
{
    public class DeleteUserCommand : IRequest<Result<DeleteUserResponse>>
    {
        public Guid Id { get; }

        public DeleteUserCommand(Guid id)
        {
            Id = id;
        }
    }
}
