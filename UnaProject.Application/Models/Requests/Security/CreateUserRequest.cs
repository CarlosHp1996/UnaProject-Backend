using UnaProject.Application.Models.Dtos;
using UnaProject.Domain.Enums;

namespace UnaProject.Application.Models.Requests.Security
{
    public class CreateUserRequest
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Cpf { get; set; }
        public EnumGender? Gender { get; set; }
        public ICollection<AddressDto>? Addresses { get; set; }
    }
}
