using MediatR;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Payments.Handlers
{
    public class GetPaymentStatusQueryHandler : IRequestHandler<GetPaymentStatusQuery, Result<PaymentStatusDto>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<GetPaymentStatusQueryHandler> _logger;

        public GetPaymentStatusQueryHandler(
            IPaymentRepository paymentRepository,
            ILogger<GetPaymentStatusQueryHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<Result<PaymentStatusDto>> Handle(GetPaymentStatusQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting payment status for: {PaymentId}", request.PaymentId);

                var result = await _paymentRepository.GetPaymentStatus(request.PaymentId, cancellationToken);

                if (!result.HasSuccess)
                {
                    var errorResult = new Result<PaymentStatusDto>();
                    errorResult.WithError(result.ErrorMessage ?? "Error when checking payment status");
                    return errorResult;
                }

                _logger.LogInformation("Payment status retrieved successfully: {PaymentId} - {Status}",
                    request.PaymentId, result.Value?.Status);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status: {PaymentId}", request.PaymentId);
                var errorResult = new Result<PaymentStatusDto>();
                errorResult.WithException("Internal error while checking payment status.");
                return errorResult;
            }
        }
    }
}