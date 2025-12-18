using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Services.Interfaces
{
    public interface IPaymentNotificationService
    {
        Task<Result<bool>> SendPaymentCreatedNotificationAsync(Guid paymentId, string customerEmail);
        Task<Result<bool>> SendPaymentConfirmedNotificationAsync(Guid paymentId, string customerEmail);
        Task<Result<bool>> SendPaymentCancelledNotificationAsync(Guid paymentId, string customerEmail);
        Task<Result<bool>> SendPaymentExpiredNotificationAsync(Guid paymentId, string customerEmail);
        Task<Result<bool>> SendPaymentFailedAdminNotificationAsync(Guid paymentId, string errorMessage);
        Task<Result<bool>> SendWebhookFailedAdminNotificationAsync(Guid paymentId, string webhookEvent, string errorMessage, int attemptCount);
    }
}