using UnaProject.Application.Models.Filters;
using UnaProject.Application.Models.Requests.Orders;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Domain.Entities;

namespace UnaProject.Application.Interfaces
{
    public interface IOrderRepository : IBaseRepository<Order>
    {
        Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken);

        Task<IEnumerable<Order>> GetPendingOrders(TimeSpan pendingTime);

        Task<(IQueryable<Order> Result, int TotalCount)> Get(GetOrdersRequestFilter filter);
        Task<Order> GetById(Guid id, CancellationToken cancellationToken);
        Task<UpdateOrderResponse> UpdateOrder(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken);
        Task<UpdateOrderItemResponse> UpdateOrderItem(Guid orderId, Guid orderItemId, UpdateOrderItemRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteOrder(Guid id, CancellationToken cancellationToken);
        Task<bool> DeleteOrderItem(Guid orderId, Guid orderItemId, CancellationToken cancellationToken);
    }
}
