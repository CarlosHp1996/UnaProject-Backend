namespace UnaProject.Application.Models.Requests.Payments
{
    public class CreateAbacatePaymentRequest
    {
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerDocument { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? ReturnUrl { get; set; }
        public string? Metadata { get; set; }
        public List<ProductBillingRequest>? Products { get; set; }
        public string? CompletionUrl { get; set; }
    }

    public class CancelPaymentRequest
    {
        public string? Reason { get; set; }
    }

    public class ProductBillingRequest
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class CreateBillingRequest
    {
        public decimal Amount { get; set; }
        public List<string>? Methods { get; set; }
        public CustomerRequest? Customer { get; set; }
        public List<ProductRequest>? Products { get; set; }
        public string? ReturnUrl { get; set; }
        public string? Metadata { get; set; }
    }

    public class CustomerRequest
    {
        public string? Name { get; set; }
        public string? Document { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class ProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? ExternalId { get; set; }
    }
}