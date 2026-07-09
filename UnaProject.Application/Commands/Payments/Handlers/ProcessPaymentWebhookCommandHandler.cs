using MediatR;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos.Gateway;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Helpers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UnaProject.Application.Commands.Payments.Handlers
{
    public class ProcessPaymentWebhookCommandHandler : IRequestHandler<ProcessPaymentWebhookCommand, ResultValue<bool>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentGatewayResolver _paymentGatewayResolver;
        private readonly IAuditService _auditService;
        private readonly IWebhookRetryService _webhookRetryService;
        private readonly IPaymentNotificationService _notificationService;
        private readonly ILogger<ProcessPaymentWebhookCommandHandler> _logger;

        public ProcessPaymentWebhookCommandHandler(
            IPaymentRepository paymentRepository,
            IPaymentGatewayResolver paymentGatewayResolver,
            IAuditService auditService,
            IWebhookRetryService webhookRetryService,
            IPaymentNotificationService notificationService,
            ILogger<ProcessPaymentWebhookCommandHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _paymentGatewayResolver = paymentGatewayResolver;
            _auditService = auditService;
            _webhookRetryService = webhookRetryService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<ResultValue<bool>> Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
        {
            var payloadHash = GeneratePayloadHash(request.WebhookPayload);
            var webhookEventId = ExtractEventIdFromPayload(request.WebhookPayload);

            try
            {
                _logger.LogInformation(
                    "Processing payment webhook. Provider: {Provider}, EventId: {EventId}, PayloadHash: {PayloadHash}",
                    request.Provider, webhookEventId, payloadHash);

                // Check idempotency
                var idempotencyCheck = await _webhookRetryService.IsWebhookProcessedAsync(webhookEventId, payloadHash);
                if (idempotencyCheck.HasSuccess && idempotencyCheck.Value)
                {
                    _logger.LogInformation(
                        "Webhook already processed. EventId: {EventId}",
                        webhookEventId);
                    return ResultValue<bool>.Success(true);
                }

                IPaymentGateway gateway;
                try
                {
                    gateway = _paymentGatewayResolver.ResolveByProvider(request.Provider);
                }
                catch (InvalidOperationException ex)
                {
                    var errorMsg = $"Unknown payment provider: {request.Provider}";
                    _logger.LogWarning(ex, errorMsg);
                    await _webhookRetryService.LogWebhookFailureAsync(webhookEventId, errorMsg);

                    var result = new ResultValue<bool>();
                    result.WithError(errorMsg);
                    return result;
                }

                // M-1: fail-closed. Assinatura ausente/vazia => falha (nunca pula a validação).
                if (string.IsNullOrEmpty(request.Signature))
                {
                    var errorMsg = "Missing webhook signature";
                    _logger.LogWarning("Webhook received without signature. EventId: {EventId}", webhookEventId);
                    await _webhookRetryService.LogWebhookFailureAsync(webhookEventId, errorMsg);

                    var result = new ResultValue<bool>();
                    result.WithError(errorMsg);
                    return result;
                }

                var signatureValidation = await gateway.ValidateWebhookSignatureAsync(
                    request.WebhookPayload, request.Signature, request.WebhookSecret);

                if (!signatureValidation.HasSuccess || signatureValidation.Value != true)
                {
                    var errorMsg = "Invalid webhook signature";
                    _logger.LogWarning(
                        "Invalid webhook signature received. EventId: {EventId}",
                        webhookEventId);

                    await _webhookRetryService.LogWebhookFailureAsync(webhookEventId, errorMsg);

                    var result = new ResultValue<bool>();
                    result.WithError(errorMsg);
                    return result;
                }

                // Parse (already-normalized) webhook event through the resolved gateway
                var parseResult = await gateway.ParseWebhookEventAsync(request.WebhookPayload);
                if (!parseResult.HasSuccess || parseResult.Value == null)
                {
                    var errorMsg = "Invalid webhook payload";
                    await _webhookRetryService.LogWebhookFailureAsync(webhookEventId, errorMsg);

                    var result = new ResultValue<bool>();
                    result.WithError(errorMsg);
                    return result;
                }

                var webhookEvent = parseResult.Value;

                _logger.LogInformation("Processing webhook event: {EventType} for external id: {ExternalId}",
                    webhookEvent.EventType, webhookEvent.ExternalId);

                // Register webhook attempt
                var attemptResult = await _webhookRetryService.LogWebhookAttemptAsync(
                    Guid.Empty, // Will be updated when we find the payment
                    webhookEventId,
                    webhookEvent.EventType,
                    request.WebhookPayload,
                    payloadHash);

                if (!attemptResult.HasSuccess)
                {
                    _logger.LogWarning("Failed to log webhook attempt: {Error}", attemptResult.ErrorMessage);
                }

                var paidAt = IsPaidStatus(webhookEvent.Status) ? DateTime.UtcNow : (DateTime?)null;

                // Process webhook (repository procura o Payment por TransactionId OU BillingId).
                // A-2: repassa valor recebido (centavos) e moeda para conferência antes de liberar o pedido.
                var processResult = await _paymentRepository.ProcessPaymentWebhook(
                    webhookEvent.ExternalId,
                    webhookEvent.Status,
                    null,
                    paidAt,
                    webhookEvent.RawPayload,
                    webhookEvent.AmountReceivedCents,
                    webhookEvent.Currency,
                    cancellationToken);

                if (!processResult.HasSuccess)
                {
                    var errorMsg = processResult.ErrorMessage ?? "Error processing webhook";
                    _logger.LogError("Failed to process webhook: {Error}", errorMsg);
                    await _webhookRetryService.LogWebhookFailureAsync(webhookEventId, errorMsg);

                    var result = new ResultValue<bool>();
                    result.WithError(errorMsg);
                    return result;
                }

                // Get the payment by external id to access payment details
                var payment = await GetPaymentByExternalId(webhookEvent.ExternalId);
                if (payment == null)
                {
                    var errorMsg = "Payment not found after processing webhook";
                    _logger.LogWarning("Payment not found for ExternalId: {ExternalId}", webhookEvent.ExternalId);
                    await _webhookRetryService.LogWebhookFailureAsync(webhookEventId, errorMsg);

                    var result = new ResultValue<bool>();
                    result.WithError(errorMsg);
                    return result;
                }

                // Register audit log
                await _auditService.LogWebhookReceivedAsync(
                    payment.Id,
                    webhookEvent.EventType,
                    payloadHash,
                    webhookEvent,
                    request.IPAddress);

                // Mark webhook as processed
                await _webhookRetryService.MarkWebhookAsProcessedAsync(
                    webhookEventId,
                    $"Webhook processed successfully for payment {payment.Id}");

                // Send notifications based on status
                await SendNotificationBasedOnStatus(payment, webhookEvent.EventType);

                _logger.LogInformation("Webhook processed successfully. PaymentId: {PaymentId}, Event: {Event}",
                    payment.Id, webhookEvent.EventType);

                return processResult;
            }
            catch (Exception ex)
            {
                // B-1: detalhe da exceção fica no log interno; a resposta ao cliente é genérica.
                _logger.LogError(ex, "Error processing payment webhook. EventId: {EventId}", webhookEventId);

                // Try to register failure in webhook if possible
                try
                {
                    await _webhookRetryService.LogWebhookFailureAsync(webhookEventId, $"Internal error: {ex.Message}");
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to log webhook failure");
                }

                var result = new ResultValue<bool>();
                result.WithError("Internal error processing webhook");
                return result;
            }
        }

        private static bool IsPaidStatus(string status) =>
            status.Equals("succeeded", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("paid", StringComparison.OrdinalIgnoreCase);

        private async Task SendNotificationBasedOnStatus(Domain.Entities.Payment payment, string eventType)
        {
            try
            {
                if (string.IsNullOrEmpty(payment.CustomerEmail))
                {
                    _logger.LogWarning("Payment {PaymentId} has no customer email for notification", payment.Id);
                    return;
                }

                switch (eventType.ToLower())
                {
                    case "billing.paid":
                    case "payment_intent.succeeded":
                        await _notificationService.SendPaymentConfirmedNotificationAsync(
                            payment.Id, payment.CustomerEmail);
                        break;

                    case "billing.failed":
                    case "payment_intent.payment_failed":
                        // Notify admin about failure
                        await _notificationService.SendPaymentFailedAdminNotificationAsync(
                            payment.Id, $"Payment failed via webhook: {eventType}");
                        break;

                    case "billing.cancelled":
                    case "payment_intent.canceled":
                        await _notificationService.SendPaymentCancelledNotificationAsync(
                            payment.Id, payment.CustomerEmail);
                        break;

                    case "billing.expired":
                        await _notificationService.SendPaymentExpiredNotificationAsync(
                            payment.Id, payment.CustomerEmail);
                        break;

                    default:
                        _logger.LogInformation(
                            "No notification configured for event type: {EventType}", eventType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send notification for payment {PaymentId}, event {EventType}",
                    payment.Id, eventType);
                // Don't fail webhook due to notification
            }
        }

        private async Task<Domain.Entities.Payment?> GetPaymentByExternalId(string externalId)
        {
            try
            {
                // Get all payments and find by TransactionId (Stripe) or BillingId (AbacatePay legado)
                var allPayments = await _paymentRepository.GetAll(null, null, "Id", true);
                var payments = allPayments.Result(out var totalCount);
                return payments?.FirstOrDefault(p => p.TransactionId == externalId || p.BillingId == externalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by external id: {ExternalId}", externalId);
                return null;
            }
        }

        private string GeneratePayloadHash(string payload)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hashBytes);
        }

        private string ExtractEventIdFromPayload(string payload)
        {
            try
            {
                using var document = JsonDocument.Parse(payload);
                if (document.RootElement.TryGetProperty("id", out var idProperty))
                    return idProperty.GetString() ?? Guid.NewGuid().ToString();

                if (document.RootElement.TryGetProperty("event_id", out var eventIdProperty))
                    return eventIdProperty.GetString() ?? Guid.NewGuid().ToString();

                // If no ID found in payload, generate based on hash
                return GeneratePayloadHash(payload);
            }
            catch
            {
                // In case of error, generate ID based on payload hash
                return GeneratePayloadHash(payload);
            }
        }
    }
}
