namespace UnaProject.Domain.Entities
{
    public class PaymentAuditLog
    {
        public Guid Id { get; set; }
        public Guid PaymentId { get; set; }
        public string EventType { get; set; } = string.Empty; // WebhookReceived, StatusChanged, PaymentCreated, etc.
        public string EventData { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; // AbacatePay, System, User
        public string? UserId { get; set; } // For user-initiated events
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AdditionalInfo { get; set; } // Additional information in JSON
        // Relationship
        public Payment Payment { get; set; } = null!;
    }
}