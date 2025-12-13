namespace UnaProject.Application.Models.Responses.Orders
{
    public class UpdateOrderItemResponse
    {
        public Guid OrderItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
