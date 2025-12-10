using UnaProject.Domain.Enums;

namespace UnaProject.Application.Models.Filters
{
    public class GetProductsRequestFilter : BaseRequestFilter
    {
        public string? Name { get; set; }
        public List<EnumCategory>? CategoryIds { get; set; } = new List<EnumCategory>();
        public List<int>? QuantityRanges { get; set; } = new List<int>();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsActive { get; set; }
    }
}
