using UnaProject.Application.Models.Dtos;

namespace UnaProject.Application.Models.Responses.Orders
{
    public class GetAllOrdersResponse
    {
        public List<OrderDto> Orders { get; set; } = new List<OrderDto>();
        public PaginationDto Pagination { get; set; }
        public ICollection<AddressDto>? Addresses { get; set; }
    }
}
