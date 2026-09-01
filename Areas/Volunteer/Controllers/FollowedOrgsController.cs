using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Services;
using OpenHandsVolunteerPlatform.ViewModels;
using System.Security.Claims;

//By ANGEL
namespace OpenHandsVolunteerPlatform.Areas.Volunteer.Controllers
{
    [Area("Volunteer")]
    [Authorize(Roles = "Volunteer")]
    public class FollowedOrgsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FollowService _followService;

        public FollowedOrgsController(
            ApplicationDbContext context,
            FollowService followService)
        {
            _context = context;
            _followService = followService;
        }

        //ww fix
        public async Task<IActionResult> Index(string search = "", string type = "", string sort = "recent")
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

            // Build the list of followed organizations
            var followedOrgs = follows.Select(f => new FollowedOrgItem
            {
                Organization = f.Organization,
                FollowerCount = _context.Follows.Count(x => x.OrganizationId == f.OrganizationId),
                FollowedAt = f.FollowedAt
            }).ToList();
            // Apply search filter (organization name)
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                followedOrgs = followedOrgs
                    .Where(o => o.Organization.OrganizationName.ToLower().Contains(search))
                    .ToList();
            }
            //// Apply mission filter (search in mission)
            //if (!string.IsNullOrEmpty(mission))
            //{
            //    mission = mission.ToLower();
            //    followedOrgs = followedOrgs
            //        .Where(o => (o.Organization.Mission != null && o.Organization.Mission.ToLower().Contains(mission)) ||
            //                    o.Organization.OrganizationName.ToLower().Contains(mission))
            //        .ToList();
            //}
            // Apply sorting
            switch (sort)
            {
                case "oldest":
                    followedOrgs = followedOrgs.OrderBy(o => o.FollowedAt).ToList();
                    break;
                case "name":
                    followedOrgs = followedOrgs.OrderBy(o => o.Organization.OrganizationName).ToList();
                    break;
                case "followers":
                    followedOrgs = followedOrgs.OrderByDescending(o => o.FollowerCount).ToList();
                    break;
                case "recent":
                default:
                    followedOrgs = followedOrgs.OrderByDescending(o => o.FollowedAt).ToList();
                    break;
            }
            // Store current filter values for the view
            ViewBag.CurrentSearch = search;
            //ViewBag.CurrentMission = mission;
            ViewBag.CurrentSort = sort;

            var viewModel = new FollowedOrganizationsViewModel
            {
                FollowedOrgs = followedOrgs
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Unfollow(string organizationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null) return Forbid();

            await _followService.UnfollowAsync(volunteer.VolunteerId, organizationId);

            return RedirectToAction("Index");
        }
    }
}