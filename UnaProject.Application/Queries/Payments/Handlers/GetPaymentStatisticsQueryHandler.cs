using MediatR;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Responses.Payments;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Queries.Payments.Handlers
{
    public class GetPaymentStatisticsQueryHandler : IRequestHandler<GetPaymentStatisticsQuery, Result<PaymentStatisticsResponse>>
    {
        private readonly IPaymentRepository _paymentRepository;

        public GetPaymentStatisticsQueryHandler(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<Result<PaymentStatisticsResponse>> Handle(GetPaymentStatisticsQuery request, CancellationToken cancellationToken)
        {
            var stats = await _paymentRepository.GetPaymentStatistics(request.UserId, cancellationToken);
            return Result<PaymentStatisticsResponse>.Success(stats);
        }
    }
}
