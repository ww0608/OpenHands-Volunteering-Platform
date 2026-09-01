using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using OpenHandsVolunteerPlatform.ViewModels;
using System.Security.Claims;

//By WW
namespace OpenHandsVolunteerPlatform.Areas.Volunteer.Controllers
{
    [Area("Volunteer")]
    [Authorize(Roles = "Volunteer")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(ApplicationDbContext context, ILogger<ProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Volunteer/Profile
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var volunteer = await _context.Volunteers
                .Include(v => v.User)
                .Include(v => v.Applications)
                    .ThenInclude(a => a.Opportunity)
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null)
            {
                return RedirectToAction("Create", "Profile", new { area = "" });
            }

            // Calculate application stats
            var appliedCount = volunteer.Applications
                .Count(a => a.Status == ApplicationStatus.Applied || a.Status == ApplicationStatus.Waitlist);

            var waitlistCount = volunteer.Applications
                .Count(a => a.Status == ApplicationStatus.Waitlist);

            var lastActive = volunteer.Applications
                .Where(a => a.Status == ApplicationStatus.Applied || a.Status == ApplicationStatus.Waitlist)
                .OrderByDescending(a => a.AppliedAt)
                .FirstOrDefault()?.AppliedAt;

            var viewModel = new VolProfileViewModel
            {
                VolunteerId = volunteer.VolunteerId,
                Name = volunteer.Name,
                Email = volunteer.User?.Email ?? "",
                Phone = volunteer.Phone ?? "",
                Age = volunteer.Age,
                Availability = volunteer.Availability,
                // THESE ARE THE IMPORTANT ONES - DIRECT FROM DATABASE
                CompletedEvents = volunteer.CompletedCount,
                TotalHours = volunteer.TotalHours,
                NoShowCount = volunteer.NoShowCount,
                AppliedCount = appliedCount,
                WaitlistCount = waitlistCount,
                JoinedDate = volunteer.User?.CreatedAt ?? DateTime.Now,
                LastActiveDate = lastActive
                // NextMilestone and ProgressToNextMilestone are now calculated in the ViewModel
            };

            return View(viewModel);
        }

        // GET: Volunteer/Profile/CreditHistory
        public async Task<IActionResult> CreditHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var volunteer = await _context.Volunteers
                .Include(v => v.CreditScoreHistories)
                    .ThenInclude(h => h.Opportunity)
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null)
            {
                return NotFound();
            }

            var histories = volunteer.CreditScoreHistories
                .OrderByDescending(h => h.ChangedAt)
                .ToList();

            ViewBag.VolunteerName = volunteer.Name;
            ViewBag.CurrentCreditLevel = new VolProfileViewModel
            {
                CompletedEvents = volunteer.CompletedCount,
                NoShowCount = volunteer.NoShowCount
            }.CreditLevel;
            ViewBag.CompletedEvents = volunteer.CompletedCount;
            ViewBag.NoShowCount = volunteer.NoShowCount;
            ViewBag.TotalHours = volunteer.TotalHours;

            return PartialView("CreditHistory", histories);
        }

        // GET: Volunteer/Profile/Edit
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var volunteer = await _context.Volunteers
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null)
            {
                return NotFound();
            }

            var viewModel = new VolEditViewModel
            {
                VolunteerId = volunteer.VolunteerId,
                Name = volunteer.Name,
                Email = volunteer.User?.Email ?? "",
                Phone = volunteer.Phone ?? "",
                Age = volunteer.Age,
                Availability = volunteer.Availability
            };

            return View(viewModel);
        }

        // POST: Volunteer/Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VolEditViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var volunteer = await _context.Volunteers
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null)
            {
                return NotFound();
            }

            volunteer.Name = viewModel.Name;
            volunteer.Phone = viewModel.Phone;
            volunteer.Age = viewModel.Age;
            volunteer.Availability = viewModel.Availability;

            if (volunteer.User != null && volunteer.User.Email != viewModel.Email)
            {
                volunteer.User.Email = viewModel.Email;
                volunteer.User.UserName = viewModel.Email;
                volunteer.User.NormalizedEmail = viewModel.Email.ToUpper();
                volunteer.User.NormalizedUserName = viewModel.Email.ToUpper();
            }

            //if (!string.IsNullOrEmpty(viewModel.Password))
            //{
            //    var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            //    var token = await userManager.GeneratePasswordResetTokenAsync(volunteer.User);
            //    var passwordResult = await userManager.ResetPasswordAsync(volunteer.User, token, viewModel.Password);

            //    if (!passwordResult.Succeeded)
            //    {
            //        foreach (var error in passwordResult.Errors)
            //        {
            //            ModelState.AddModelError("Password", error.Description);
            //        }
            //        return View(viewModel);
            //    }
            //}

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Profile updated successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile");
                TempData["ErrorMessage"] = "Error updating profile. Please try again.";
                return View(viewModel);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}