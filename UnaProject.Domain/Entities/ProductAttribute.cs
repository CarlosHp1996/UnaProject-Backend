using UnaProject.Domain.Enums;

namespace UnaProject.Domain.Entities
{
    public class ProductAttribute
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public EnumCategory? Category { get; set; }
        public EnumColor? Color { get; set; }

        // Navigation
        public virtual Product Product { get; set; }
    }
}
