using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Orders.Handlers
{
    public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, Result<UpdateOrderResponse>>
    {
        private readonly IOrderRepository _orderRepository;

        public UpdateOrderCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Result<UpdateOrderResponse>> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
        {
            var result = new Result<UpdateOrderResponse>();

            try
            {
                var response = await _orderRepository.UpdateOrder(request.OrderId, request.Request, cancellationToken);
                result.Value = response;
                result.Count = 1;
                result.HasSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                result.WithError($"Error updating order: {ex.Message}");
                return result;
            }
        }
    }
}
