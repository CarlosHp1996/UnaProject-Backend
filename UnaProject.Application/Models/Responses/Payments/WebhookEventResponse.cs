namespace UnaProject.Application.Models.Responses.Payments
{
    public class WebhookEventResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public bool DevMode { get; set; }
        public WebhookEventData Data { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class WebhookEventData
    {
        public WebhookPaymentData? Payment { get; set; }
        public WebhookPixData? PixQrCode { get; set; }
        public WebhookTransactionData? Transaction { get; set; }
    }

    public class WebhookPaymentData
    {
        public int Amount { get; set; }
        public int Fee { get; set; }
        public string Method { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
    }

    public class WebhookPixData
    {
        public string Id { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class WebhookTransactionData
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool DevMode { get; set; }
        public string? ReceiptUrl { get; set; }
        public string Kind { get; set; } = string.Empty;
        public int Amount { get; set; }
        public int PlatformFee { get; set; }
        public string? ExternalId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}