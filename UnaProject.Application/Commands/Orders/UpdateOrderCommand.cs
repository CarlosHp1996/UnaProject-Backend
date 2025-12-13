using MediatR;
using UnaProject.Application.Models.Requests.Orders;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Orders
{
    public class UpdateOrderCommand : IRequest<Result<UpdateOrderResponse>>
    {
        public Guid OrderId { get; }
        public UpdateOrderRequest Request { get; }

        public UpdateOrderCommand(Guid orderId, UpdateOrderRequest request)
        {
            OrderId = orderId;
            Request = request;
        }
    }
}
