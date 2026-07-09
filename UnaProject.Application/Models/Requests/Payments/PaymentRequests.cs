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

        // Preenchidos pelo handler a partir das claims do JWT (NÃO do corpo da requisição).
        // O controller nunca copia estes campos do body para o command.
        public Guid UserId { get; set; }
        public bool IsAdmin { get; set; }
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
}