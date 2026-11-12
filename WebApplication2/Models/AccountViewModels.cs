using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your username or email.")]
        [Display(Name = "Username or Email")]
        public string NameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string LoginRole { get; set; } = "User";
        public string? ReturnUrl { get; set; }
    }

    public class RegisterUserViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [MaxLength(40)]
        [Display(Name = "Username / Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(40)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [MaxLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(60)]
        [Display(Name = "Department / Organization")]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(200)]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide your personal details or registration reason.")]
        [MaxLength(500)]
        [Display(Name = "Personal Details / Notes")]
        public string PersonalDetails { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [MaxLength(40)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class RegisterAdminViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [MaxLength(40)]
        [Display(Name = "Admin Username")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(40)]
        [Display(Name = "Admin Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [MaxLength(40)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }

    public class UserDetailUpdateViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [MaxLength(40)]
        [Display(Name = "Username / Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(40)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [MaxLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(60)]
        [Display(Name = "Department / Organization")]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(200)]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide the details to submit for Admin approval.")]
        [MaxLength(500)]
        [Display(Name = "Submitted Personal Details")]
        public string PersonalDetails { get; set; } = string.Empty;
    }

    public class UserDashboardViewModel
    {
        public Users Profile { get; set; } = new();
        public UserDetailUpdateViewModel UpdateForm { get; set; } = new();
        public List<RegistrationRequest> MyRequests { get; set; } = new();
        public int ProductCount { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public List<RegistrationRequest> PendingRequests { get; set; } = new();
        public List<RegistrationRequest> ReviewedRequests { get; set; } = new();
        public List<Users> AllUsers { get; set; } = new();
        public List<product> Products { get; set; } = new();
        public RegisterAdminViewModel CreateAdminForm { get; set; } = new();
    }

    public class AdminUserDetailsViewModel
    {
        public Users User { get; set; } = new();
        public List<RegistrationRequest> Requests { get; set; } = new();
    }
}
