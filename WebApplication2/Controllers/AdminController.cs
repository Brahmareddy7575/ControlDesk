using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IPasswordHasher<Users> _passwordHasher;

        public AdminController(ApplicationDBContext context, IPasswordHasher<Users> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
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
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRole)
                .Include(u => u.ApprovalStatus)
                .FirstOrDefaultAsync(u => u.ID == id);

            if (user == null)
            {
                return NotFound();
            }

            var requests = await _context.RegistrationRequests
                .Include(r => r.ApprovalStatus)
                .Include(r => r.ReviewedByUser)
                .Where(r => r.UserId == id)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            return View(new AdminUserDetailsViewModel
            {
                User = user,
                Requests = requests
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int id, string? remarks = null)
        {
            var request = await _context.RegistrationRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null || request.User == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (request.ApprovalStatusId != 1)
            {
                TempData["ErrorMessage"] = "This request has already been reviewed.";
                return RedirectToAction(nameof(Dashboard));
            }

            var adminId = GetCurrentUserId();

            if (request.RequestType == "DetailUpdate")
            {
                var duplicate = await _context.Users.AnyAsync(u =>
                    u.ID != request.UserId &&
                    ((!string.IsNullOrEmpty(request.SubmittedName) && u.Name != null && u.Name.ToLower() == request.SubmittedName.ToLower()) ||
                     (!string.IsNullOrEmpty(request.SubmittedEmail) && u.Email != null && u.Email.ToLower() == request.SubmittedEmail.ToLower())));

                if (duplicate)
                {
                    TempData["ErrorMessage"] = "Cannot approve detail update because the requested username or email is already used by another account.";
                    return RedirectToAction(nameof(Dashboard));
                }

                if (!string.IsNullOrWhiteSpace(request.SubmittedName))
                {
                    request.User.Name = request.SubmittedName;
                }
                if (!string.IsNullOrWhiteSpace(request.SubmittedEmail))
                {
                    request.User.Email = request.SubmittedEmail;
                }
                request.User.PhoneNumber = request.PhoneNumber;
                request.User.Department = request.Department;
                request.User.Address = request.Address;
                request.User.PersonalDetails = request.PersonalDetails;
            }
            else
            {
                request.User.ApprovalStatusId = 2; // Approved
                request.User.IsApproved = true;
            }

            request.ApprovalStatusId = 2; // Approved
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedByUserId = adminId;
            request.AdminRemarks = string.IsNullOrWhiteSpace(remarks) ? "Approved by Admin" : remarks.Trim();

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{request.RequestType} request for '{request.User.Name}' has been approved.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int id, string? remarks = null)
        {
            var request = await _context.RegistrationRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null || request.User == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (request.ApprovalStatusId != 1)
            {
                TempData["ErrorMessage"] = "This request has already been reviewed.";
                return RedirectToAction(nameof(Dashboard));
            }

            var adminId = GetCurrentUserId();

            if (request.RequestType == "Registration")
            {
                request.User.ApprovalStatusId = 3; // Rejected
                request.User.IsApproved = false;
            }

            request.ApprovalStatusId = 3; // Rejected
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedByUserId = adminId;
            request.AdminRemarks = string.IsNullOrWhiteSpace(remarks) ? "Rejected by Admin" : remarks.Trim();

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{request.RequestType} request for '{request.User.Name}' has been rejected.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            user.ApprovalStatusId = 2;
            user.IsApproved = true;

            var adminId = GetCurrentUserId();
            var pendingRegistrations = await _context.RegistrationRequests
                .Where(r => r.UserId == id && r.RequestType == "Registration" && r.ApprovalStatusId == 1)
                .ToListAsync();

            foreach (var req in pendingRegistrations)
            {
                req.ApprovalStatusId = 2;
                req.ReviewedAt = DateTime.UtcNow;
                req.ReviewedByUserId = adminId;
                req.AdminRemarks = "Approved by Admin";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"User '{user.Name}' has been approved for login.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(int id)
        {
            var currentAdminId = GetCurrentUserId();
            if (currentAdminId == id)
            {
                TempData["ErrorMessage"] = "You cannot reject your own currently logged-in Admin account.";
                return RedirectToAction(nameof(Dashboard));
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            user.ApprovalStatusId = 3;
            user.IsApproved = false;

            var pendingRegistrations = await _context.RegistrationRequests
                .Where(r => r.UserId == id && r.RequestType == "Registration" && r.ApprovalStatusId == 1)
                .ToListAsync();

            foreach (var req in pendingRegistrations)
            {
                req.ApprovalStatusId = 3;
                req.ReviewedAt = DateTime.UtcNow;
                req.ReviewedByUserId = currentAdminId;
                req.AdminRemarks = "Rejected by Admin";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"User '{user.Name}' has been rejected and blocked from logging in.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentAdminId = GetCurrentUserId();
            if (currentAdminId == id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own currently logged-in Admin account.";
                return RedirectToAction(nameof(Dashboard));
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Account '{user.Name}' has been deleted.";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin([Bind(Prefix = "CreateAdminForm")] RegisterAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var vm = await BuildDashboardViewModelAsync(model);
                return View("Dashboard", vm);
            }

            var trimmedName = model.Name.Trim();
            var trimmedEmail = model.Email.Trim();

            var exists = await _context.Users.AnyAsync(u =>
                (u.Email != null && u.Email.ToLower() == trimmedEmail.ToLower()) ||
                (u.Name != null && u.Name.ToLower() == trimmedName.ToLower()));

            if (exists)
            {
                ModelState.AddModelError("CreateAdminForm.Email", "An account with that username or email already exists.");
                var vm = await BuildDashboardViewModelAsync(model);
                return View("Dashboard", vm);
            }

            var newAdmin = new Users
            {
                Name = trimmedName,
                Email = trimmedEmail,
                Role = "Admin",
                RoleId = 1,
                ApprovalStatusId = 2,
                IsApproved = true,
                Department = "Administration",
                PersonalDetails = "Created by Admin",
                CreatedAt = DateTime.UtcNow
            };
            newAdmin.password = _passwordHasher.HashPassword(newAdmin, model.Password);

            _context.Users.Add(newAdmin);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Admin account '{trimmedName}' created and approved.";
            return RedirectToAction(nameof(Dashboard));
        }

        private int? GetCurrentUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out var id) ? id : null;
        }

        private async Task<AdminDashboardViewModel> BuildDashboardViewModelAsync(RegisterAdminViewModel? createAdminForm = null)
        {
            var pendingRequests = await _context.RegistrationRequests
                .Include(r => r.User)
                .Include(r => r.ApprovalStatus)
                .Where(r => r.ApprovalStatusId == 1)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            var reviewedRequests = await _context.RegistrationRequests
                .Include(r => r.User)
                .Include(r => r.ApprovalStatus)
                .Include(r => r.ReviewedByUser)
                .Where(r => r.ApprovalStatusId != 1)
                .OrderByDescending(r => r.ReviewedAt ?? r.SubmittedAt)
                .Take(20)
                .ToListAsync();

            var allUsers = await _context.Users
                .Include(u => u.UserRole)
                .Include(u => u.ApprovalStatus)
                .OrderBy(u => u.ID)
                .ToListAsync();

            var products = await _context.Products
                .OrderBy(p => p.prdid)
                .ToListAsync();

            return new AdminDashboardViewModel
            {
                PendingRequests = pendingRequests,
                ReviewedRequests = reviewedRequests,
                AllUsers = allUsers,
                Products = products,
                CreateAdminForm = createAdminForm ?? new RegisterAdminViewModel()
            };
        }
    }
}

