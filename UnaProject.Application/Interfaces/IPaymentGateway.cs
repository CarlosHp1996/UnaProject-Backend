using UnaProject.Application.Models.Dtos.Gateway;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Interfaces
{
    /// <summary>
    /// Abstração de gateway de pagamento. Cada provedor (Stripe, AbacatePay, ...)
    /// implementa esta interface para que o restante da aplicação (PaymentRepository,
    /// handlers de webhook) nunca dependa diretamente de um SDK específico.
    /// </summary>
    public interface IPaymentGateway
    {
        /// <summary>Identificador do provedor usado para resolução por webhook (ex.: "stripe").</summary>
        string ProviderName { get; }

        /// <summary>Métodos de pagamento suportados por este gateway (ex.: "card", "pix").</summary>
        IReadOnlyCollection<string> SupportedMethods { get; }

        Task<Result<GatewayPaymentResult>> CreatePaymentAsync(GatewayPaymentRequest request, CancellationToken ct);
        Task<Result<GatewayPaymentStatus>> GetStatusAsync(string externalId, CancellationToken ct);
        Task<ResultValue<bool>> CancelAsync(string externalId, string reason, CancellationToken ct);
        Task<ResultValue<bool>> ValidateWebhookSignatureAsync(string rawPayload, string? signature, string? secret);
        Task<Result<GatewayWebhookEvent>> ParseWebhookEventAsync(string rawPayload);
    }
}
