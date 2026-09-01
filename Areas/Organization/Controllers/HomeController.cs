using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using System.Security.Claims;

//By ANGEL
namespace OpenHandsVolunteerPlatform.Areas.Organization.Controllers
{
    [Area("Organization")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var org = await _context.Organizations
                .Include(o => o.Ratings) //Fix by ANGEL
                    .ThenInclude(r => r.Volunteer)
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (org == null) return RedirectToAction("Index", "Opportunity");

            // Followers count
            var followersCount = await _context.Follows
                .CountAsync(f => f.OrganizationId == org.OrganizationId);

            // Total opportunities
            var totalOpportunities = await _context.Opportunities
                .CountAsync(o => o.OrganizationId == org.OrganizationId);

            // Active volunteers (applied to upcoming events)
            var activeVolunteers = await _context.Applications
                .Include(a => a.Opportunity)
                .CountAsync(a => a.Opportunity.OrganizationId == org.OrganizationId
                              && a.Status == ApplicationStatus.Applied
                              && a.Opportunity.Date >= DateTime.Today);

            // Total hours (completed events)
            var completedEvents = await _context.Opportunities
                .Where(o => o.OrganizationId == org.OrganizationId
                         && o.Date.Date < DateTime.Today)
                .ToListAsync();

            var totalHours = completedEvents
                .Where(o => o.EndTime > o.StartTime) // only count valid events
                .Sum(o => (o.EndTime - o.StartTime).TotalHours);

            ViewBag.TotalHours = Math.Round(totalHours, 1);

            // Pending reports on this org
            var pendingReports = await _context.Reports
                .CountAsync(r => r.TargetId == org.OrganizationId
                              && r.Status == ReportStatus.Pending);

            ViewBag.Organization = org;
            ViewBag.FollowersCount = followersCount;
            ViewBag.IsOrganizationApproved = org.VerificationStatus == VerificationStatus.Approved; //Fix by ANGEL
            ViewBag.TotalOpportunities = totalOpportunities;
            ViewBag.ActiveVolunteers = activeVolunteers;
            ViewBag.TotalHours = Math.Round(totalHours, 1);
            ViewBag.PendingReports = pendingReports;

            var ongoingOpportunities = await _context.Opportunities
                .Include(o => o.Applications)
                .Where(o => o.OrganizationId == org.OrganizationId
                         && o.Date.Date == DateTime.Today)
                .ToListAsync();

            ViewBag.OngoingOpportunities = ongoingOpportunities;

            //Fix by ANGEL
            var avgRating = org.Ratings.Any()
            ? Math.Round(org.Ratings.Average(r => r.Stars), 1)
            : 0;

            ViewBag.AverageRating = avgRating;
            ViewBag.RatingCount = org.Ratings.Count;
            ViewBag.RecentRatings = org.Ratings
                .OrderByDescending(r => r.RatedAt)
                .Take(5)
                .ToList();

            return View();
        }
    }
}