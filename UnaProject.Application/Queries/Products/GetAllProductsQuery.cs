using MediatR;
using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Responses.Products;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Products
{
    public class GetAllProductsQuery : IRequest<Result<GetAllProductsResponse>>
    {
        public GetProductsRequestFilter Filter { get; }

        public GetAllProductsQuery(GetProductsRequestFilter filter)
        {
            Filter = filter;
        }
    }
}
