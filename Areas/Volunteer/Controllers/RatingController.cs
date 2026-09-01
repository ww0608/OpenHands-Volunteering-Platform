using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using OpenHandsVolunteerPlatform.ViewModels;
using System.Security.Claims;

//By ANGEL
namespace OpenHandsVolunteerPlatform.Areas.Volunteer.Controllers
{
    [Area("Volunteer")]
    [Authorize(Roles = "Volunteer")]
    public class RatingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RatingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Rate an organization after completing an opportunity
        [HttpGet]
        public async Task<IActionResult> RateOrganization(string organizationId, string? opportunityId)
        {
            var org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.OrganizationId == organizationId);
            if (org == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null) return Forbid();

            // Block access if volunteer didn't attend
            if (!string.IsNullOrEmpty(opportunityId))
            {
                var attended = await _context.Applications
                    .AnyAsync(a => a.VolunteerId == volunteer.VolunteerId
                                && a.OpportunityId == opportunityId
                                && a.AttendanceStatus == AttendanceStatus.Present);

                if (!attended)
                {
                    TempData["ErrorMessage"] = "You can only rate an organization after attending their event.";
                    return RedirectToAction("Index", "Application");
                }
            }

            // Check if already rated
            var existingRating = await _context.OrganizationRatings
                .FirstOrDefaultAsync(r => r.VolunteerId == volunteer.VolunteerId
                                       && r.OrganizationId == organizationId
                                       && r.OpportunityId == opportunityId);

            var viewModel = new RatingViewModel
            {
                OrganizationId = organizationId,
                OrganizationName = org.OrganizationName,
                OpportunityId = opportunityId ?? "",
                OpportunityTitle = opportunityId != null
                    ? (await _context.Opportunities.FindAsync(opportunityId))?.Title ?? ""
                    : "",
                HasRated = existingRating != null,
                ExistingRating = existingRating?.Stars
            };

            return View(viewModel);
        }

        // POST: Submit rating
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(RatingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("RateOrganization", model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null) return Forbid();

            // Verify volunteer actually attended this opportunity
            if (!string.IsNullOrEmpty(model.OpportunityId))
            {
                var application = await _context.Applications
                    .FirstOrDefaultAsync(a => a.VolunteerId == volunteer.VolunteerId
                                           && a.OpportunityId == model.OpportunityId
                                           && a.AttendanceStatus == AttendanceStatus.Present);

                if (application == null)
                {
                    TempData["ErrorMessage"] = "You can only rate an organization after attending their event.";
                    return RedirectToAction("Index", "Application");
                }
            }

            // Check if already rated
            var existing = await _context.OrganizationRatings
                .FirstOrDefaultAsync(r => r.VolunteerId == volunteer.VolunteerId
                                       && r.OrganizationId == model.OrganizationId
                                       && r.OpportunityId == model.OpportunityId);

            if (existing != null)
            {
                // Update existing rating
                existing.Stars = model.Stars;
                existing.Comment = model.Comment;
                existing.RatedAt = DateTime.Now;
            }
            else
            {
                // Create new rating
                _context.OrganizationRatings.Add(new OrganizationRating
                {
                    RatingId = Guid.NewGuid().ToString(),
                    VolunteerId = volunteer.VolunteerId,
                    OrganizationId = model.OrganizationId,
                    OpportunityId = string.IsNullOrEmpty(model.OpportunityId) ? null : model.OpportunityId,
                    Stars = model.Stars,
                    Comment = model.Comment,
                    RatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thank you for your feedback!";

            if (!string.IsNullOrEmpty(model.OpportunityId))
            {
                return RedirectToAction("Details", "Opportunities", new { id = model.OpportunityId });
            }

            return RedirectToAction("Index", "Opportunities");
        }
    }
}