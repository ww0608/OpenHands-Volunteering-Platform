//By ANGEL - admin side combined

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models.Enums;
using System.Security.Claims;

namespace OpenHandsVolunteerPlatform.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Reports queue
        public async Task<IActionResult> Index()
        {
            var reports = await _context.Reports
                .Include(r => r.Reporter)
                .OrderBy(r => r.Status)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Resolve target names
            var targetNames = new Dictionary<string, string>();
            foreach (var r in reports)
            {
                if (targetNames.ContainsKey(r.TargetId)) continue;

                if (r.TargetType == ReportTargetType.Opportunity)
                {
                    var opp = await _context.Opportunities
                        .FirstOrDefaultAsync(o => o.OpportunityId == r.TargetId);
                    targetNames[r.TargetId] = opp?.Title ?? "Deleted opportunity";
                }
                else
                {
                    var org = await _context.Organizations
                        .FirstOrDefaultAsync(o => o.OrganizationId == r.TargetId);
                    targetNames[r.TargetId] = org?.OrganizationName ?? "Deleted organization";
                }
            }

            ViewBag.TargetNames = targetNames;
            return View(reports);
        }

        // Review a single report
        public async Task<IActionResult> Review(string id)
        {
            var report = await _context.Reports
                .Include(r => r.Reporter)
                .FirstOrDefaultAsync(r => r.ReportId == id);

            if (report == null) return NotFound();

            // Load the target name
            if (report.TargetType == ReportTargetType.Opportunity)
            {
                var opp = await _context.Opportunities
                    .FirstOrDefaultAsync(o => o.OpportunityId == report.TargetId);
                ViewBag.TargetName = opp?.Title ?? "Deleted opportunity";
            }
            else
            {
                var org = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.OrganizationId == report.TargetId);
                ViewBag.TargetName = org?.OrganizationName ?? "Deleted organization";
            }

            return View(report);
        }

        // Resolve report
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(string id, string adminNote)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = ReportStatus.Resolved;
            report.AdminNote = adminNote;
            report.ResolvedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Report resolved.";
            return RedirectToAction("Index");
        }

        // Dismiss report
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dismiss(string id, string adminNote)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = ReportStatus.Dismissed;
            report.AdminNote = adminNote;
            report.ResolvedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Report dismissed.";
            return RedirectToAction("Index");
        }

    }
}