using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using System.Security.Claims;

//By ANGEL
namespace OpenHandsVolunteerPlatform.Areas.Volunteer.Controllers
{
    [Area("Volunteer")]
    [Authorize(Roles = "Volunteer")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Report an opportunity
        [HttpGet]
        public async Task<IActionResult> ReportOpportunity(string id)
        {
            var opportunity = await _context.Opportunities
                .Include(o => o.Organization)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null) return NotFound();

            ViewBag.TargetName = opportunity.Title;
            ViewBag.TargetId = id;
            ViewBag.TargetType = ReportTargetType.Opportunity;
            return View("Report");
        }

        // GET: Report an organization
        [HttpGet]
        public async Task<IActionResult> ReportOrganization(string id)
        {
            var org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.OrganizationId == id);

            if (org == null) return NotFound();

            ViewBag.TargetName = org.OrganizationName;
            ViewBag.TargetId = id;
            ViewBag.TargetType = ReportTargetType.Organization;
            return View("Report");
        }

        // POST: Submit report
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string targetId, ReportTargetType targetType, string reason, string? details)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if already reported by this user
            var alreadyReported = await _context.Reports
                .AnyAsync(r => r.ReporterId == userId
                            && r.TargetId == targetId
                            && r.Status == ReportStatus.Pending);

            if (alreadyReported)
            {
                TempData["ErrorMessage"] = "You have already submitted a report for this. Please wait for it to be reviewed.";
                return RedirectToAction("Index", "Opportunities");
            }

            _context.Reports.Add(new Report
            {
                ReportId = Guid.NewGuid().ToString(),
                ReporterId = userId,
                TargetType = targetType,
                TargetId = targetId,
                Reason = reason,
                Details = details,
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your report has been submitted. Our team will review it shortly.";
            return RedirectToAction("Index", "Opportunities");
        }
    }
}