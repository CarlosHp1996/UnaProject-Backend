namespace UnaProject.Application.Models.Responses.Orders
{
    public class UpdateOrderResponse
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
