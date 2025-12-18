using MediatR;
using UnaProject.Application.Models.Dtos;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Payments
{
    public record GetPaymentStatusQuery(Guid PaymentId) : IRequest<Result<PaymentStatusDto>>;
}