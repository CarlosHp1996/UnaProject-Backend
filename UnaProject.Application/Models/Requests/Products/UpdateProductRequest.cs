using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using UnaProject.Application.Models.Dtos;

namespace UnaProject.Application.Models.Requests.Products
{
    public class UpdateProductRequest
    {

        [StringLength(100)]
        public string? Name { get; set; }
        public string? Description { get; set; }
        [Range(0.01, double.MaxValue)]
        public decimal? Price { get; set; }
        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }
        public IFormFile? ImageUrl { get; set; }
        public bool? IsActive { get; set; }
        public List<ProductAttributeDto>? Attributes { get; set; } = new List<ProductAttributeDto>();
        public Guid? InventoryId { get; set; }
    }
}
