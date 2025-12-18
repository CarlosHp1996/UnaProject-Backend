using UnaProject.Domain.Entities;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Services.Interfaces
{
    public interface IAuditService
    {
        Task<Result<PaymentAuditLog>> LogPaymentEventAsync(
            Guid paymentId,
            string eventType,
            string source,
            object eventData,
            string? userId = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? additionalInfo = null);
        Task<Result<PaymentAuditLog>> LogWebhookReceivedAsync(
            Guid paymentId,
            string eventType,
            string payloadHash,
            object webhookData,
            string? ipAddress = null);
        Task<Result<PaymentAuditLog>> LogPaymentStatusChangeAsync(
            Guid paymentId,
            string oldStatus,
            string newStatus,
            string source,
            string? userId = null);
        Task<Result<List<PaymentAuditLog>>> GetAuditLogsByPaymentAsync(Guid paymentId);
        Task<Result<List<PaymentAuditLog>>> GetAuditLogsByPeriodAsync(DateTime startDate, DateTime endDate);
    }
}