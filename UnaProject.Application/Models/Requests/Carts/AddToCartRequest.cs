namespace UnaProject.Application.Models.Requests.Carts
{
    public class AddToCartRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Size { get; set; }
        //public string SessionId { get; set; }
    }
}
