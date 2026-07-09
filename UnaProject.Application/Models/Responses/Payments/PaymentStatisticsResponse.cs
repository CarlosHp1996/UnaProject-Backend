namespace UnaProject.Application.Models.Responses.Payments
{
    public class PaymentStatisticsResponse
    {
        public int TotalPayments { get; set; }
        public int SuccessfulPayments { get; set; }
        public int FailedPayments { get; set; }
        public int PendingPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AveragePayment { get; set; }
    }
}
