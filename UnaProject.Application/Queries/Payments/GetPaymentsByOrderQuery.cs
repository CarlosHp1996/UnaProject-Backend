using MediatR;
using UnaProject.Application.Models.Dtos;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Payments
{
    public class GetPaymentsByOrderQuery : IRequest<Result<List<PaymentDto>>>
    {
        public Guid OrderId { get; set; }

        public GetPaymentsByOrderQuery(Guid orderId)
        {
            OrderId = orderId;
        }
    }
}