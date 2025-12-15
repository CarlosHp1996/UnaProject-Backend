namespace UnaProject.Domain.Entities
{
    public class Cart
    {
        public required string UserId { get; set; } // This could be the sessionId for non-logged-in users
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
        public int TotalItems => Items.Sum(i => i.Quantity);
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
