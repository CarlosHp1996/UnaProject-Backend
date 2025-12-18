using MediatR;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Payments.Handlers
{
    public class GetPaymentByBillingIdQueryHandler : IRequestHandler<GetPaymentByBillingIdQuery, Result<PaymentDto>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<GetPaymentByBillingIdQueryHandler> _logger;

        public GetPaymentByBillingIdQueryHandler(
            IPaymentRepository paymentRepository,
            ILogger<GetPaymentByBillingIdQueryHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<Result<PaymentDto>> Handle(GetPaymentByBillingIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting payment by BillingId: {BillingId}", request.BillingId);

                var result = await _paymentRepository.GetPaymentByBillingId(request.BillingId, cancellationToken);

                if (!result.HasSuccess)
                {
                    var errorResult = new Result<PaymentDto>();
                    errorResult.WithError(result.ErrorMessage ?? "Error when checking payment");
                    return errorResult;
                }

                _logger.LogInformation("Payment retrieved successfully by BillingId: {BillingId}", request.BillingId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by BillingId: {BillingId}", request.BillingId);
                var errorResult = new Result<PaymentDto>();
                errorResult.WithException("Internal error while checking payment.");
                return errorResult;
            }
        }
    }
}