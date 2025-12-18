using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UnaProject.Application.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace UnaProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly IWebhookRetryService _webhookRetryService;
        private readonly ILogger<AuditController> _logger;

        public AuditController(
            IAuditService auditService,
            IWebhookRetryService webhookRetryService,
            ILogger<AuditController> logger)
        {
            _auditService = auditService;
            _webhookRetryService = webhookRetryService;
            _logger = logger;
        }

        [HttpGet("payment/{paymentId}")]
        public async Task<IActionResult> GetAuditLogsByPayment([FromRoute] Guid paymentId)
        {
            try
            {
                var result = await _auditService.GetAuditLogsByPaymentAsync(paymentId);

                if (!result.HasSuccess)
                    return BadRequest(new { error = result.HasError });

                return Ok(new
                {
                    paymentId,
                    totalLogs = result.Value.Count,
                    logs = result.Value.Select(log => new
                    {
                        id = log.Id,
                        eventType = log.EventType,
                        source = log.Source,
                        createdAt = log.CreatedAt,
                        userId = log.UserId,
                        ipAddress = log.IPAddress,
                        additionalInfo = log.AdditionalInfo
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for payment {PaymentId}", paymentId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("period")]
        public async Task<IActionResult> GetAuditLogsByPeriod(
            [FromQuery, Required] DateTime startDate,
            [FromQuery, Required] DateTime endDate)
        {
            try
            {
                if (endDate < startDate)
                    return BadRequest(new { error = "The end date must be later than the start date." });

                var timeSpan = endDate - startDate;
                if (timeSpan.TotalDays > 31)
                    return BadRequest(new { error = "The period cannot be longer than 31 days." });

                var result = await _auditService.GetAuditLogsByPeriodAsync(startDate, endDate);

                if (!result.HasSuccess)
                    return BadRequest(new { error = result.HasError });

                return Ok(new
                {
                    startDate,
                    endDate,
                    totalLogs = result.Value.Count,
                    logs = result.Value.Select(log => new
                    {
                        id = log.Id,
                        paymentId = log.PaymentId,
                        eventType = log.EventType,
                        source = log.Source,
                        createdAt = log.CreatedAt,
                        userId = log.UserId,
                        ipAddress = log.IPAddress
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for period {StartDate} to {EndDate}", startDate, endDate);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("webhooks/pending")]
        public async Task<IActionResult> GetPendingWebhooks()
        {
            try
            {
                var result = await _webhookRetryService.GetPendingWebhooksForRetryAsync();

                if (!result.HasSuccess)
                {
                    return BadRequest(new { error = result.HasError });
                }

                return Ok(new
                {
                    totalPending = result.Value.Count,
                    webhooks = result.Value.Select(webhook => new
                    {
                        id = webhook.Id,
                        paymentId = webhook.PaymentId,
                        webhookEventId = webhook.WebhookEventId,
                        eventType = webhook.EventType,
                        attemptCount = webhook.AttemptCount,
                        firstAttemptAt = webhook.FirstAttemptAt,
                        lastAttemptAt = webhook.LastAttemptAt,
                        nextRetryAt = webhook.NextRetryAt,
                        lastErrorMessage = webhook.LastErrorMessage,
                        isProcessed = webhook.IsProcessed
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending webhooks");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("webhooks/{webhookRetryLogId}/retry")]
        public async Task<IActionResult> ForceWebhookRetry([FromRoute] Guid webhookRetryLogId)
        {
            try
            {
                _logger.LogInformation("Manual webhook retry requested for {WebhookRetryLogId}", webhookRetryLogId);

                var result = await _webhookRetryService.ProcessWebhookRetryAsync(webhookRetryLogId);

                if (!result.HasError)
                {
                    return BadRequest(new { error = result.HasError });
                }

                return Ok(new
                {
                    message = "Webhook retried successfully",
                    webhookRetryLogId,
                    processedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing manual webhook retry {WebhookRetryLogId}", webhookRetryLogId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetAuditStatistics()
        {
            try
            {
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-7); // Last 7 days

                var result = await _auditService.GetAuditLogsByPeriodAsync(startDate, endDate);

                if (!result.HasSuccess)
                    return BadRequest(new { error = result.HasError });

                var logs = result.Value;
                var webhooksPendingResult = await _webhookRetryService.GetPendingWebhooksForRetryAsync();

                return Ok(new
                {
                    period = new { startDate, endDate },
                    totalLogsLast7Days = logs.Count,
                    logsByEventType = logs
                        .GroupBy(l => l.EventType)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    logsBySource = logs
                        .GroupBy(l => l.Source)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    webhooksPending = webhooksPendingResult.HasSuccess ? webhooksPendingResult.Value.Count : 0,
                    lastActivity = logs.OrderByDescending(l => l.CreatedAt).FirstOrDefault()?.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit statistics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}