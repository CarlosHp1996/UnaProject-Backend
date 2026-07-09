namespace UnaProject.Application.Models.Dtos.Gateway
{
    /// <summary>
    /// Requisição genérica de criação de pagamento, agnóstica de provedor
    /// (Stripe, AbacatePay, etc.). Construída a partir de CreateAbacatePaymentRequest
    /// pelo PaymentRepository antes de ser enviada ao IPaymentGateway resolvido.
    /// </summary>
    public class GatewayPaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "BRL";
        public Guid OrderId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerDocument { get; set; }
        public string? CustomerPhone { get; set; }

        /// <summary>"card", "pix", etc.</summary>
        public string PaymentMethod { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
