namespace UnaProject.Application.Models.Dtos
{
    public class ProductListItemDto
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? Benefit { get; set; }
        public string? NutritionalInfo { get; set; }
        public bool? IsActive { get; set; }
        public List<ProductAttributeDto>? Attributes { get; set; } = new List<ProductAttributeDto>();
    }
}
