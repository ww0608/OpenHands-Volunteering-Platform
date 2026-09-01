using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using System.Security.Claims;

//By ANGEL
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using OpenHandsVolunteerPlatform.Services;
using OpenHandsVolunteerPlatform.ViewModels;

namespace OpenHandsVolunteerPlatform.Areas.Volunteer.Controllers
{
    [Area("Volunteer")]
    [Authorize(Roles = "Volunteer")]
    public class OpportunitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        //By ANGEL
        private readonly FollowService _followService;
        private readonly UserManager<ApplicationUser> _userManager;

        //By ANGEL
        public OpportunitiesController(
            ApplicationDbContext context,
            FollowService followService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _followService = followService;
            _userManager = userManager;
        }

        //By WW        
        // GET: Volunteer/Opportunity
        public async Task<IActionResult> Index(string search = "", string dateFilter = "all", string weekendFilter = "all", string location = "")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get the volunteer to check their credit level
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null)
            {
                return RedirectToAction("Create", "Profile", new { area = "" });
            }

            // Get all open opportunities that are NOT completed
            var allOpportunities = await _context.Opportunities
                .Include(o => o.Applications)
                    .ThenInclude(a => a.Volunteer)
                .Include(o => o.Organization)
                    .ThenInclude(org => org.User)
                .Where(o => o.Organization.VerificationStatus == VerificationStatus.Approved
                    && (o.Organization.User.LockoutEnd == null
                        || o.Organization.User.LockoutEnd < DateTimeOffset.UtcNow)) 
                .Where(o => o.ApplicationCloseDate >= DateTime.Now)
                .Where(o => o.Status == OpportunityOpenStatus.Open)
                .Where(o => o.Date.Date > DateTime.Now.Date   // Future events
                         || (o.Date.Date == DateTime.Now.Date && o.EndTime > DateTime.Now.TimeOfDay)) // Today's ongoing events

                .ToListAsync();

            // Show ALL opportunities to everyone (no credit filtering)
            var filteredOpportunities = allOpportunities.ToList();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                filteredOpportunities = filteredOpportunities
                    .Where(o => o.Title.ToLower().Contains(search) ||
                                 o.Description.ToLower().Contains(search) ||
                               o.Location.ToLower().Contains(search))
                    .ToList();
            }

            // Apply date filter
            if (!string.IsNullOrEmpty(dateFilter) && dateFilter != "all")
            {
                var today = DateTime.Today;
                switch (dateFilter)
                {
                    case "today":
                        filteredOpportunities = filteredOpportunities.Where(o => o.Date.Date == today).ToList();
                        break;
                    case "week":
                        var weekStart = today.AddDays(-(int)today.DayOfWeek);
                        filteredOpportunities = filteredOpportunities.Where(o => o.Date.Date >= weekStart && o.Date.Date <= weekStart.AddDays(6)).ToList();
                        break;
                    case "month":
                        filteredOpportunities = filteredOpportunities.Where(o => o.Date.Year == today.Year && o.Date.Month == today.Month).ToList();
                        break;
                }
            }

            // Apply weekend/weekday filter
            if (!string.IsNullOrEmpty(weekendFilter) && weekendFilter != "all")
            {
                if (weekendFilter == "weekend")
                {
                    filteredOpportunities = filteredOpportunities.Where(o => o.Date.DayOfWeek == DayOfWeek.Saturday || o.Date.DayOfWeek == DayOfWeek.Sunday).ToList();
                }
                else if (weekendFilter == "weekday")
                {
                    filteredOpportunities = filteredOpportunities.Where(o => o.Date.DayOfWeek != DayOfWeek.Saturday && o.Date.DayOfWeek != DayOfWeek.Sunday).ToList();
                }
            }

            //// Apply location filter
            //if (!string.IsNullOrEmpty(location))
            //{
            //    location = location.ToLower();
            //    filteredOpportunities = filteredOpportunities
            //        .Where(o => o.Location.ToLower().Contains(location))
            //        .ToList();
            //}

            // Separate emergency and normal opportunities
            var emergencyOpportunities = filteredOpportunities
                .Where(o => o.IsEmergency)
                .OrderBy(o => o.Date)
                .ToList();

            var normalOpportunities = filteredOpportunities
                .Where(o => !o.IsEmergency)
                .OrderBy(o => o.Date)
                .ToList();

            // Combine with emergency on top
            var opportunities = emergencyOpportunities.Concat(normalOpportunities).ToList();

            // Get applied opportunities for this volunteer
            var appliedIds = opportunities
                .Where(o => o.Applications.Any(a => a.Volunteer != null
                                                && a.Volunteer.UserId == userId
                                                && a.Status == ApplicationStatus.Applied))
                .Select(o => o.OpportunityId)
                .ToHashSet();

            ViewBag.AppliedIds = appliedIds;
            ViewBag.VolunteerCreditLevel = volunteer.CreditLevel;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentDateFilter = dateFilter;
            ViewBag.CurrentWeekendFilter = weekendFilter;
            ViewBag.CurrentLocation = location;

            return View(opportunities);
        }

        // GET: Volunteer/Opportunities/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            var opportunity = await _context.Opportunities
                .Include(o => o.Organization)
                    .ThenInclude(org => org.User)
                .Include(o => o.Applications)
                    .ThenInclude(a => a.Volunteer)
                .FirstOrDefaultAsync(m => m.OpportunityId == id);

            if (opportunity == null)
            {
                return NotFound();
            }

            // Block if org is locked
            var orgUser = opportunity.Organization?.User;
            bool orgIsLocked = orgUser != null
                && orgUser.LockoutEnd.HasValue
                && orgUser.LockoutEnd > DateTimeOffset.UtcNow;

            if (orgIsLocked)
            {
                TempData["ErrorMessage"] = "This organization is currently inactive.";
                return RedirectToAction("Index");
            }

            bool hasApplied = opportunity.Applications
                .Any(a => a.Volunteer.UserId == userId && a.Status == ApplicationStatus.Applied);

            ViewBag.HasApplied = hasApplied;

            var appliedCount = opportunity.Applications?
                .Count(a => a.Status == ApplicationStatus.Applied) ?? 0;

            //By WW
            // Check if emergency opportunity is full - just add warning, don't redirect
            if (opportunity.IsEmergency && opportunity.AutoCloseWhenFull)
            {
                //var appliedCount = opportunity.Applications.Count(a => a.Status == ApplicationStatus.Applied);
                if (appliedCount >= opportunity.VolunteersNeeded)
                {
                    TempData["ErrorMessage"] = "This emergency opportunity is already full.";
                }
            }

            // Pass volunteer credit level to the view
            if (volunteer != null)
            {
                ViewBag.VolunteerCreditLevel = volunteer.CreditLevel;
            }

            //By ANGEL
            // Follow info — uses OrganizationId, not OpportunityId
            if (User.IsInRole("Volunteer") && opportunity.Organization != null)
            {
                ViewBag.IsFollowing = volunteer != null &&
                    await _followService.IsFollowingAsync(
                        volunteer.VolunteerId,
                        opportunity.Organization.OrganizationId);

                ViewBag.FollowerCount = await _followService
                    .GetFollowerCountAsync(opportunity.Organization.OrganizationId);
            }
            else
            {
                ViewBag.IsFollowing = false;
                ViewBag.FollowerCount = await _followService
                    .GetFollowerCountAsync(opportunity.Organization.OrganizationId);
            }

            var now = DateTime.Now;
            var today = now.Date;
            var currentTime = now.TimeOfDay;

            bool isToday = opportunity.Date.Date == today;
            bool isFutureDate = opportunity.Date.Date > today;

            bool isOngoing =
                isToday &&
                opportunity.StartTime <= currentTime &&
                opportunity.EndTime > currentTime;

            bool isFuture =
                isFutureDate ||
                (isToday && opportunity.StartTime > currentTime);

            OpportunityStatus timeStatus;

            if (isOngoing)
            {
                timeStatus = OpportunityStatus.Ongoing;
            }
            else if (isFuture)
            {
                timeStatus = OpportunityStatus.Upcoming;
            }
            else
            {
                timeStatus = OpportunityStatus.Past;
            }

            var vm = new OpportunityViewModel
            {
                Opportunity = opportunity,
                TimeStatus = timeStatus,
                AppliedCount = appliedCount,
                SpotsLeft = opportunity.VolunteersNeeded - appliedCount,
                Now = now
            };

            return View(vm);
        }

        //By ANGEL
        public async Task<IActionResult> FollowedOrganizations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null) return Forbid();

            var follows = await _context.Follows
                .Where(f => f.VolunteerId == volunteer.VolunteerId)
                .Include(f => f.Organization)
                    .ThenInclude(o => o.User)
                .OrderByDescending(f => f.FollowedAt)
                .ToListAsync();

            var viewModel = new FollowedOrganizationsViewModel
            {
                FollowedOrgs = follows.Select(f => new FollowedOrgItem
                {
                    Organization = f.Organization,
                    FollowerCount = _context.Follows
                        .Count(x => x.OrganizationId == f.OrganizationId),
                    FollowedAt = f.FollowedAt
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Volunteer")]
        public async Task<IActionResult> Follow(string organizationId, string opportunityId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null) return Forbid();

            await _followService.FollowAsync(volunteer.VolunteerId, organizationId);

            return RedirectToAction("Details", new { id = opportunityId });
        }

        [HttpPost]
        [Authorize(Roles = "Volunteer")]
        public async Task<IActionResult> Unfollow(string organizationId, string opportunityId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null) return Forbid();

            await _followService.UnfollowAsync(volunteer.VolunteerId, organizationId);

            return RedirectToAction("Details", new { id = opportunityId });
        }

        [HttpPost]
        [Authorize(Roles = "Volunteer")]
        public async Task<IActionResult> FollowFromProfile(string organizationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null) return Forbid();

            await _followService.FollowAsync(volunteer.VolunteerId, organizationId);
            return RedirectToAction("Profile", "Organizations", new { id = organizationId });
        }

        [HttpPost]
        [Authorize(Roles = "Volunteer")]
        public async Task<IActionResult> UnfollowFromProfile(string organizationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null) return Forbid();

            await _followService.UnfollowAsync(volunteer.VolunteerId, organizationId);
            return RedirectToAction("Profile", "Organizations", new { id = organizationId });
        }
    }
}
