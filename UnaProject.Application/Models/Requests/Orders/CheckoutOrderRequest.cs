using UnaProject.Application.Models.Requests.Payments;

namespace UnaProject.Application.Models.Requests.Orders
{
    public class CheckoutOrderRequest
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
        public string? CompletionUrl { get; set; }
        public List<ProductBillingRequest>? Products { get; set; }
    }
}