using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UnaProject.Application.Services.Interfaces;

namespace UnaProject.Application.Services.Background
{
    public class WebhookRetryBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WebhookRetryBackgroundService> _logger;
        private readonly WebhookRetryBackgroundOptions _options;

        public WebhookRetryBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<WebhookRetryBackgroundOptions> options,
            ILogger<WebhookRetryBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Webhook Retry Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingWebhooks(stoppingToken);

                    // Wait for the configured interval before the next execution.
                    await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Webhook Retry Background Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in webhook retry background service");

                    // Em caso de erro, aguardar um pouco antes de tentar novamente
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("Webhook Retry Background Service stopped");
        }

        private async Task ProcessPendingWebhooks(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var webhookRetryService = scope.ServiceProvider.GetRequiredService<IWebhookRetryService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IPaymentNotificationService>();

            try
            {
                var pendingWebhooksResult = await webhookRetryService.GetPendingWebhooksForRetryAsync();

                if (!pendingWebhooksResult.HasSuccess)
                {
                    _logger.LogError("Failed to get pending webhooks: {Error}", pendingWebhooksResult.ErrorMessage);
                    return;
                }

                var pendingWebhooks = pendingWebhooksResult.Value;

                if (!pendingWebhooks?.Any() ?? true)
                {
                    _logger.LogDebug("No pending webhooks found for retry");
                    return;
                }

                _logger.LogInformation("Processing {Count} pending webhooks", pendingWebhooks.Count);

                foreach (var webhookLog in pendingWebhooks)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await ProcessSingleWebhookRetry(webhookLog, webhookRetryService, notificationService);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending webhooks");
            }
        }

        private async Task ProcessSingleWebhookRetry(
            Domain.Entities.WebhookRetryLog webhookLog,
            IWebhookRetryService webhookRetryService,
            IPaymentNotificationService notificationService)
        {
            try
            {
                _logger.LogInformation(
                    "Processing webhook retry. EventId: {EventId}, Attempt: {Attempt}",
                    webhookLog.WebhookEventId, webhookLog.AttemptCount + 1);

                var retryResult = await webhookRetryService.ProcessWebhookRetryAsync(webhookLog.Id);

                if (retryResult.HasSuccess)
                {
                    _logger.LogInformation(
                        "Webhook retry processed successfully. EventId: {EventId}",
                        webhookLog.WebhookEventId);

                    await webhookRetryService.MarkWebhookAsProcessedAsync(
                        webhookLog.WebhookEventId,
                        $"Successfully reprocessed in background after {webhookLog.AttemptCount} attempts");
                }
                else
                {
                    _logger.LogWarning(
                        "Webhook retry failed. EventId: {EventId}, Error: {Error}",
                        webhookLog.WebhookEventId, retryResult.ErrorMessage);

                    await webhookRetryService.LogWebhookFailureAsync(
                        webhookLog.WebhookEventId,
                        retryResult.ErrorMessage ?? "Unknown error in retry");

                    // If exceeded maximum attempts, notify admin
                    if (webhookLog.AttemptCount >= 5)
                    {
                        await notificationService.SendWebhookFailedAdminNotificationAsync(
                            webhookLog.PaymentId,
                            webhookLog.EventType,
                            retryResult.ErrorMessage ?? "Maximum attempts exceeded",
                            webhookLog.AttemptCount);

                        _logger.LogError(
                            "Webhook exceeded maximum retry attempts. EventId: {EventId}, PaymentId: {PaymentId}",
                            webhookLog.WebhookEventId, webhookLog.PaymentId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing individual webhook retry. EventId: {EventId}",
                    webhookLog.WebhookEventId);

                await webhookRetryService.LogWebhookFailureAsync(
                    webhookLog.WebhookEventId,
                    $"Error in background service: {ex.Message}");
            }
        }
    }

    public class WebhookRetryBackgroundOptions
    {
        public const string SectionName = "WebhookRetryBackground";
        public int IntervalMinutes { get; set; } = 5;
        public bool Enabled { get; set; } = true;
        public int MaxWebhooksPerBatch { get; set; } = 50;
        public int WebhookTimeoutSeconds { get; set; } = 30;
    }
}