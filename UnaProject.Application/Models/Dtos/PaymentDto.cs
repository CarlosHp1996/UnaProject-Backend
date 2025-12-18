namespace UnaProject.Application.Models.Dtos
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public decimal? Fee { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? BillingId { get; set; }
        public string? PaymentUrl { get; set; }
        public string? QrCode { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public bool DevMode { get; set; }
        public string? AbacateStatus { get; set; }
        public string? AbacateFrequency { get; set; }
        public string? AbacateMethod { get; set; }
        public string? AbacateFeeType { get; set; }
        public string? Metadata { get; set; }

        // Additional properties for compatibility
        public string? TransactionId { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}