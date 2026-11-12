using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IPasswordHasher<Users> _passwordHasher;

        public AccountController(ApplicationDBContext context, IPasswordHasher<Users> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? mode = null, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToRoleDashboard();
            }

            var normalizedMode = string.Equals(mode, "Admin", StringComparison.OrdinalIgnoreCase)
                ? "Admin"
                : "User";

            return View(new LoginViewModel
            {
                LoginRole = normalizedMode,
                ReturnUrl = returnUrl
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AdminLogin(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToRoleDashboard();
            }

            return View("Login", new LoginViewModel
            {
                LoginRole = "Admin",
                ReturnUrl = returnUrl
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult UserLogin(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToRoleDashboard();
            }

            return View("Login", new LoginViewModel
            {
                LoginRole = "User",
                ReturnUrl = returnUrl
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var expectedRole = string.Equals(model.LoginRole, "Admin", StringComparison.OrdinalIgnoreCase)
                ? "Admin"
                : "User";
            model.LoginRole = expectedRole;

            if (!ModelState.IsValid)
            {
                return View("Login", model);
            }

            Users? user;
            try
            {
                var identifier = model.NameOrEmail.Trim().ToLower();
                user = await _context.Users
                    .Include(u => u.UserRole)
                    .Include(u => u.ApprovalStatus)
                    .FirstOrDefaultAsync(u =>
                        (u.Name != null && u.Name.ToLower() == identifier) ||
                        (u.Email != null && u.Email.ToLower() == identifier));
            }
            catch (NpgsqlException ex)
            {
                ModelState.AddModelError(string.Empty, $"Database connection error: {ex.Message}");
                return View("Login", model);
            }

            if (user == null || string.IsNullOrEmpty(user.password))
            {
                ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
                return View("Login", model);
            }

            PasswordVerificationResult verification;
            try
            {
                verification = _passwordHasher.VerifyHashedPassword(user, user.password, model.Password);
            }
            catch (FormatException)
            {
                verification = PasswordVerificationResult.Failed;
            }

            if (verification == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
                return View("Login", model);
            }

            var actualRole = user.UserRole?.RoleName ?? user.Role ?? "User";
            if (!string.Equals(actualRole, expectedRole, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty,
                    expectedRole == "Admin"
                        ? "Only Admin accounts can sign in via Admin Login. Please use User Login."
                        : "Admin accounts must sign in via Admin Login.");
                return View("Login", model);
            }

            if (user.ApprovalStatusId == 3)
            {
                ModelState.AddModelError(string.Empty, "Your registration request was rejected by the Admin.");
                return View("Login", model);
            }

            if (!user.IsApproved || user.ApprovalStatusId != 2)
            {
                ModelState.AddModelError(string.Empty, "Your account status is Pending. Please wait for Admin approval before signing in.");
                return View("Login", model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? user.Email ?? "User"),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, actualRole)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            if (!string.IsNullOrEmpty(model.ReturnUrl) &&
                Url.IsLocalUrl(model.ReturnUrl) &&
                !string.Equals(model.ReturnUrl, "/", StringComparison.Ordinal) &&
                !model.ReturnUrl.StartsWith("/Account", StringComparison.OrdinalIgnoreCase))
            {
                var allowedReturnUrl =
                    (string.Equals(actualRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
                     !model.ReturnUrl.StartsWith("/User", StringComparison.OrdinalIgnoreCase)) ||
                    (string.Equals(actualRole, "User", StringComparison.OrdinalIgnoreCase) &&
                     !model.ReturnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase));

                if (allowedReturnUrl)
                {
                    return Redirect(model.ReturnUrl);
                }
            }

            return actualRole == "Admin"
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Dashboard", "User");
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> AdminLogin(LoginViewModel model)
        {
            model.LoginRole = "Admin";
            return Login(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> UserLogin(LoginViewModel model)
        {
            model.LoginRole = "User";
            return Login(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToRoleDashboard();
            }

            return View(new RegisterUserViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var trimmedName = model.Name.Trim();
            var trimmedEmail = model.Email.Trim();

            try
            {
                var exists = await _context.Users.AnyAsync(u =>
                    (u.Email != null && u.Email.ToLower() == trimmedEmail.ToLower()) ||
                    (u.Name != null && u.Name.ToLower() == trimmedName.ToLower()));

                if (exists)
                {
                    ModelState.AddModelError(string.Empty, "An account with that username or email already exists.");
                    return View(model);
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();

                var newUser = new Users
                {
                    Name = trimmedName,
                    Email = trimmedEmail,
                    Role = "User",
                    RoleId = 2, // User
                    ApprovalStatusId = 1, // Pending
                    IsApproved = false,
                    PhoneNumber = model.PhoneNumber.Trim(),
                    Department = string.IsNullOrWhiteSpace(model.Department) ? null : model.Department.Trim(),
                    Address = model.Address.Trim(),
                    PersonalDetails = model.PersonalDetails.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                newUser.password = _passwordHasher.HashPassword(newUser, model.Password);

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                var request = new RegistrationRequest
                {
                    UserId = newUser.ID,
                    RequestType = "Registration",
                    ApprovalStatusId = 1, // Pending
                    SubmittedName = newUser.Name,
                    SubmittedEmail = newUser.Email,
                    PhoneNumber = newUser.PhoneNumber,
                    Department = newUser.Department,
                    Address = newUser.Address,
                    PersonalDetails = newUser.PersonalDetails,
                    SubmittedAt = DateTime.UtcNow
                };

                _context.RegistrationRequests.Add(request);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (NpgsqlException ex)
            {
                ModelState.AddModelError(string.Empty, $"Database connection error: {ex.Message}");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Registration submitted for '{trimmedName}'. Your account status is Pending until an Admin approves it.";
            return RedirectToAction(nameof(UserLogin));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View("~/Views/Home/AccessDenied.cshtml");
        }

        private IActionResult RedirectToRoleDashboard()
        {
            return User.IsInRole("Admin")
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Dashboard", "User");
        }
    }
}

