using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;

namespace OpenHandsVolunteerPlatform.Controllers
{
    public class SeedController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SeedController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Navigate to /Seed/Run to seed database
        public async Task<IActionResult> Run()
        {
            // Only allow in development environment
            if (!_env.IsDevelopment())
            {
                return NotFound("Seeding is only available in development environment.");
            }

            try
            {
                await SeedData.InitializeAsync(HttpContext.RequestServices);
                return Content("Database seeded successfully!");
            }
            catch (Exception ex)
            {
                return Content($"Error seeding database: {ex.Message}");
            }
        }

        // GET: /Seed/Reset
        public async Task<IActionResult> Reset()
        {
            if (!_env.IsDevelopment())
            {
                return NotFound("Seeding is only available in development environment.");
            }

            try
            {
                // Clear all data in correct order (respect foreign keys)
                await ClearAllData();

                //// Reseed
                //await SeedData.InitializeAsync(HttpContext.RequestServices);

                return Content("Database reset successfully!");
            }
            catch (Exception ex)
            {
                return Content($"Error resetting database: {ex.Message}");
            }
        }

        private async Task ClearAllData()
        {
            // Clear in reverse order of dependencies
            _context.Certificates.RemoveRange(await _context.Certificates.ToListAsync());
            _context.Notifications.RemoveRange(await _context.Notifications.ToListAsync());
            _context.Follows.RemoveRange(await _context.Follows.ToListAsync());
            _context.Reports.RemoveRange(await _context.Reports.ToListAsync());
            _context.OrganizationRatings.RemoveRange(await _context.OrganizationRatings.ToListAsync());
            _context.Applications.RemoveRange(await _context.Applications.ToListAsync());
            _context.Opportunities.RemoveRange(await _context.Opportunities.ToListAsync());
            _context.Organizations.RemoveRange(await _context.Organizations.ToListAsync());
            _context.Volunteers.RemoveRange(await _context.Volunteers.ToListAsync());

            // Remove users (except don't remove yourself if logged in)
            var currentUserId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var usersToDelete = await _context.Users
                .Where(u => u.Id != currentUserId)
                .ToListAsync();
            _context.Users.RemoveRange(usersToDelete);

            await _context.SaveChangesAsync();
        }

    }
}