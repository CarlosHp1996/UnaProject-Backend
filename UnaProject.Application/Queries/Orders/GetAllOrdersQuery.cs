using MediatR;
using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Orders
{
    public class GetAllOrdersQuery : IRequest<Result<GetAllOrdersResponse>>
    {
        public GetOrdersRequestFilter Filter { get; }

        public GetAllOrdersQuery(GetOrdersRequestFilter filter)
        {
            Filter = filter;
        }
    }
}
