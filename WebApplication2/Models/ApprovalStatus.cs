using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("ApprovalStatuses")]
    public class ApprovalStatus
    {
        [Key]
        public int ApprovalStatusId { get; set; }

        [Required]
        [MaxLength(40)]
        public string StatusName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Description { get; set; }

        public ICollection<Users> Users { get; set; } = new List<Users>();
        public ICollection<RegistrationRequest> RegistrationRequests { get; set; } = new List<RegistrationRequest>();
    }
}

