using UnaProject.Domain.Enums;

namespace UnaProject.Application.Models.Dtos
{
    public class PaymentStatusDto
    {
        public Guid PaymentId { get; set; }
        public string? BillingId { get; set; }
        public string Status { get; set; } = string.Empty;
        public PaymentStatusAbacate? AbacateStatus { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal? Fee { get; set; }
        public string? PaymentUrl { get; set; }
        public string? QrCode { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public bool? DevMode { get; set; }
    }
}