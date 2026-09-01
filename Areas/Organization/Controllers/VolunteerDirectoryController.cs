using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using OpenHandsVolunteerPlatform.ViewModels;
using System.Security.Claims;
using System.Text;

namespace OpenHandsVolunteerPlatform.Areas.Organization.Controllers
{
    [Area("Organization")]
    [Authorize(Roles = "Organization")]
    public class VolunteerDirectoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VolunteerDirectoryController> _logger;

        public VolunteerDirectoryController(ApplicationDbContext context, ILogger<VolunteerDirectoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Organization/VolunteerDirectory
        public async Task<IActionResult> Index(string search = "", string filter = "all", string opportunityId = "")
        {
            var orgId = await GetCurrentOrganizationId();
            if (orgId == null)
                return NotFound();

            // Base query: Get applications for this organization
            var query = _context.Applications
                .Include(a => a.Volunteer)
                    .ThenInclude(v => v.User)
                .Include(a => a.Opportunity)
                .Where(a => a.Opportunity.OrganizationId == orgId);

            // If opportunityId is provided, filter to ONLY that opportunity
            if (!string.IsNullOrEmpty(opportunityId))
            {
                query = query.Where(a => a.OpportunityId == opportunityId);

                // IMPORTANT: Only show volunteers with APPLIED status for this opportunity
                query = query.Where(a => a.Status == ApplicationStatus.Applied);

                var opportunity = await _context.Opportunities
                    .FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);
                ViewBag.OpportunityTitle = opportunity?.Title;
                ViewBag.IsSpecificOpportunity = true;
            }
            else
            {
                // For general directory, show all volunteers with APPLIED status across all opportunities
                query = query.Where(a => a.Status == ApplicationStatus.Applied);
                ViewBag.IsSpecificOpportunity = false;
            }

            var applications = await query.ToListAsync();

            //By FY - fix for stats-card
            // Group by volunteer to get unique volunteers with their stats
            var allVolunteers = applications
                .GroupBy(x => x.VolunteerId)
                .Select(g =>
                {
                    var firstApp = g.First();
                    var volunteer = firstApp.Volunteer;
                    var latestApp = g.OrderByDescending(x => x.AppliedAt).First();

                    return new VolunteerDirectoryItemViewModel
                    {
                        VolunteerId = volunteer.VolunteerId,
                        Name = volunteer.Name,
                        Email = volunteer.User?.Email ?? "-",
                        Phone = volunteer.Phone ?? "-",
                        Age = volunteer.Age,
                        JoinedDate = volunteer.User?.CreatedAt ?? DateTime.Now,
                        //CreditLevel = volunteer.CreditLevel,
                        CompletedEvents = volunteer.CompletedCount,
                        TotalHours = volunteer.TotalHours,
                        NoShowCount = volunteer.NoShowCount,
                        LastAppliedDate = latestApp.AppliedAt,
                        LastOpportunity = latestApp.Opportunity?.Title ?? "-"
                    };
                })
                .ToList();

            // Use allVolunteers for stats
            var viewModel = new VolunteerDirectoryViewModel
            {
                Volunteers = allVolunteers
            };

            // Apply table filter separately
            List<VolunteerDirectoryItemViewModel> tableVolunteers = allVolunteers;


            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                tableVolunteers = tableVolunteers
                    .Where(v => v.Name.ToLower().Contains(search) ||
                               v.Email.ToLower().Contains(search))
                    .ToList();
            }

            // Apply credit filter
            switch (filter.ToLower())
            {
                case "high":
                    tableVolunteers = tableVolunteers.Where(v => v.CreditLevel == CreditLevel.Core).ToList();
                    break;
                case "average":
                    tableVolunteers = tableVolunteers.Where(v => v.CreditLevel == CreditLevel.Growing).ToList();
                    break;
                case "low":
                    tableVolunteers = tableVolunteers.Where(v => v.CreditLevel == CreditLevel.Inactive).ToList();
                    break;
            }

            // Assign filtered list for display in the view
            ViewBag.TableVolunteers = tableVolunteers;

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentOpportunityId = opportunityId;

            return View(viewModel);
        }

        // GET: Organization/VolunteerDirectory/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var orgId = await GetCurrentOrganizationId();
            if (orgId == null)
                return NotFound();

            var volunteer = await _context.Volunteers
                .Include(v => v.User)
                .Include(v => v.Applications)
                    .ThenInclude(a => a.Opportunity)
                .FirstOrDefaultAsync(v => v.VolunteerId == id);

            if (volunteer == null)
                return NotFound();

            // Get only applications to this organization's opportunities
            var applications = volunteer.Applications
                .Where(a => a.Opportunity.OrganizationId == orgId)
                .OrderByDescending(a => a.AppliedAt)
                .ToList();

            var viewModel = new
            {
                Volunteer = new
                {
                    volunteer.VolunteerId,
                    volunteer.Name,
                    Email = volunteer.User?.Email,
                    volunteer.Phone,
                    volunteer.Age,
                    volunteer.CreditLevel,
                    volunteer.CompletedCount,
                    volunteer.TotalHours,
                    volunteer.NoShowCount,
                    JoinedDate = volunteer.User?.CreatedAt
                },
                Applications = applications.Select(a => new
                {
                    a.ApplicationId,
                    Opportunity = a.Opportunity.Title,
                    a.Opportunity.Date,
                    a.Status,
                    a.AttendanceStatus,
                    a.AppliedAt
                })
            };

            return Json(viewModel);
        }

        // GET: Organization/VolunteerDirectory/Export
        public async Task<IActionResult> Export(string filter = "all")
        {
            var orgId = await GetCurrentOrganizationId();
            if (orgId == null)
                return NotFound();

            var volunteers = await GetVolunteersForExport(orgId, filter);

            // Build CSV
            var csv = new StringBuilder();
            csv.AppendLine("Name,Email,Phone,Age,Credit Level,Completed Events,Total Hours,No-Shows,Last Applied");

            foreach (var v in volunteers)
            {
                csv.AppendLine($"{v.Name},{v.Email},{v.Phone},{v.Age},{v.CreditLevelDisplay},{v.CompletedEvents},{v.TotalHours},{v.NoShowCount},{v.LastAppliedDate:dd MMM yyyy}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"volunteer_directory_{DateTime.Now:yyyyMMdd}.csv");
        }

        private async Task<List<VolunteerDirectoryItemViewModel>> GetVolunteersForExport(string orgId, string filter)
        {
            var applications = await _context.Applications
                .Include(a => a.Volunteer)
                    .ThenInclude(v => v.User)
                .Include(a => a.Opportunity)
                .Where(a => a.Opportunity.OrganizationId == orgId)
                .ToListAsync();

            var volunteers = applications
                .GroupBy(x => x.Volunteer.VolunteerId)
                .Select(g =>
                {
                    var volunteer = g.First().Volunteer;
                    var latestApp = g.OrderByDescending(x => x.AppliedAt).First();

                    return new VolunteerDirectoryItemViewModel
                    {
                        Name = volunteer.Name,
                        Email = volunteer.User?.Email ?? "-",
                        Phone = volunteer.Phone ?? "-",
                        Age = volunteer.Age,
                        //CreditLevel = volunteer.CreditLevel,
                        CompletedEvents = volunteer.CompletedCount,
                        TotalHours = volunteer.TotalHours,
                        NoShowCount = volunteer.NoShowCount,
                        LastAppliedDate = latestApp.AppliedAt
                    };
                })
                .ToList();

            // Apply filter
            switch (filter.ToLower())
            {
                case "high":
                    volunteers = volunteers.Where(v => v.CreditLevel == CreditLevel.Core).ToList();
                    break;
                case "average":
                    volunteers = volunteers.Where(v => v.CreditLevel == CreditLevel.Growing).ToList();
                    break;
                case "low":
                    volunteers = volunteers.Where(v => v.CreditLevel == CreditLevel.Inactive).ToList();
                    break;
            }

            return volunteers;
        }

        private async Task<string?> GetCurrentOrganizationId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.UserId == userId);
            return org?.OrganizationId;
        }

        // GET: Organization/VolunteerDirectory/CreditHistory/5
        public async Task<IActionResult> CreditHistory(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var orgId = await GetCurrentOrganizationId();
            if (orgId == null)
                return NotFound();

            var volunteer = await _context.Volunteers
                .Include(v => v.User)
                .Include(v => v.CreditScoreHistories)
                    .ThenInclude(h => h.Opportunity)
                .FirstOrDefaultAsync(v => v.VolunteerId == id);

            if (volunteer == null)
                return NotFound();

            // Filter histories for this organization only
            var histories = volunteer.CreditScoreHistories
                .Where(h => h.OrganizationId == orgId)
                .OrderByDescending(h => h.ChangedAt)
                .ToList();

            ViewBag.VolunteerName = volunteer.Name;
            ViewBag.CurrentCreditLevel = volunteer.CreditLevel;
            ViewBag.CompletedEvents = volunteer.CompletedCount;
            ViewBag.NoShowCount = volunteer.NoShowCount;
            ViewBag.TotalHours = volunteer.TotalHours;

            return PartialView("CreditHistory", histories); // Make sure this matches your filename
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveVolunteer([FromBody] RemoveVolunteerModel model)
        {
            try
            {
                var orgId = await GetCurrentOrganizationId();
                if (orgId == null)
                    return Json(new { success = false, message = "Organization not found" });

                // Find the application
                var application = await _context.Applications
                    .Include(a => a.Opportunity)
                    .Include(a => a.Volunteer)
                    .FirstOrDefaultAsync(a => a.OpportunityId == model.OpportunityId &&
                                              a.VolunteerId == model.VolunteerId &&
                                              a.Opportunity.OrganizationId == orgId);

                if (application == null)
                    return Json(new { success = false, message = "Application not found" });

                // Check if opportunity is in the future (can remove)
                if (application.Opportunity.Date < DateTime.Now.Date)
                {
                    return Json(new { success = false, message = "Cannot remove volunteer from past opportunities" });
                }

                // Store original status for waitlist promotion
                var wasApplied = application.Status == ApplicationStatus.Applied;

                // Update status to Removed
                application.Status = ApplicationStatus.Removed;

                await _context.SaveChangesAsync();

                // If it was an applied volunteer, promote from waitlist if any
                if (wasApplied)
                {
                    await PromoteFromWaitlist(application.OpportunityId);
                }

                return Json(new { success = true, message = "Volunteer removed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Add this helper method for waitlist promotion
        private async Task PromoteFromWaitlist(string opportunityId)
        {
            var opportunity = await _context.Opportunities
                .Include(o => o.Applications)
                .FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);

            if (opportunity == null) return;

            // Get current applied count
            var currentApplied = opportunity.Applications.Count(a => a.Status == ApplicationStatus.Applied);
            var slotsAvailable = opportunity.VolunteersNeeded - currentApplied;

            if (slotsAvailable > 0)
            {
                // Get waitlisted applications in order
                var waitlistedApps = opportunity.Applications
                    .Where(a => a.Status == ApplicationStatus.Waitlist)
                    .OrderBy(a => a.AppliedAt)
                    .Take(slotsAvailable)
                    .ToList();

                foreach (var waitlistApp in waitlistedApps)
                {
                    waitlistApp.Status = ApplicationStatus.Applied;
                    // TODO: Send email notification to volunteer
                }

                await _context.SaveChangesAsync();
            }
        }

        public class RemoveVolunteerModel
        {
            public string VolunteerId { get; set; }
            public string OpportunityId { get; set; }
        }
    }
}