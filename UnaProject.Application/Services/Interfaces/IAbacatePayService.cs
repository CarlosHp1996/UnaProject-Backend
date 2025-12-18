using UnaProject.Application.Models.Requests.Payments;
using UnaProject.Application.Models.Responses.Payments;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Services.Interfaces
{
    public interface IAbacatePayService
    {
        Task<Result<CreateBillingResponse>> CreateBillingAsync(CreateBillingRequest request);
        Task<Result<BillingStatusResponse>> GetBillingStatusAsync(string billingId);
        Task<ResultValue<bool>> CancelBillingAsync(string billingId);
        Task<ResultValue<bool>> ValidateWebhookSignatureAsync(string payload, string signature);
        Task<ResultValue<bool>> ProcessWebhookEventAsync(string webhookPayload);
        Task<ResultValue<bool>> IsBillingValidAsync(string billingId);
    }
}