using MediatR;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Requests.Payments;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Payments.Handlers
{
    public class CreateAbacatePaymentCommandHandler : IRequestHandler<CreateAbacatePaymentCommand, Result<CreateAbacatePaymentResponse>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<CreateAbacatePaymentCommandHandler> _logger;

        public CreateAbacatePaymentCommandHandler(
            IPaymentRepository paymentRepository,
            ILogger<CreateAbacatePaymentCommandHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<Result<CreateAbacatePaymentResponse>> Handle(CreateAbacatePaymentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating AbacatePay payment for order: {OrderId}", request.OrderId);

                var paymentRequest = new CreateAbacatePaymentRequest
                {
                    OrderId = request.OrderId,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    CustomerName = request.CustomerName,
                    CustomerDocument = request.CustomerDocument,
                    CustomerEmail = request.CustomerEmail,
                    CustomerPhone = request.CustomerPhone,
                    ReturnUrl = request.ReturnUrl,
                    Metadata = request.Metadata
                };

                var result = await _paymentRepository.CreateAbacatePayment(paymentRequest, cancellationToken);

                if (!result.HasSuccess)
                {
                    _logger.LogError("Failed to create AbacatePay payment: {Error}", result.ErrorMessage);
                    var errorResult = new Result<CreateAbacatePaymentResponse>();
                    errorResult.WithError(result.ErrorMessage ?? "Error creating payment");
                    return errorResult;
                }

                _logger.LogInformation("AbacatePay payment created successfully: {PaymentId}",
                    result.Value?.PaymentId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating AbacatePay payment for order: {OrderId}", request.OrderId);
                var errorResult = new Result<CreateAbacatePaymentResponse>();
                errorResult.WithException("Internal error creating payment");
                return errorResult;
            }
        }
    }
}