using MediatR;
using UnaProject.Application.Models.Dtos;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Payments
{
    public record CreatePaymentCommand(
        Guid OrderId,
        decimal Amount,
        string PaymentMethod,
        string CustomerName,
        string? CustomerDocument,
        string? CustomerEmail,
        string? CustomerPhone,
        string? ReturnUrl,
        string? Metadata,
        // Preenchidos pelo controller a partir das claims do JWT — nunca do corpo.
        Guid UserId = default,
        bool IsAdmin = false
    ) : IRequest<Result<CreateAbacatePaymentResponse>>;
}
