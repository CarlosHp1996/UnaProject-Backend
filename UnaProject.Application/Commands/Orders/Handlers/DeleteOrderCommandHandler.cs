using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Orders.Handlers
{
    public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, Result>
    {
        private readonly IOrderRepository _orderRepository;

        public DeleteOrderCommandHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Result> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
        {
            var result = new Result();

            try
            {
                var success = await _orderRepository.DeleteOrder(request.OrderId, cancellationToken);

                if (success)
                {
                    result.HasSuccess = true;
                    return result;
                }

                result.WithError("Order not found or could not be deleted");
                return result;
            }
            catch (Exception ex)
            {
                result.WithError($"Error deleting order: {ex.Message}");
                return result;
            }
        }
    }
}
