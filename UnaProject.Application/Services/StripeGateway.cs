using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos.Gateway;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Services
{
    /// <summary>
    /// Implementação de IPaymentGateway usando Stripe PaymentIntents.
    /// Suporta "card" e "pix" através do mesmo PaymentIntent (payment_method_types).
    /// </summary>
    public class StripeGateway : IPaymentGateway
    {
        private readonly StripeOptions _options;
        private readonly ILogger<StripeGateway> _logger;
        private readonly IStripeClient _client;

        public string ProviderName => "stripe";
        public IReadOnlyCollection<string> SupportedMethods { get; } = new[] { "card", "pix" };

        public StripeGateway(IOptions<StripeOptions> options, ILogger<StripeGateway> logger)
        {
            _options = options.Value;
            _logger = logger;
            _client = new StripeClient(_options.STRIPE_SECRET_KEY ?? string.Empty);
        }

        public async Task<Result<GatewayPaymentResult>> CreatePaymentAsync(GatewayPaymentRequest request, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Creating Stripe PaymentIntent for order: {OrderId}", request.OrderId);

                var amountInCents = (long)Math.Round(request.Amount * 100m, MidpointRounding.AwayFromZero);

                var metadata = new Dictionary<string, string>
                {
                    ["OrderId"] = request.OrderId.ToString()
                };

                if (request.Metadata != null)
                {
                    foreach (var kv in request.Metadata)
                        metadata[kv.Key] = kv.Value;
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = amountInCents,
                    Currency = string.IsNullOrWhiteSpace(request.Currency) ? "brl" : request.Currency.ToLowerInvariant(),
                    PaymentMethodTypes = new List<string> { "card", "pix" },
                    Metadata = metadata,
                    ReceiptEmail = request.CustomerEmail
                };

                var service = new PaymentIntentService(_client);
                var paymentIntent = await service.CreateAsync(options, cancellationToken: ct);

                var result = new GatewayPaymentResult
                {
                    ExternalId = paymentIntent.Id,
                    ClientSecret = paymentIntent.ClientSecret,
                    Status = MapStatus(paymentIntent.Status),
                    PaymentUrl = null,
                    // Para PIX, o código copia-e-cola só existe depois que o PaymentIntent é
                    // confirmado no cliente (Stripe.js); na criação normalmente ainda não há NextAction.
                    QrCode = paymentIntent.NextAction?.PixDisplayQrCode?.Data,
                    RawResponse = SafeSerialize(paymentIntent)
                };

                _logger.LogInformation("Stripe PaymentIntent created: {PaymentIntentId} - Status: {Status}",
                    paymentIntent.Id, paymentIntent.Status);

                return Result<GatewayPaymentResult>.Success(result);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating PaymentIntent for order: {OrderId}", request.OrderId);
                var result = new Result<GatewayPaymentResult>();
                result.WithError($"Stripe error: {ex.StripeError?.Message ?? ex.Message}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating Stripe PaymentIntent for order: {OrderId}", request.OrderId);
                var result = new Result<GatewayPaymentResult>();
                result.WithException("Internal error creating Stripe payment");
                return result;
            }
        }

        public async Task<Result<GatewayPaymentStatus>> GetStatusAsync(string externalId, CancellationToken ct)
        {
            try
            {
                var service = new PaymentIntentService(_client);
                var paymentIntent = await service.GetAsync(externalId, cancellationToken: ct);

                var status = new GatewayPaymentStatus
                {
                    ExternalId = paymentIntent.Id,
                    Status = MapStatus(paymentIntent.Status),
                    // Stripe.net não expõe um timestamp de "pago em" no PaymentIntent;
                    // usamos o momento da consulta como aproximação quando o status é succeeded.
                    PaidAt = paymentIntent.Status == "succeeded" ? DateTime.UtcNow : (DateTime?)null
                };

                return Result<GatewayPaymentStatus>.Success(status);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error getting PaymentIntent status: {ExternalId}", externalId);
                var result = new Result<GatewayPaymentStatus>();
                result.WithError($"Stripe error: {ex.StripeError?.Message ?? ex.Message}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting Stripe PaymentIntent status: {ExternalId}", externalId);
                var result = new Result<GatewayPaymentStatus>();
                result.WithException("Internal error getting Stripe payment status");
                return result;
            }
        }

        public async Task<ResultValue<bool>> CancelAsync(string externalId, string reason, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Cancelling Stripe PaymentIntent: {ExternalId} - Reason: {Reason}", externalId, reason);

                var service = new PaymentIntentService(_client);
                await service.CancelAsync(externalId, cancellationToken: ct);

                return ResultValue<bool>.Success(true);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error cancelling PaymentIntent: {ExternalId}", externalId);
                var result = new ResultValue<bool>();
                result.WithError($"Stripe error: {ex.StripeError?.Message ?? ex.Message}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error cancelling Stripe PaymentIntent: {ExternalId}", externalId);
                var result = new ResultValue<bool>();
                result.WithException("Internal error cancelling Stripe payment");
                return result;
            }
        }

        public Task<ResultValue<bool>> ValidateWebhookSignatureAsync(string rawPayload, string? signature, string? secret)
        {
            try
            {
                var webhookSecret = string.IsNullOrWhiteSpace(secret) ? _options.STRIPE_WEBHOOK_SECRET : secret;

                if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(webhookSecret))
                {
                    var missing = new ResultValue<bool>();
                    missing.WithError("Webhook signature or secret not provided");
                    return Task.FromResult(missing);
                }

                // ConstructEvent valida a assinatura HMAC e lança StripeException se for inválida.
                EventUtility.ConstructEvent(rawPayload, signature, webhookSecret, throwOnApiVersionMismatch: false);

                return Task.FromResult(ResultValue<bool>.Success(true));
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Invalid Stripe webhook signature");
                var result = new ResultValue<bool>();
                result.WithError("Invalid webhook signature");
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error validating Stripe webhook signature");
                var result = new ResultValue<bool>();
                result.WithException("Internal error validating webhook signature");
                return Task.FromResult(result);
            }
        }

        public Task<Result<GatewayWebhookEvent>> ParseWebhookEventAsync(string rawPayload)
        {
            try
            {
                // Assume-se que a assinatura já foi validada via ValidateWebhookSignatureAsync antes desta chamada.
                var stripeEvent = EventUtility.ParseEvent(rawPayload, throwOnApiVersionMismatch: false);
                var paymentIntent = stripeEvent.Data?.Object as PaymentIntent;

                var gatewayEvent = new GatewayWebhookEvent
                {
                    EventType = stripeEvent.Type,
                    ExternalId = paymentIntent?.Id ?? string.Empty,
                    Status = MapStatus(paymentIntent?.Status ?? string.Empty),
                    RawPayload = rawPayload
                };

                return Task.FromResult(Result<GatewayWebhookEvent>.Success(gatewayEvent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Stripe webhook payload");
                var result = new Result<GatewayWebhookEvent>();
                result.WithException("Error parsing webhook payload");
                return Task.FromResult(result);
            }
        }

        /// <summary>
        /// Mapeia os status do PaymentIntent do Stripe para um vocabulário compatível
        /// com o restante do domínio (que hoje usa "pending"/"paid"/"succeeded"/"cancelled"/"failed").
        /// </summary>
        private static string MapStatus(string stripeStatus) => stripeStatus switch
        {
            "succeeded" => "succeeded",
            "canceled" => "cancelled",
            "processing" => "processing",
            "requires_payment_method" or "requires_confirmation" or "requires_action" or "requires_capture"
                => "pending",
            _ => stripeStatus
        };

        private string SafeSerialize(PaymentIntent paymentIntent)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(paymentIntent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to serialize PaymentIntent {Id} for RawResponse", paymentIntent.Id);
                return string.Empty;
            }
        }
    }
}
