using MediatR;
using UnaProject.Application.Models.Requests.Orders;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Orders
{
    public class UpdateOrderItemCommand : IRequest<Result<UpdateOrderItemResponse>>
    {
        public Guid OrderId { get; }
        public Guid OrderItemId { get; }
        public UpdateOrderItemRequest Request { get; }

        public UpdateOrderItemCommand(Guid orderId, Guid orderItemId, UpdateOrderItemRequest request)
        {
            OrderId = orderId;
            OrderItemId = orderItemId;
            Request = request;
        }
    }
}
