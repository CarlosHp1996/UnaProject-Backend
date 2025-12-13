using MediatR;
using UnaProject.Application.Models.Requests.Orders;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Orders
{
    public class CreateOrderCommand : IRequest<Result<CreateOrderResponse>>
    {
        public CreateOrderRequest Request { get; }

        public CreateOrderCommand(CreateOrderRequest request)
        {
            Request = request;
        }
    }
}
