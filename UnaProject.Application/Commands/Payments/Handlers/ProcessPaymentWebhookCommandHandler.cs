using MediatR;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Helpers;
using System.Text.Json;
using UnaProject.Application.Models.Dtos;
using System.Security.Cryptography;
using System.Text;

namespace UnaProject.Application.Commands.Payments.Handlers
{
    public class ProcessPaymentWebhookCommandHandler : IRequestHandler<ProcessPaymentWebhookCommand, ResultValue<bool>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IAbacatePayService _abacatePayService;
        private readonly IAuditService _auditService;
        private readonly IWebhookRetryService _webhookRetryService;
        private readonly IPaymentNotificationService _notificationService;
        private readonly ILogger<ProcessPaymentWebhookCommandHandler> _logger;

        public ProcessPaymentWebhookCommandHandler(
            IPaymentRepository paymentRepository,
            IAbacatePayService abacatePayService,
            IAuditService auditService,
            IWebhookRetryService webhookRetryService,
            IPaymentNotificationService notificationService,
            ILogger<ProcessPaymentWebhookCommandHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _abacatePayService = abacatePayService;
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
                    "Processing payment webhook. EventId: {EventId}, PayloadHash: {PayloadHash}",
                    webhookEventId, payloadHash);

                // Check idempotency
                var idempotencyCheck = await _webhookRetryService.IsWebhookProcessedAsync(webhookEventId, payloadHash);
                if (idempotencyCheck.HasSuccess && idempotencyCheck.Value)
                {
                    _logger.LogInformation(
                        "Webhook already processed. EventId: {EventId}",
                        webhookEventId);
                    return ResultValue<bool>.Success(true);
                }

                // Validate signature if provided
                if (!string.IsNullOrEmpty(request.Signature) && !string.IsNullOrEmpty(request.WebhookSecret))
                {
                    var signatureValidation = await _abacatePayService.ValidateWebhookSignatureAsync(
                        request.WebhookPayload, request.Signature);

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
                }

                // Deserialize payload
                WebhookEventDto? webhookEvent;
                try
                {
                    webhookEvent = JsonSerializer.Deserialize<WebhookEventDto>(request.WebhookPayload, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (webhookEvent == null || webhookEvent.Data == null)
                    {
                        var errorMsg = "Invalid webhook payload";
                        await _webhookRetryService.LogWebhookFailureAsync(webhookEventId, errorMsg);

                        var result = new ResultValue<bool>();
                        result.WithError(errorMsg);
                        return result;
                    }
                }
                catch (JsonException ex)
                {
                    var errorMsg = $"Error processing webhook payload: {ex.Message}";
                    _logger.LogError(ex, "Failed to deserialize webhook payload");
                    await _webhookRetryService.LogWebhookFailureAsync(webhookEventId, errorMsg);

                    var result = new ResultValue<bool>();
                    result.WithError("Error processing webhook payload");
                    return result;
                }

                _logger.LogInformation("Processing webhook event: {EventType} for billing: {BillingId}",
                    webhookEvent.Event, webhookEvent.Data.BillingId);

                // Register webhook attempt
                var attemptResult = await _webhookRetryService.LogWebhookAttemptAsync(
                    Guid.Empty, // Will be updated when we find the payment
                    webhookEventId,
                    webhookEvent.Event ?? "unknown",
                    request.WebhookPayload,
                    payloadHash);

                if (!attemptResult.HasSuccess)
                {
                    _logger.LogWarning("Failed to log webhook attempt: {Error}", attemptResult.ErrorMessage);
                }

                // Process webhook
                var processResult = await _paymentRepository.ProcessPaymentWebhook(
                    webhookEvent.Data.BillingId ?? "",
                    webhookEvent.Data.Status ?? "",
                    webhookEvent.Data.PlatformFee,
                    webhookEvent.Data.PaidAt,
                    webhookEvent.Data.Metadata ?? "",
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

                // Get the payment by billing ID to access payment details
                var payment = await GetPaymentByBillingId(webhookEvent.Data.BillingId ?? "");
                if (payment == null)
                {
                    var errorMsg = "Payment not found after processing webhook";
                    _logger.LogWarning("Payment not found for BillingId: {BillingId}", webhookEvent.Data.BillingId);
                    await _webhookRetryService.LogWebhookFailureAsync(webhookEventId, errorMsg);

                    var result = new ResultValue<bool>();
                    result.WithError(errorMsg);
                    return result;
                }

                // Register audit log
                await _auditService.LogWebhookReceivedAsync(
                    payment.Id,
                    webhookEvent.Event ?? "unknown",
                    payloadHash,
                    webhookEvent,
                    request.IPAddress);

                // Mark webhook as processed
                await _webhookRetryService.MarkWebhookAsProcessedAsync(
                    webhookEventId,
                    $"Webhook processed successfully for payment {payment.Id}");

                // Send notifications based on status
                await SendNotificationBasedOnStatus(payment, webhookEvent.Event ?? "");

                _logger.LogInformation("Webhook processed successfully. PaymentId: {PaymentId}, Event: {Event}",
                    payment.Id, webhookEvent.Event);

                return processResult;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Internal error processing webhook: {ex.Message}";
                _logger.LogError(ex, "Error processing payment webhook. EventId: {EventId}", webhookEventId);

                // Try to register failure in webhook if possible
                try
                {
                    await _webhookRetryService.LogWebhookFailureAsync(webhookEventId, errorMsg);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to log webhook failure");
                }

                var result = new ResultValue<bool>();
                result.WithError(errorMsg);
                return result;
            }
        }

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
                        await _notificationService.SendPaymentConfirmedNotificationAsync(
                            payment.Id, payment.CustomerEmail);
                        break;

                    case "billing.failed":
                        // Notify admin about failure
                        await _notificationService.SendPaymentFailedAdminNotificationAsync(
                            payment.Id, $"Payment failed via webhook: {eventType}");
                        break;

                    case "billing.cancelled":
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

        private async Task<Domain.Entities.Payment?> GetPaymentByBillingId(string billingId)
        {
            try
            {
                // Get all payments and find by billing ID
                var allPayments = await _paymentRepository.GetAll(null, null, "Id", true);
                var payments = allPayments.Result(out var totalCount);
                return payments?.FirstOrDefault(p => p.BillingId == billingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by billing ID: {BillingId}", billingId);
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