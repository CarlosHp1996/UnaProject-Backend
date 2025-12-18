using Microsoft.Extensions.Logging;
using System.Text.Json;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Entities;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly IBaseRepository<PaymentAuditLog> _auditRepository;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            IBaseRepository<PaymentAuditLog> auditRepository,
            ILogger<AuditService> logger)
        {
            _auditRepository = auditRepository;
            _logger = logger;
        }

        public async Task<Result<PaymentAuditLog>> LogPaymentEventAsync(
            Guid paymentId,
            string eventType,
            string source,
            object eventData,
            string? userId = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? additionalInfo = null)
        {
            try
            {
                var auditLog = new PaymentAuditLog
                {
                    Id = Guid.NewGuid(),
                    PaymentId = paymentId,
                    EventType = eventType,
                    EventData = JsonSerializer.Serialize(eventData),
                    Source = source,
                    UserId = userId,
                    IPAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow,
                    AdditionalInfo = additionalInfo
                };

                await _auditRepository.AddAsync(auditLog);

                _logger.LogInformation(
                    "Payment audit log created. PaymentId: {PaymentId}, EventType: {EventType}, Source: {Source}",
                    paymentId, eventType, source);

                return Result<PaymentAuditLog>.Success(auditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating payment audit log. PaymentId: {PaymentId}, EventType: {EventType}",
                    paymentId, eventType);

                return Result<PaymentAuditLog>.Failure($"Error creating audit log: {ex.Message}");
            }
        }

        public async Task<Result<PaymentAuditLog>> LogWebhookReceivedAsync(
            Guid paymentId,
            string eventType,
            string payloadHash,
            object webhookData,
            string? ipAddress = null)
        {
            try
            {
                var additionalInfo = JsonSerializer.Serialize(new
                {
                    PayloadHash = payloadHash,
                    ReceivedAt = DateTime.UtcNow,
                    EventType = eventType
                });

                return await LogPaymentEventAsync(
                    paymentId,
                    "WebhookReceived",
                    "AbacatePay",
                    webhookData,
                    ipAddress: ipAddress,
                    additionalInfo: additionalInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error logging webhook received. PaymentId: {PaymentId}, EventType: {EventType}",
                    paymentId, eventType);

                return Result<PaymentAuditLog>.Failure($"Error registering received webhook: {ex.Message}");
            }
        }

        public async Task<Result<PaymentAuditLog>> LogPaymentStatusChangeAsync(
            Guid paymentId,
            string oldStatus,
            string newStatus,
            string source,
            string? userId = null)
        {
            try
            {
                var statusChangeData = new
                {
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    ChangedAt = DateTime.UtcNow
                };

                return await LogPaymentEventAsync(
                    paymentId,
                    "StatusChanged",
                    source,
                    statusChangeData,
                    userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error logging payment status change. PaymentId: {PaymentId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}",
                    paymentId, oldStatus, newStatus);

                return Result<PaymentAuditLog>.Failure($"Error registering status change: {ex.Message}");
            }
        }

        public async Task<Result<List<PaymentAuditLog>>> GetAuditLogsByPaymentAsync(Guid paymentId)
        {
            try
            {
                var allLogs = await _auditRepository.GetAll(null, null, "CreatedAt", false);
                var logs = allLogs.Result(out var totalCount);
                var paymentLogs = logs?.Where(x => x.PaymentId == paymentId)
                    .OrderByDescending(x => x.CreatedAt).ToList() ?? new List<PaymentAuditLog>();

                return Result<List<PaymentAuditLog>>.Success(paymentLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error retrieving audit logs for payment. PaymentId: {PaymentId}",
                    paymentId);

                return Result<List<PaymentAuditLog>>.Failure($"Error retrieving audit logs: {ex.Message}");
            }
        }

        public async Task<Result<List<PaymentAuditLog>>> GetAuditLogsByPeriodAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var allLogs = await _auditRepository.GetAll(null, null, "CreatedAt", false);
                var logs = allLogs.Result(out var totalCount);
                var periodLogs = logs?.Where(x =>
                    x.CreatedAt >= startDate && x.CreatedAt <= endDate)
                    .OrderByDescending(x => x.CreatedAt).ToList() ?? new List<PaymentAuditLog>();

                return Result<List<PaymentAuditLog>>.Success(periodLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error retrieving audit logs by period. StartDate: {StartDate}, EndDate: {EndDate}",
                    startDate, endDate);

                return Result<List<PaymentAuditLog>>.Failure($"Error retrieving logs by period: {ex.Message}");
            }
        }
    }
}