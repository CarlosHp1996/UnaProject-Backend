using MediatR;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Payments.Handlers
{
    public class GetPaymentsByOrderQueryHandler : IRequestHandler<GetPaymentsByOrderQuery, Result<List<PaymentDto>>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<GetPaymentsByOrderQueryHandler> _logger;

        public GetPaymentsByOrderQueryHandler(
            IPaymentRepository paymentRepository,
            ILogger<GetPaymentsByOrderQueryHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<Result<List<PaymentDto>>> Handle(GetPaymentsByOrderQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting payments for order: {OrderId}", request.OrderId);

                var result = await _paymentRepository.GetPaymentsByOrder(request.OrderId, cancellationToken);

                if (!result.HasSuccess)
                {
                    var errorResult = new Result<List<PaymentDto>>();
                    errorResult.WithError(result.ErrorMessage ?? "Error when checking payments for order");
                    return errorResult;
                }

                _logger.LogInformation("Retrieved {Count} payments for order: {OrderId}",
                    result.Value?.Count ?? 0, request.OrderId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments for order: {OrderId}", request.OrderId);
                var errorResult = new Result<List<PaymentDto>>();
                errorResult.WithException("Internal error while checking payments for order.");
                return errorResult;
            }
        }
    }
}