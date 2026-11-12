using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    [Table("RegistrationRequests")]
    public class RegistrationRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public Users? User { get; set; }

        [Required]
        [MaxLength(30)]
        public string RequestType { get; set; } = "Registration";

        [Required]
        public int ApprovalStatusId { get; set; } = 1;

        [ForeignKey(nameof(ApprovalStatusId))]
        public ApprovalStatus? ApprovalStatus { get; set; }

        [MaxLength(40)]
        public string? SubmittedName { get; set; }

        [MaxLength(40)]
        public string? SubmittedEmail { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(60)]
        public string? Department { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(500)]
        public string? PersonalDetails { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedByUserId { get; set; }

        [ForeignKey(nameof(ReviewedByUserId))]
        public Users? ReviewedByUser { get; set; }

        [MaxLength(250)]
        public string? AdminRemarks { get; set; }
    }
}

