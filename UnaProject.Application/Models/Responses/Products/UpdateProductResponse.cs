using UnaProject.Application.Models.Dtos;

namespace UnaProject.Application.Models.Responses.Products
{
    public class UpdateProductResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ProductAttributeDto> Attributes { get; set; } = new List<ProductAttributeDto>();
        public Guid InventoryId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
