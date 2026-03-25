using MediatR;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Products
{
    public class DeleteProductCommand : IRequest<Result>
    {
        public List<Guid> Ids { get; }

        public DeleteProductCommand(List<Guid> ids)
        {
            Ids = ids;
        }
    }
}
