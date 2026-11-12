using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class Users
    {
        [Key]
        public int ID { get; set; }

        [MaxLength(40)]
        public string? Name { get; set; }

        [MaxLength(40)]
        public string? Email { get; set; }

        [MaxLength(60)]
        public string? Role { get; set; }

        [MaxLength(255)]
        public string? password { get; set; }

        public bool IsApproved { get; set; } = false;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(60)]
        public string? Department { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(500)]
        public string? PersonalDetails { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int RoleId { get; set; } = 2;

        [ForeignKey(nameof(RoleId))]
        public Role? UserRole { get; set; }

        public int ApprovalStatusId { get; set; } = 1;

        [ForeignKey(nameof(ApprovalStatusId))]
        public ApprovalStatus? ApprovalStatus { get; set; }

        public ICollection<RegistrationRequest> RegistrationRequests { get; set; } = new List<RegistrationRequest>();
    }
}
