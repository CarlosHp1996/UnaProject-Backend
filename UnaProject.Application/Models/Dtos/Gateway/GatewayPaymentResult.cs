namespace UnaProject.Application.Models.Dtos.Gateway
{
    /// <summary>
    /// Resultado genérico da criação de um pagamento em um gateway externo.
    /// Para Stripe: ExternalId = PaymentIntent.Id, ClientSecret = PaymentIntent.ClientSecret.
    /// </summary>
    public class GatewayPaymentResult
    {
        public string ExternalId { get; set; } = string.Empty;
        public string? ClientSecret { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentUrl { get; set; }
        public string? QrCode { get; set; }
        public string? RawResponse { get; set; }
    }
}
