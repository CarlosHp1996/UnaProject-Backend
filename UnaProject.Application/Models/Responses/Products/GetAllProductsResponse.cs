using UnaProject.Application.Models.Dtos;

namespace UnaProject.Application.Models.Responses.Products
{
    public class GetAllProductsResponse
    {
        public List<ProductListItemDto>? Products { get; set; } = new List<ProductListItemDto>();
        public PaginationDto? Pagination { get; set; }
        public FiltersDto? Filters { get; set; }
    }
}
