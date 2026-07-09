namespace UnaProject.Application.Models.Dtos
{
    /// <summary>
    /// Resposta de criação de pagamento retornada por POST /api/payment/create.
    /// O nome mantém o histórico (fluxo antigo AbacatePay), mas hoje é usada pelo fluxo Stripe:
    /// o campo relevante é <see cref="ClientSecret"/> (PaymentIntent do Stripe).
    /// </summary>
    public class CreateAbacatePaymentResponse
    {
        public Guid PaymentId { get; set; }
        public string? BillingId { get; set; }
        public string? PaymentUrl { get; set; }
        public string? QrCode { get; set; }
        public string? QrCodeImage { get; set; }
        public string? Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public decimal Amount { get; set; }
        public bool DevMode { get; set; }

        /// <summary>
        /// PaymentIntent.ClientSecret do Stripe, usado pelo frontend (Stripe Elements/Stripe.js)
        /// para confirmar o pagamento no navegador.
        /// </summary>
        public string? ClientSecret { get; set; }
    }
}
