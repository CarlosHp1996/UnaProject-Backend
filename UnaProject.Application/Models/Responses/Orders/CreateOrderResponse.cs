namespace UnaProject.Application.Models.Responses.Orders
{
    public class CreateOrderResponse
    {
        public Guid OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public int OrderNumber { get; set; }
        public bool? IsActive { get; set; }
    }
}
