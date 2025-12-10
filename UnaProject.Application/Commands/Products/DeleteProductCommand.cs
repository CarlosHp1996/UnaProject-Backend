using MediatR;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Products
{
    public class DeleteProductCommand : IRequest<Result>
    {
        public Guid Id { get; }

        public DeleteProductCommand(Guid id)
        {
            Id = id;
        }
    }
}
