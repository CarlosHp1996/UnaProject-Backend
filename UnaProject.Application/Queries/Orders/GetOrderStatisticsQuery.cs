using MediatR;
using UnaProject.Application.Models.Responses.Orders;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Orders
{
    public class GetOrderStatisticsQuery : IRequest<Result<OrderStatisticsResponse>>
    {
        public Guid? UserId { get; set; }

        public GetOrderStatisticsQuery(Guid? userId = null)
        {
            UserId = userId;
        }
    }
}
