using UnaProject.Domain.Enums;

namespace UnaProject.Application.Models.Dtos
{
    public class AddressDto
    {
        public Guid? Id { get; set; }
        public string? Street { get; set; }
        public string? CompletName { get; set; }
        public string? City { get; set; }
        public EnumState? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Neighborhood { get; set; }
        public string? Number { get; set; }
        public string? Complement { get; set; }
        public bool? MainAddress { get; set; }
    }
}
