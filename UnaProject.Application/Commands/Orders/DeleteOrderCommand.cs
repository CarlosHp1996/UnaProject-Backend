using MediatR;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Orders
{
    public class DeleteOrderCommand : IRequest<Result>
    {
        public Guid OrderId { get; }

        public DeleteOrderCommand(Guid orderId)
        {
            OrderId = orderId;
        }
    }
}
