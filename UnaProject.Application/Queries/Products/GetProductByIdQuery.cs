using MediatR;
using UnaProject.Application.Models.Responses.Products;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Products
{
    public class GetProductByIdQuery : IRequest<Result<GetProductByIdResponse>>
    {
        public Guid Id { get; }

        public GetProductByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
