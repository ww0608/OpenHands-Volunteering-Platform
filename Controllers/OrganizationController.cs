using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Services;

//By ANGEL
namespace OpenHandsVolunteerPlatform.Controllers
{
    public class OrganizationController : Controller
    {
        private readonly FollowService _followService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public OrganizationController(
            FollowService followService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _followService = followService;
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Volunteer")]
        public async Task<IActionResult> Follow(string organizationId)
        {
            var user = await _userManager.GetUserAsync(User);
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == user.Id);

            if (volunteer == null) return Forbid();

            await _followService.FollowAsync(volunteer.VolunteerId, organizationId);
            return RedirectToAction("Details", new { id = organizationId });
        }

        [HttpPost]
        [Authorize(Roles = "Volunteer")]
        public async Task<IActionResult> Unfollow(string organizationId)
        {
            var user = await _userManager.GetUserAsync(User);
            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == user.Id);

            if (volunteer == null) return Forbid();

            await _followService.UnfollowAsync(volunteer.VolunteerId, organizationId);
            return RedirectToAction("Details", new { id = organizationId });
        }
    }
}