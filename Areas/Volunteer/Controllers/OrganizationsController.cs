using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models.Enums;
using System.Reflection;
using System.Security.Claims;

//By ANGEL
namespace OpenHandsVolunteerPlatform.Areas.Volunteer.Controllers
{
    [Area("Volunteer")]
    public class OrganizationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrganizationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Page 10 — Browse all organizations
        public async Task<IActionResult> Index(string search = "", string type = "", string sort = "newest")
        {
            var orgs = await _context.Organizations
                .Include(o => o.User)
                .Include(o => o.Opportunities)
                .Where(o => o.VerificationStatus == VerificationStatus.Approved
                    && (o.User.LockoutEnd == null
                    || o.User.LockoutEnd <= DateTimeOffset.Now))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // Apply search filter (organization name)
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                orgs = orgs.Where(o => o.OrganizationName.ToLower().Contains(search)).ToList();
            }
            //// Apply mission filter (free text)
            //if (!string.IsNullOrEmpty(mission))
            //{
            //    mission = mission.ToLower();
            //    orgs = orgs.Where(o => (o.Mission != null && o.Mission.ToLower().Contains(mission)) ||
            //                           o.OrganizationName.ToLower().Contains(mission)).ToList();
            //}
            // Apply organization type filter
            if (!string.IsNullOrEmpty(type) && type != "all")
            {
                orgs = orgs.Where(o => o.OrganizationType != null &&
                                       o.OrganizationType.Equals(type, StringComparison.OrdinalIgnoreCase))
                           .ToList();
            }
            // Apply sorting
            switch (sort)
            {
                case "oldest":
                    orgs = orgs.OrderBy(o => o.CreatedAt).ToList();
                    break;
                case "name":
                    orgs = orgs.OrderBy(o => o.OrganizationName).ToList();
                    break;
                case "followers":
                    orgs = orgs.OrderByDescending(o => o.Followers.Count).ToList();
                    break;
                case "newest":
                default:
                    orgs = orgs.OrderByDescending(o => o.CreatedAt).ToList();
                    break;
            }

            ViewBag.CurrentSearch = search;
            //ViewBag.CurrentMission = mission;
            ViewBag.CurrentType = type;
            ViewBag.CurrentSort = sort;

            return View(orgs);
        }

        // Page 11 — Organization public profile
        public async Task<IActionResult> Profile(string id)
        {
            if (id == null) return NotFound();

            var org = await _context.Organizations
                .Include(o => o.User)
                .Include(o => o.Opportunities)
                .Include(o => o.Ratings)
                .Include(o => o.Followers)
                .FirstOrDefaultAsync(o => o.OrganizationId == id && o.VerificationStatus == VerificationStatus.Approved
                    && (o.User.LockoutEnd == null
                        || o.User.LockoutEnd <= DateTimeOffset.Now));

            if (org == null)
            {
                TempData["ErrorMessage"] = "This organization is not available.";
                return RedirectToAction("Index");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if current volunteer follows this org
            bool isFollowing = false;
            if (User.IsInRole("Volunteer") && userId != null)
            {
                var volunteer = await _context.Volunteers
                    .FirstOrDefaultAsync(v => v.UserId == userId);

                if (volunteer != null)
                {
                    isFollowing = await _context.Follows
                        .AnyAsync(f => f.VolunteerId == volunteer.VolunteerId
                                    && f.OrganizationId == id);
                }
            }

            // Average rating
            var avgRating = org.Ratings.Any()
                ? org.Ratings.Average(r => r.Stars)
                : 0;

            ViewBag.IsFollowing = isFollowing;
            ViewBag.FollowerCount = org.Followers.Count;
            ViewBag.AverageRating = Math.Round(avgRating, 1);
            ViewBag.RatingCount = org.Ratings.Count;

            return View(org);
        }
    }
}