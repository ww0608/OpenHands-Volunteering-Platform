//By ANGEL - admin side combined

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using OpenHandsVolunteerPlatform.ViewModels;
using System.Text;

namespace OpenHandsVolunteerPlatform.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<UsersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Admin/Users/Index
        public async Task<IActionResult> Index(string search = "", string filter = "all")
        {
            var volunteers = await _context.Volunteers
                .Include(v => v.User)
                .Include(v => v.Applications)
                .ToListAsync();

            var viewModels = volunteers.Select(v => new AdminUserViewModel
            {
                UserId = v.UserId,
                Name = v.Name,
                Email = v.User?.Email ?? "-",
                JoinedDate = v.User?.CreatedAt ?? DateTime.Now,
                CompletedEvents = v.CompletedCount,
                NoShowCount = v.NoShowCount,
                TotalHours = v.TotalHours,
                CreditLevel = v.CreditLevel,
                Role = "Volunteer",
                IsActive = v.User?.LockoutEnd == null || v.User.LockoutEnd <= DateTime.Now
            }).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                viewModels = viewModels
                    .Where(v => v.Name.ToLower().Contains(search) ||
                               v.Email.ToLower().Contains(search))
                    .ToList();
            }

            switch (filter.ToLower())
            {
                case "high":
                    viewModels = viewModels.Where(v => v.CreditLevel == CreditLevel.Core).ToList();
                    break;
                case "average":
                    viewModels = viewModels.Where(v => v.CreditLevel == CreditLevel.Growing).ToList();
                    break;
                case "low":
                    viewModels = viewModels.Where(v => v.CreditLevel == CreditLevel.Inactive).ToList();
                    break;
            }

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentFilter = filter;

            return View(viewModels);
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var volunteer = await _context.Volunteers
                .Include(v => v.User)
                .Include(v => v.Applications)
                    .ThenInclude(a => a.Opportunity)
                        .ThenInclude(o => o.Organization)
                .FirstOrDefaultAsync(v => v.UserId == id);

            if (volunteer == null) return NotFound();

            var viewModel = new AdminUserDetailViewModel
            {
                UserId = volunteer.UserId,
                Name = volunteer.Name,
                Email = volunteer.User?.Email ?? "-",
                Phone = volunteer.Phone ?? "-",
                Age = volunteer.Age,
                Availability = volunteer.Availability,
                JoinedDate = volunteer.User?.CreatedAt ?? DateTime.Now,
                CompletedEvents = volunteer.CompletedCount,
                TotalHours = volunteer.TotalHours,
                NoShowCount = volunteer.NoShowCount,
                CreditLevel = volunteer.CreditLevel,
                Applications = volunteer.Applications
                    .OrderByDescending(a => a.AppliedAt)
                    .Select(a => new UserApplicationViewModel
                    {
                        ApplicationId = a.ApplicationId,
                        OpportunityTitle = a.Opportunity?.Title ?? "-",
                        OrganizationName = a.Opportunity?.Organization?.OrganizationName ?? "-",
                        Date = a.Opportunity?.Date ?? DateTime.Now,
                        Status = a.Status,
                        AttendanceStatus = a.AttendanceStatus,
                        AppliedAt = a.AppliedAt
                    }).ToList()
            };

            return View(viewModel);
        }

        // POST: Admin/Users/Disable/5 (JSON)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Invalid user ID" });

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            user.LockoutEnd = DateTime.MaxValue;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "User account disabled" });
        }

        // POST: Admin/Users/Enable/5 (JSON)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Invalid user ID" });

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            user.LockoutEnd = null;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "User account enabled" });
        }

        // POST: Admin/Users/ToggleLock (form-based)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = "Account unlocked successfully.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["SuccessMessage"] = "Account locked successfully.";
            }

            return RedirectToAction("Index");
        }

        //// POST: Admin/Users/Delete
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Delete(string userId)
        //{
        //    var user = await _userManager.FindByIdAsync(userId);
        //    if (user == null) return NotFound();

        //    await _userManager.DeleteAsync(user);
        //    TempData["SuccessMessage"] = "User deleted successfully.";
        //    return RedirectToAction("Index");
        //}

        // GET: Admin/Users/Export
        public async Task<IActionResult> Export(string filter = "all")
        {
            var volunteers = await _context.Volunteers
                .Include(v => v.User)
                .ToListAsync();

            var list = volunteers.Select(v => new
            {
                Name = v.Name,
                Email = v.User?.Email ?? "-",
                Phone = v.Phone ?? "-",
                Age = v.Age,
                JoinedDate = v.User?.CreatedAt.ToString("dd MMM yyyy") ?? "-",
                CompletedEvents = v.CompletedCount,
                NoShows = v.NoShowCount,
                TotalHours = v.TotalHours,
                CreditLevel = v.CreditLevel.ToString()
            }).ToList();

            if (filter == "high")
                list = list.Where(v => v.CreditLevel == "Core").ToList();
            else if (filter == "average")
                list = list.Where(v => v.CreditLevel == "Growing").ToList();
            else if (filter == "low")
                list = list.Where(v => v.CreditLevel == "Inactive").ToList();

            var csv = new StringBuilder();
            csv.AppendLine("Name,Email,Phone,Age,Joined Date,Completed Events,No-Shows,Total Hours,Credit Level");

            foreach (var v in list)
            {
                csv.AppendLine($"{v.Name},{v.Email},{v.Phone},{v.Age},{v.JoinedDate}," +
                               $"{v.CompletedEvents},{v.NoShows},{v.TotalHours},{v.CreditLevel}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"volunteers_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}