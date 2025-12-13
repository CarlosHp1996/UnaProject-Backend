namespace UnaProject.Application.Models.Requests.Orders
{
    public class CreateOrderRequest
    {
        public Guid UserId { get; set; }
        public Guid AddressId { get; set; }
        public string PaymentMethod { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();
    }

    public class OrderItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
