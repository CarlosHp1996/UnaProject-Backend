using MediatR;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Payments
{
    public class CancelPaymentCommand : IRequest<ResultValue<bool>>
    {
        public Guid PaymentId { get; set; }
        public string Reason { get; set; }

        public CancelPaymentCommand(Guid paymentId, string reason = "Cancellation requested by the user.")
        {
            PaymentId = paymentId;
            Reason = reason;
        }
    }
}
