using System.ComponentModel.DataAnnotations;
using UnaProject.Application.Models.Dtos;
using UnaProject.Domain.Enums;

namespace UnaProject.Application.Models.Requests.Security
{
    public class UpdateUserRequest
    {
        public Guid? Id { get; set; }

        public string? Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Password { get; set; }

        public string? Cpf { get; set; }

        public EnumGender? Gender { get; set; }

        public ICollection<AddressDto>? Addresses { get; set; }

        public bool IsPasswordRecovery { get; set; } = false;
    }
}
