using MediatR;
using UnaProject.Application.Models.Dtos;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Payments
{
    public class GetPaymentByBillingIdQuery : IRequest<Result<PaymentDto>>
    {
        public string BillingId { get; set; }

        public GetPaymentByBillingIdQuery(string billingId)
        {
            BillingId = billingId;
        }
    }
}