using UnaProject.Domain.Enums;

namespace UnaProject.Application.Models.Dtos
{
    public class ProductAttributeDto
    {
        public Guid? Id { get; set; }
        public EnumCategory? Category { get; set; }
    }
}
