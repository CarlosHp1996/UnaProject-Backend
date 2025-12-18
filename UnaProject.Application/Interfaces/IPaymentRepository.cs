using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Requests.Payments;
using UnaProject.Domain.Entities;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Interfaces
{
    public interface IPaymentRepository : IBaseRepository<Payment>
    {
        Task<Result<CreateAbacatePaymentResponse>> CreateAbacatePayment(
            CreateAbacatePaymentRequest request,
            CancellationToken cancellationToken);

        Task<ResultValue<bool>> ProcessPaymentWebhook(
            string billingId,
            string status,
            decimal? fee,
            DateTime? paidAt,
            string metadata,
            CancellationToken cancellationToken);

        Task<ResultValue<bool>> CancelPayment(
            Guid paymentId,
            string reason,
            CancellationToken cancellationToken);

        Task<Result<PaymentStatusDto>> GetPaymentStatus(
            Guid paymentId,
            CancellationToken cancellationToken);

        Task<Result<PaymentDto>> GetPaymentByBillingId(
            string billingId,
            CancellationToken cancellationToken);

        Task<Result<List<PaymentDto>>> GetPaymentsByOrder(
            Guid orderId,
            CancellationToken cancellationToken);
    }
}
