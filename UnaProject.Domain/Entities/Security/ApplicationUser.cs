using Microsoft.AspNetCore.Identity;
using UnaProject.Domain.Enums;

namespace UnaProject.Domain.Entities.Security
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? Cpf { get; set; }
        public EnumGender? Gender { get; set; }
        public virtual ICollection<Address> Addresses { get; set; }

        public ApplicationUser()
        {
            Addresses = new HashSet<Address>();
        }
    }
}
