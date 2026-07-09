namespace UnaProject.Application.Models.Dtos.Gateway
{
    /// <summary>
    /// Evento de webhook já normalizado pelo IPaymentGateway (ex.: Stripe "payment_intent.succeeded").
    /// </summary>
    public class GatewayWebhookEvent
    {
        public string EventType { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RawPayload { get; set; } = string.Empty;
    }
}
