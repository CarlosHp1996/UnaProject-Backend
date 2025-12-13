namespace UnaProject.Application.Models.Requests.Orders
{
    public class UpdateOrderRequest
    {
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public bool? IsActive { get; set; }
    }
}
