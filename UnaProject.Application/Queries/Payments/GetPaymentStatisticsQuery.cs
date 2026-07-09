using MediatR;
using UnaProject.Application.Models.Responses.Payments;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Payments
{
    public class GetPaymentStatisticsQuery : IRequest<Result<PaymentStatisticsResponse>>
    {
        public Guid? UserId { get; set; }

        public GetPaymentStatisticsQuery(Guid? userId = null)
        {
            UserId = userId;
        }
    }
}
