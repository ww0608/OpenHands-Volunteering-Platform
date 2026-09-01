//By ANGEL - admin side combined

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models.Enums;

namespace OpenHandsVolunteerPlatform.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalOpportunities = await _context.Opportunities.CountAsync();
            ViewBag.TotalOrganizations = await _context.Organizations
                .Where(o => o.VerificationStatus == VerificationStatus.Approved)
                .CountAsync(); 
            ViewBag.TotalVolunteers = await _context.Volunteers.CountAsync();
            ViewBag.UnsolvedReports = await _context.Reports
                .CountAsync(r => r.Status == ReportStatus.Pending);

            // Monthly volunteer participation for chart (last 12 months)
            var monthlyData = new List<object>();
            for (int i = 11; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                var start = new DateTime(month.Year, month.Month, 1);
                var end = start.AddMonths(1);

                var count = await _context.Applications
                    .CountAsync(a => a.AppliedAt >= start && a.AppliedAt < end);

                monthlyData.Add(new
                {
                    month = month.ToString("MMM"),
                    count
                });
            }

            ViewBag.MonthlyData = System.Text.Json.JsonSerializer.Serialize(monthlyData);
            return View();
        }
    }
}