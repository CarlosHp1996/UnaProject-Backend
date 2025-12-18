using System.ComponentModel.DataAnnotations;

namespace UnaProject.Application.Models.Requests.Payments
{
    public class UpdatePaymentRequest
    {
        [Required]
        public Guid PaymentId { get; set; }
        public string? Status { get; set; }
        [StringLength(1000)]
        public string? ErrorMessage { get; set; }
        [Url]
        public string? ReceiptUrl { get; set; }
        public DateTime? ProcessedAt { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? Fee { get; set; }
    }
}