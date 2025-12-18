using MediatR;
using UnaProject.Application.Models.Dtos;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Payments
{
    public record CreateAbacatePaymentCommand(
        Guid OrderId,
        decimal Amount,
        string PaymentMethod,
        string CustomerName,
        string? CustomerDocument,
        string? CustomerEmail,
        string? CustomerPhone,
        string? ReturnUrl,
        string? Metadata
    ) : IRequest<Result<CreateAbacatePaymentResponse>>;
}