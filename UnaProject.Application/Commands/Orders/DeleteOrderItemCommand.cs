using MediatR;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Orders
{
    public class DeleteOrderItemCommand : IRequest<Result>
    {
        public Guid OrderId { get; }
        public Guid OrderItemId { get; }

        public DeleteOrderItemCommand(Guid orderId, Guid orderItemId)
        {
            OrderId = orderId;
            OrderItemId = orderItemId;
        }
    }
}
