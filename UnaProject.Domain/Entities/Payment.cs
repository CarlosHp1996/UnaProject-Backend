namespace UnaProject.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // "stripe", "paypal", "abacatepay", etc.
        public string Status { get; set; } = string.Empty; // "pending", "succeeded", "failed", etc.
        public string Currency { get; set; } = "BRL"; // Default currency
        public string? TransactionId { get; set; } // External payment ID (e.g., Stripe's PaymentIntent ID)
        public string? ClientSecret { get; set; } // For frontend integration with Stripe
        public string? ReceiptUrl { get; set; } // Payment receipt URL (provided by payment provider)
        public string? ErrorMessage { get; set; } // Store error messages if payment fails
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; } // When payment was processed
        public DateTime PaymentDate { get; set; }

        // === AbacatePay specific fields ===
        public string? BillingId { get; set; }           // Payment ID on AbacatePay
        public string? PaymentUrl { get; set; }          // Payment URL
        public string? QrCode { get; set; }              // PIX QR Code
        public string? QrCodeImage { get; set; }         // QR Code image (base64)
        public DateTime? ExpiresAt { get; set; }         // Payment expiration
        public string? AbacateStatus { get; set; }       // AbacatePay specific status
        public decimal? Fee { get; set; }                // Fee charged by AbacatePay
        public string? PaymentToken { get; set; }        // Unique payment token
        public string? CustomerEmail { get; set; }       // Customer email
        public string? AbacateKind { get; set; }         // "PIX", "CARD", etc.
        public bool? DevMode { get; set; }              // Created in development mode
        public string? ExternalId { get; set; }         // External ID for idempotency

        // Propriedades adicionais para AbacatePay
        public string? AbacateMethod { get; set; }       // AbacatePay payment method
        public string? AbacateFrequency { get; set; }    // Billing frequency
        public string? AbacateFeeType { get; set; }      // Fee type
        public string? Metadata { get; set; }            // Additional metadata

        // Navigation property
        public virtual Order Order { get; set; } = null!;
    }
}
