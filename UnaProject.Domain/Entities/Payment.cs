namespace UnaProject.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } // "stripe", "paypal", etc.
        public string Status { get; set; } // "pending", "succeeded", "failed", etc.
        public string Currency { get; set; } = "BRL"; // Default currency
        public string? TransactionId { get; set; } // External payment ID (e.g., Stripe's PaymentIntent ID)
        public string? ClientSecret { get; set; } // For frontend integration with Stripe
        public string? ReceiptUrl { get; set; } // Payment receipt URL (provided by payment provider)
        public string? ErrorMessage { get; set; } // Store error messages if payment fails
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; } // When payment was processed
        public DateTime PaymentDate { get; set; }

        // Navigation property
        public virtual Order Order { get; set; }
    }
}
