namespace UnaProject.Application.Models.Responses.Payments
{
    public class CreateAbacatePaymentResponse
    {
        public Guid PaymentId { get; set; }
        public string BillingId { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public string? QrCode { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}