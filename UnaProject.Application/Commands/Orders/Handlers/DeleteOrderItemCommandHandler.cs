using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Orders.Handlers
{
    public class DeleteOrderItemCommandHandler : IRequestHandler<DeleteOrderItemCommand, Result>
    {
        private readonly IOrderRepository _orderRepository;

        public DeleteOrderItemCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Result> Handle(DeleteOrderItemCommand request, CancellationToken cancellationToken)
        {
            var result = new Result();

            try
            {
                var success = await _orderRepository.DeleteOrderItem(request.OrderId, request.OrderItemId, cancellationToken);

                if (success)
                {
                    result.HasSuccess = true;
                    return result;
                }

                result.WithError("Order item not found or could not be deleted");
                return result;
            }
            catch (Exception ex)
            {
                result.WithError($"Error deleting order item: {ex.Message}");
                return result;
            }
        }
    }
}
