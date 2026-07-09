using MediatR;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Payments
{
    public class CancelPaymentCommand : IRequest<ResultValue<bool>>
    {
        public Guid PaymentId { get; set; }
        public string Reason { get; set; }
        // Preenchidos pelo controller a partir das claims do JWT — nunca do corpo.
        public Guid UserId { get; set; }
        public bool IsAdmin { get; set; }

        public CancelPaymentCommand(Guid paymentId, string reason = "Cancellation requested by the user.", Guid userId = default, bool isAdmin = false)
        {
            PaymentId = paymentId;
            Reason = reason;
            UserId = userId;
            IsAdmin = isAdmin;
        }
    }
}
