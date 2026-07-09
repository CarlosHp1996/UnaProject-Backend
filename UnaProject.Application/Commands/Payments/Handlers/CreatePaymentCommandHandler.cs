using MediatR;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Requests.Payments;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Payments.Handlers
{
    public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, Result<CreateAbacatePaymentResponse>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<CreatePaymentCommandHandler> _logger;

        public CreatePaymentCommandHandler(
            IPaymentRepository paymentRepository,
            ILogger<CreatePaymentCommandHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<Result<CreateAbacatePaymentResponse>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating payment for order: {OrderId} via method {PaymentMethod}",
                    request.OrderId, request.PaymentMethod);

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
                    Metadata = request.Metadata,
                    // Vindos das claims do JWT (via command), nunca do corpo da requisição.
                    UserId = request.UserId,
                    IsAdmin = request.IsAdmin
                };

                var result = await _paymentRepository.CreateAbacatePayment(paymentRequest, cancellationToken);

                if (!result.HasSuccess)
                {
                    _logger.LogError("Failed to create payment: {Error}", result.ErrorMessage);
                    var errorResult = new Result<CreateAbacatePaymentResponse>();
                    errorResult.WithError(result.ErrorMessage ?? "Error creating payment");
                    return errorResult;
                }

                _logger.LogInformation("Payment created successfully: {PaymentId}", result.Value?.PaymentId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for order: {OrderId}", request.OrderId);
                var errorResult = new Result<CreateAbacatePaymentResponse>();
                errorResult.WithException("Internal error creating payment");
                return errorResult;
            }
        }
    }
}
