namespace UnaProject.Application.Models.Dtos
{
    public class WebhookEventDto
    {
        public string? Event { get; set; }
        public WebhookDataDto? Data { get; set; }
    }

    public class WebhookDataDto
    {
        public string? BillingId { get; set; }
        public string? Status { get; set; }
        public decimal? PlatformFee { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Metadata { get; set; }
    }

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
    }
}