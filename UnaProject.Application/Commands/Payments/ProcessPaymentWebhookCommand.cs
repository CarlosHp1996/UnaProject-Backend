using MediatR;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Payments
{
    public record ProcessPaymentWebhookCommand(
        string Provider,
        string WebhookPayload,
        string? Signature,
        string? WebhookSecret,
        string? IPAddress
    ) : IRequest<ResultValue<bool>>;
}
