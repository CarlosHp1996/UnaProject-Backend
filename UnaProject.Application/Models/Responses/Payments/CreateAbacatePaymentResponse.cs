namespace UnaProject.Application.Models.Responses.Payments
{
    public class CreateAbacatePaymentResponse
    {
        public Guid PaymentId { get; set; }
        public string BillingId { get; set; }
        public string PaymentUrl { get; set; }
        public string? QrCode { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}