using UnaProject.Domain.Entities;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Services.Interfaces
{
    public interface IWebhookRetryService
    {
        Task<Result<bool>> IsWebhookProcessedAsync(string webhookEventId, string payloadHash);
        Task<Result<WebhookRetryLog>> LogWebhookAttemptAsync(
            Guid paymentId,
            string webhookEventId,
            string eventType,
            string payloadJson,
            string payloadHash);
        Task<Result<bool>> MarkWebhookAsProcessedAsync(string webhookEventId, string successMessage);
        Task<Result<bool>> LogWebhookFailureAsync(string webhookEventId, string errorMessage);
        Task<Result<List<WebhookRetryLog>>> GetPendingWebhooksForRetryAsync();
        Task<Result<bool>> ProcessWebhookRetryAsync(Guid webhookRetryLogId);
        DateTime CalculateNextRetryTime(int attemptCount);
    }
}