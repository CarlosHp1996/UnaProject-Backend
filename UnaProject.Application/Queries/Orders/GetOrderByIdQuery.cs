using MediatR;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Orders
{
    public class GetOrderByIdQuery : IRequest<Result<GetOrderByIdResponse>>
    {
        public Guid OrderId { get; }

        public GetOrderByIdQuery(Guid orderId)
        {
            OrderId = orderId;
        }
    }
}
