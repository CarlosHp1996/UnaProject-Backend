namespace UnaProject.Application.Models.Responses.Payments
{
    public class CreateBillingResponse
    {
        public string Id { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? QrCode { get; set; }
        public string? QrCodeImage { get; set; }
        public string? Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool DevMode { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Frequency { get; set; }
        public List<string>? Methods { get; set; }
        public string? PixQrCode { get; set; }
    }

    public class BillingStatusResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal? PlatformFee { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Metadata { get; set; }
    }
}