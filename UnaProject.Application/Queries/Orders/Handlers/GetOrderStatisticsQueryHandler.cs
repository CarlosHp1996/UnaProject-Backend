using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Orders.Handlers
{
    public class GetOrderStatisticsQueryHandler : IRequestHandler<GetOrderStatisticsQuery, Result<OrderStatisticsResponse>>
    {
        private readonly IOrderRepository _orderRepository;

        public GetOrderStatisticsQueryHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Result<OrderStatisticsResponse>> Handle(GetOrderStatisticsQuery request, CancellationToken cancellationToken)
        {
            var result = new Result<OrderStatisticsResponse>();
            
            try
            {
                var stats = await _orderRepository.GetOrderStatistics(request.UserId, cancellationToken);
                result.Value = stats;
                result.HasSuccess = true;
            }
            catch (Exception ex)
            {
                result.WithError($"Error retrieving order statistics: {ex.Message}");
            }

            return result;
        }
    }
}
