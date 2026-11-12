using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(60)]
        public string RoleName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Description { get; set; }

        public ICollection<Users> Users { get; set; } = new List<Users>();
    }
}

