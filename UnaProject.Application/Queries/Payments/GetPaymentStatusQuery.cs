using MediatR;
using UnaProject.Application.Models.Dtos;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Payments
{
    // UserId/IsAdmin são preenchidos pelo controller a partir das claims do JWT — nunca do corpo.
    public record GetPaymentStatusQuery(Guid PaymentId, Guid UserId = default, bool IsAdmin = false) : IRequest<Result<PaymentStatusDto>>;
}
