using Microsoft.Extensions.Logging;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Entities;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Services
{
    public class WebhookRetryService : IWebhookRetryService
    {
        private readonly IBaseRepository<WebhookRetryLog> _retryRepository;
        private readonly ILogger<WebhookRetryService> _logger;

        public WebhookRetryService(
            IBaseRepository<WebhookRetryLog> retryRepository,
            ILogger<WebhookRetryService> logger)
        {
            _retryRepository = retryRepository;
            _logger = logger;
        }

        public async Task<Result<bool>> IsWebhookProcessedAsync(string webhookEventId, string payloadHash)
        {
            try
            {
                var allWebhooks = await _retryRepository.GetAll(null, null, "Id", true);
                var webhookLogs = allWebhooks.Result(out var totalCount);

                var existingWebhook = webhookLogs?.FirstOrDefault(x =>
                    x.WebhookEventId == webhookEventId && x.PayloadHash == payloadHash);

                var isProcessed = existingWebhook?.IsProcessed ?? false;

                _logger.LogInformation(
                    "Webhook idempotency check. EventId: {EventId}, PayloadHash: {PayloadHash}, IsProcessed: {IsProcessed}",
                    webhookEventId, payloadHash, isProcessed);

                return Result<bool>.Success(isProcessed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error checking webhook idempotency. EventId: {EventId}",
                    webhookEventId);

                return Result<bool>.Failure($"Error verifying webhook idempotency: {ex.Message}");
            }
        }

        public async Task<Result<WebhookRetryLog>> LogWebhookAttemptAsync(Guid paymentId, string webhookEventId, string eventType, string payloadJson, string payloadHash)
        {
            try
            {
                var allWebhooks = await _retryRepository.GetAll(null, null, "Id", true);
                var existingLogs = allWebhooks.Result(out var totalCount);

                var existingAttempt = existingLogs?.FirstOrDefault(x =>
                    x.WebhookEventId == webhookEventId && x.PayloadHash == payloadHash);

                if (existingAttempt != null)
                {
                    _logger.LogInformation(
                        "Webhook attempt already logged. EventId: {EventId}",
                        webhookEventId);
                    return Result<WebhookRetryLog>.Success(existingAttempt);
                }

                var webhookLog = new WebhookRetryLog
                {
                    Id = Guid.NewGuid(),
                    PaymentId = paymentId,
                    WebhookEventId = webhookEventId,
                    EventType = eventType,
                    AttemptCount = 1,
                    PayloadJson = payloadJson,
                    PayloadHash = payloadHash,
                    FirstAttemptAt = DateTime.UtcNow,
                    LastAttemptAt = DateTime.UtcNow,
                    IsProcessed = false
                };

                await _retryRepository.AddAsync(webhookLog);

                _logger.LogInformation(
                    "Webhook attempt logged. EventId: {EventId}, PaymentId: {PaymentId}",
                    webhookEventId, paymentId);

                return Result<WebhookRetryLog>.Success(webhookLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error logging webhook attempt. EventId: {EventId}",
                    webhookEventId);

                return Result<WebhookRetryLog>.Failure($"Error logging webhook attempt: {ex.Message}");
            }
        }

        public async Task<Result<bool>> MarkWebhookAsProcessedAsync(string webhookEventId, string successMessage)
        {
            try
            {
                var allRetryLogs = await _retryRepository.GetAll(null, null, "Id", true);
                var retryLogs = allRetryLogs.Result(out var totalCount);

                var existingRetryLog = retryLogs?.FirstOrDefault(x =>
                    x.WebhookEventId == webhookEventId);

                if (existingRetryLog != null)
                {
                    existingRetryLog.IsProcessed = true;
                    existingRetryLog.ProcessedAt = DateTime.UtcNow;
                    existingRetryLog.SuccessMessage = successMessage;
                    existingRetryLog.LastErrorMessage = null;

                    await _retryRepository.UpdateAsync(existingRetryLog);

                    _logger.LogInformation(
                        "Webhook marked as processed. EventId: {EventId}",
                        webhookEventId);
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error marking webhook as processed. EventId: {EventId}",
                    webhookEventId);

                return Result<bool>.Failure($"Error marking webhook as processed: {ex.Message}");
            }
        }

        public async Task<Result<bool>> LogWebhookFailureAsync(string webhookEventId, string errorMessage)
        {
            try
            {
                var allRetryLogs = await _retryRepository.GetAll(null, null, "Id", true);
                var retryLogs = allRetryLogs.Result(out var totalCount);

                var existingRetryLog = retryLogs?.FirstOrDefault(x =>
                    x.WebhookEventId == webhookEventId);

                if (existingRetryLog != null)
                {
                    existingRetryLog.LastErrorMessage = errorMessage;
                    existingRetryLog.IsProcessed = false;
                    existingRetryLog.LastAttemptAt = DateTime.UtcNow;
                    existingRetryLog.AttemptCount++;
                    existingRetryLog.NextRetryAt = CalculateNextRetryTime(existingRetryLog.AttemptCount);

                    await _retryRepository.UpdateAsync(existingRetryLog);
                }
                else
                {
                    var failureLog = new WebhookRetryLog
                    {
                        Id = Guid.NewGuid(),
                        WebhookEventId = webhookEventId,
                        LastErrorMessage = errorMessage,
                        FirstAttemptAt = DateTime.UtcNow,
                        LastAttemptAt = DateTime.UtcNow,
                        AttemptCount = 1,
                        IsProcessed = false,
                        EventType = "failure",
                        NextRetryAt = CalculateNextRetryTime(1)
                    };

                    await _retryRepository.AddAsync(failureLog);
                }

                _logger.LogWarning(
                    "Webhook failure logged. EventId: {EventId}, Error: {Error}",
                    webhookEventId, errorMessage);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error logging webhook failure. EventId: {EventId}",
                    webhookEventId);

                return Result<bool>.Failure($"Error logging webhook failure: {ex.Message}");
            }
        }

        public async Task<Result<List<WebhookRetryLog>>> GetPendingWebhooksForRetryAsync()
        {
            try
            {
                var allRetryLogs = await _retryRepository.GetAll(null, null, "FirstAttemptAt", false);
                var retryLogs = allRetryLogs.Result(out var totalCount);

                var pendingRetries = retryLogs?
                    .Where(x => !x.IsProcessed && !string.IsNullOrEmpty(x.LastErrorMessage))
                    .Where(x => x.NextRetryAt.HasValue && x.NextRetryAt <= DateTime.UtcNow)
                    .OrderBy(x => x.FirstAttemptAt)
                    .ToList() ?? new List<WebhookRetryLog>();

                _logger.LogInformation(
                    "Retrieved {Count} pending webhook retries",
                    pendingRetries.Count);

                return Result<List<WebhookRetryLog>>.Success(pendingRetries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending retries");
                return Result<List<WebhookRetryLog>>.Failure($"Error retrieving pending retries: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ProcessWebhookRetryAsync(Guid webhookRetryLogId)
        {
            try
            {
                var webhookLog = await _retryRepository.GetById(webhookRetryLogId);
                if (webhookLog == null)
                {
                    return Result<bool>.Failure("Webhook retry log not found");
                }

                webhookLog.AttemptCount += 1;
                webhookLog.LastAttemptAt = DateTime.UtcNow;
                webhookLog.NextRetryAt = CalculateNextRetryTime(webhookLog.AttemptCount);

                await _retryRepository.UpdateAsync(webhookLog);

                _logger.LogInformation(
                    "Webhook retry processed. EventId: {EventId}, Attempt: {AttemptNumber}",
                    webhookLog.WebhookEventId, webhookLog.AttemptCount);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing webhook retry. LogId: {LogId}",
                    webhookRetryLogId);

                return Result<bool>.Failure($"Error processing webhook retry: {ex.Message}");
            }
        }

        public DateTime CalculateNextRetryTime(int attemptCount)
        {
            // Exponential backoff: 2^attemptNumber minutes, max 60 minutes
            var delayMinutes = Math.Min(Math.Pow(2, attemptCount), 60);
            return DateTime.UtcNow.AddMinutes(delayMinutes);
        }
    }
}