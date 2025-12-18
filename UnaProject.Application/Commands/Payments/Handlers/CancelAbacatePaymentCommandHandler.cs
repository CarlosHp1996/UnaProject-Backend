using MediatR;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Interfaces;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Payments.Handlers
{
    public class CancelAbacatePaymentCommandHandler : IRequestHandler<CancelAbacatePaymentCommand, ResultValue<bool>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<CancelAbacatePaymentCommandHandler> _logger;

        public CancelAbacatePaymentCommandHandler(
            IPaymentRepository paymentRepository,
            ILogger<CancelAbacatePaymentCommandHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<ResultValue<bool>> Handle(CancelAbacatePaymentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Cancelling payment: {PaymentId} - Reason: {Reason}",
                    request.PaymentId, request.Reason);

                var result = await _paymentRepository.CancelPayment(request.PaymentId, request.Reason, cancellationToken);

                if (!result.HasSuccess)
                {
                    _logger.LogError("Failed to cancel payment: {PaymentId} - {Error}",
                        request.PaymentId, result.ErrorMessage);
                    var errorResult = new ResultValue<bool>();
                    errorResult.WithError(result.ErrorMessage ?? "Error canceling payment");
                    return errorResult;
                }

                _logger.LogInformation("Payment cancelled successfully: {PaymentId}", request.PaymentId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payment: {PaymentId}", request.PaymentId);
                var result = new ResultValue<bool>();
                result.WithException("Internal error canceling payment");
                return result;
            }
        }
    }
}