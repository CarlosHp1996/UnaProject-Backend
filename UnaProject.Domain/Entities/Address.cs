using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UnaProject.Domain.Entities.Security;
using UnaProject.Domain.Enums;

namespace UnaProject.Domain.Entities
{
    public class Address
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Street { get; set; } = string.Empty;

        [Required]
        public string CompletName { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public EnumState State { get; set; }

        [Required]
        public string ZipCode { get; set; } = string.Empty;

        [Required]
        public string Neighborhood { get; set; } = string.Empty;

        [Required]
        public string Number { get; set; } = string.Empty;

        public string? Complement { get; set; }

        [Required]
        public bool MainAddress { get; set; } = false;

        // Foreign key for ApplicationUser
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
