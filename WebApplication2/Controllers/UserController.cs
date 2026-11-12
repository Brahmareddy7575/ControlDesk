using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Authorize(Policy = "UserOnly")]
    public class UserController : Controller
    {
        private readonly ApplicationDBContext _context;

        public UserController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var vm = await BuildDashboardViewModelAsync();
            if (vm == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDetails([Bind(Prefix = "UpdateForm")] UserDetailUpdateViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                var invalidVm = await BuildDashboardViewModelAsync(model);
                return View("Dashboard", invalidVm);
            }

            var trimmedName = model.Name.Trim();
            var trimmedEmail = model.Email.Trim();

            var duplicate = await _context.Users.AnyAsync(u =>
                u.ID != userId.Value &&
                ((u.Name != null && u.Name.ToLower() == trimmedName.ToLower()) ||
                 (u.Email != null && u.Email.ToLower() == trimmedEmail.ToLower())));

            if (duplicate)
            {
                ModelState.AddModelError("UpdateForm.Email", "That username or email is already used by another account.");
                var vm = await BuildDashboardViewModelAsync(model);
                return View("Dashboard", vm);
            }

            var request = new RegistrationRequest
            {
                UserId = userId.Value,
                RequestType = "DetailUpdate",
                ApprovalStatusId = 1, // Pending Admin approval
                SubmittedName = trimmedName,
                SubmittedEmail = trimmedEmail,
                PhoneNumber = model.PhoneNumber.Trim(),
                Department = string.IsNullOrWhiteSpace(model.Department) ? null : model.Department.Trim(),
                Address = model.Address.Trim(),
                PersonalDetails = model.PersonalDetails.Trim(),
                SubmittedAt = DateTime.UtcNow
            };

            _context.RegistrationRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your details have been submitted and sent to the Admin for approval.";
            return RedirectToAction(nameof(Dashboard));
        }

        private int? GetCurrentUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out var id) ? id : null;
        }

        private async Task<UserDashboardViewModel?> BuildDashboardViewModelAsync(UserDetailUpdateViewModel? updateForm = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return null;
            }

            var profile = await _context.Users
                .Include(u => u.UserRole)
                .Include(u => u.ApprovalStatus)
                .FirstOrDefaultAsync(u => u.ID == userId.Value);

            if (profile == null)
            {
                return null;
            }

            var myRequests = await _context.RegistrationRequests
                .Include(r => r.ApprovalStatus)
                .Include(r => r.ReviewedByUser)
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            var productCount = await _context.Products.CountAsync();

            return new UserDashboardViewModel
            {
                Profile = profile,
                MyRequests = myRequests,
                ProductCount = productCount,
                UpdateForm = updateForm ?? new UserDetailUpdateViewModel
                {
                    Name = profile.Name ?? string.Empty,
                    Email = profile.Email ?? string.Empty,
                    PhoneNumber = profile.PhoneNumber ?? string.Empty,
                    Department = profile.Department,
                    Address = profile.Address ?? string.Empty,
                    PersonalDetails = profile.PersonalDetails ?? string.Empty
                }
            };
        }
    }
}

