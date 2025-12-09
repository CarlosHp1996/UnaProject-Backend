using UnaProject.Domain.Enums;

namespace UnaProject.Application.Models.Dtos
{
    public class UserDto
    {
        public Guid? Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Cpf { get; set; }
        public EnumGender? Gender { get; set; }
        public ICollection<AddressDto>? Addresses { get; set; }
    }
}
