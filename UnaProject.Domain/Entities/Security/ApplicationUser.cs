using Microsoft.AspNetCore.Identity;
using UnaProject.Domain.Enums;

namespace UnaProject.Domain.Entities.Security
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? Cpf { get; set; }
        public EnumGender? Gender { get; set; }
        public virtual ICollection<Address> Addresses { get; set; }

        // === SOCIAL LOGIN PROPERTIES ===
        public string? GoogleId { get; set; }
        public string? FacebookId { get; set; }
        public string? ProfilePicture { get; set; }
        public DateTime? LastSocialLogin { get; set; }
        public bool EmailVerified { get; set; } = false;

        public ApplicationUser()
        {
            Addresses = new HashSet<Address>();
        }
    }
}
