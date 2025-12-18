namespace UnaProject.Domain.Entities
{
    public class WebhookRetryLog
    {
        public Guid Id { get; set; }
        public Guid PaymentId { get; set; }
        public string WebhookEventId { get; set; } = string.Empty; // Unique webhook event ID
        public string EventType { get; set; } = string.Empty; // billing.paid, billing.failed, etc.
        public string PayloadHash { get; set; } = string.Empty; // Payload hash for idempotency
        public int AttemptCount { get; set; }
        public bool IsProcessed { get; set; }
        public DateTime FirstAttemptAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public string? LastErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public string PayloadJson { get; set; } = string.Empty; // Original payload for retry

        // Relationship
        public Payment Payment { get; set; } = null!;
    }
}