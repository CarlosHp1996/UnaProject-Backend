namespace UnaProject.Application.Models.Dtos
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
