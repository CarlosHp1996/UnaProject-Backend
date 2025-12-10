using UnaProject.Domain.Enums;

namespace UnaProject.Application.Models.Requests.Products
{
    public class ProductAttributeRequest
    {
        public EnumCategory? Category { get; set; }
    }
}
