//By ANGEL - admin side combined

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using OpenHandsVolunteerPlatform.Services;
using OpenHandsVolunteerPlatform.ViewModels;
using System.Text;

namespace OpenHandsVolunteerPlatform.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrganizationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;
        private readonly ILogger<OrganizationController> _logger;

        public OrganizationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            EmailService emailService,
            ILogger<OrganizationController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: Admin/Organization/Index (Management)
        public async Task<IActionResult> Index(string search = "")
        {
            var orgs = await _context.Organizations
                .Include(o => o.User)
                .Include(o => o.Opportunities)
                .Where(o => o.VerificationStatus == VerificationStatus.Approved)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                orgs = orgs
                    .Where(o => o.OrganizationName.ToLower().Contains(search) ||
                               (o.User != null && o.User.Email.ToLower().Contains(search)))
                    .ToList();
            }

            ViewBag.CurrentSearch = search;
            return View(orgs);
        }

        // GET: Admin/Organization/VerificationQueue
        public async Task<IActionResult> VerificationQueue(string search = "")
        {
            var orgs = await _context.Organizations
                .Include(o => o.User)
                .Where(o => o.VerificationStatus == VerificationStatus.Pending)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                orgs = orgs
                    .Where(o => o.OrganizationName.ToLower().Contains(search) ||
                               (o.User != null && o.User.Email.ToLower().Contains(search)))
                    .ToList();
            }

            ViewBag.CurrentSearch = search;
            return View(orgs);
        }

        // GET: Admin/Organization/ReviewOrg/5
        public async Task<IActionResult> ReviewOrg(string id)
        {
            var org = await _context.Organizations
                .Include(o => o.User)
                .Include(o => o.Opportunities)
                .FirstOrDefaultAsync(o => o.OrganizationId == id);

            if (org == null) return NotFound();
            return View(org);
            //return PartialView("_ReviewOrganization", org);
        }

        // POST: Admin/Organization/Verify (Approve or Reject from ReviewOrg page)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(string id, bool approve, string? reason)
        {
            var org = await _context.Organizations
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrganizationId == id);
            
            if (org == null) return NotFound();

            org.VerificationStatus = approve ? VerificationStatus.Approved : VerificationStatus.Rejected;
            
            org.VerifiedAt = approve ? DateTime.Now : null;
            org.RejectionReason = approve ? null : reason;

            await _context.SaveChangesAsync();

            // Send email
            if (!string.IsNullOrEmpty(org.User?.Email))
            {
                try
                {
                    var subject = approve
                        ? "Your OpenHands Organization Has Been Approved!"
                        : "Update on Your OpenHands Organization Application";

                    var body = _emailService.OrgVerificationTemplate(
                        org.OrganizationName, approve, reason);

                    await _emailService.SendEmailAsync(org.User.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send verification email to {Email}",
                        org.User.Email);
                }
            }

            TempData["SuccessMessage"] = approve
                ? "Organization approved."
                : "Organization rejected.";
            
            return RedirectToAction(nameof(VerificationQueue));
        }

        //// POST: Admin/Organization/Approve (JSON)
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Approve(string id)
        //{
        //    var org = await _context.Organizations.FindAsync(id);
        //    if (org == null)
        //        return Json(new { success = false, message = "Organization not found" });

        //    org.VerificationStatus = VerificationStatus.Approved;
        //    org.VerifiedAt = DateTime.Now;
        //    await _context.SaveChangesAsync();

        //    return Json(new { success = true, message = "Organization approved successfully" });
        //}

        //// POST: Admin/Organization/Reject (JSON)
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Reject(string id, [FromBody] RejectModel model)
        //{
        //    var org = await _context.Organizations.FindAsync(id);
        //    if (org == null)
        //        return Json(new { success = false, message = "Organization not found" });

        //    org.VerificationStatus = VerificationStatus.Rejected;
        //    org.RejectionReason = model?.Reason;
        //    await _context.SaveChangesAsync();

        //    return Json(new { success = true, message = "Organization rejected" });
        //}

        // POST: Admin/Organization/ToggleLock 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = "Organization account unlocked.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["SuccessMessage"] = "Organization account locked.";
            }

            return RedirectToAction("Index");
        }

        // POST: Admin/Organization/Disable
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable(string id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null)
                return Json(new { success = false, message = "Organization not found" });

            // CLOSE ALL OPPORTUNITIES
            var opportunities = await _context.Opportunities
                .Where(o => o.OrganizationId == id)
                .ToListAsync();

            foreach (var opp in opportunities)
            {
                opp.Status = OpportunityOpenStatus.Close;
            }

            // GET affected applications
            var applications = await _context.Applications
                .Include(a => a.Volunteer)
                    .ThenInclude(v => v.User)
                .Include(a => a.Opportunity)
                .Where(a => opportunities.Select(o => o.OpportunityId).Contains(a.OpportunityId))
                .ToListAsync();
            
            var emailTasks = new List<Task>();

            foreach (var app in applications)
            {
                var email = app.Volunteer?.User?.Email;

                //NOTIFICATION (IMPORTANT)
                _context.Notifications.Add(new Notification
                {
                    UserId = app.Volunteer.User.Id,
                    Message = $"Opportunity '{app.Opportunity.Title}' is no longer available.",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
                ////EMAIL
                //if (!string.IsNullOrEmpty(email))
                //{
                //    emailTasks.Add(_emailService.SendEmailAsync(
                //        email,
                //        "Opportunity Cancelled",
                //        $"The opportunity '{app.Opportunity.Title}' is no longer available because the organization has been deactivated."
                //    ));
                //}
            }

            await Task.WhenAll(emailTasks);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(org.UserId);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            user.LockoutEnd = DateTime.MaxValue;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Organization disabled successfully" });
        }

        // POST: Admin/Organization/Enable (JSON)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable(string id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null)
                return Json(new { success = false, message = "Organization not found" });
            // REOPEN OPPORTUNITIES
            var opportunities = await _context.Opportunities
                .Where(o => o.OrganizationId == id)
                .ToListAsync();

            foreach (var opp in opportunities)
            {
                opp.Status = OpportunityOpenStatus.Open;
            }

            // GET affected applications
            var applications = await _context.Applications
                .Include(a => a.Volunteer)
                    .ThenInclude(v => v.User)
                .Include(a => a.Opportunity)
                .Where(a => opportunities.Select(o => o.OpportunityId).Contains(a.OpportunityId))
                .ToListAsync();


            foreach (var app in applications)
            {
                var email = app.Volunteer?.User?.Email;

                ////EMAIL
                //if (!string.IsNullOrEmpty(email))
                //{
                //    await _emailService.SendEmailAsync(
                //        email,
                //        "Opportunity Available Again",
                //        $"The opportunity '{app.Opportunity.Title}' is now available again."
                //    );
                //}

                //NOTIFICATION
                _context.Notifications.Add(new Notification
                {
                    UserId = app.Volunteer.User.Id,
                    Message = $"Opportunity '{app.Opportunity.Title}' is available again.",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(org.UserId);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            user.LockoutEnd = null;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Organization enabled successfully" });
        }

        //// POST: Admin/Organization/Delete
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Delete(string orgId)
        //{
        //    var org = await _context.Organizations.FindAsync(orgId);
        //    if (org == null) return NotFound();

        //    var user = await _userManager.FindByIdAsync(org.UserId);

        //    _context.Organizations.Remove(org);
        //    await _context.SaveChangesAsync();

        //    if (user != null)
        //        await _userManager.DeleteAsync(user);

        //    TempData["SuccessMessage"] = "Organization deleted.";
        //    return RedirectToAction("Index");
        //}

        // GET: Admin/Organization/Export
        public async Task<IActionResult> Export(string type = "all")
        {
            var orgs = await _context.Organizations
                .Include(o => o.User)
                .ToListAsync();

            if (type == "approved")
                orgs = orgs.Where(o => o.VerificationStatus == VerificationStatus.Approved).ToList();
            else if (type == "pending")
                orgs = orgs.Where(o => o.VerificationStatus == VerificationStatus.Pending).ToList();
            else if (type == "rejected")
                orgs = orgs.Where(o => o.VerificationStatus == VerificationStatus.Rejected).ToList();

            var csv = new StringBuilder();
            csv.AppendLine("Organization Name,Email,Joined Date,Status,Event Count");

            foreach (var o in orgs)
            {
                var eventCount = await _context.Opportunities
                    .CountAsync(op => op.OrganizationId == o.OrganizationId);
                csv.AppendLine($"{o.OrganizationName},{o.User?.Email}," +
                               $"{o.CreatedAt:dd MMM yyyy},{o.VerificationStatus},{eventCount}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"organizations_{DateTime.Now:yyyyMMdd}.csv");
        }

        public async Task<IActionResult> ViewDocument(string id)
        {
            var org = await _context.Organizations.FindAsync(id);
            if (org == null || string.IsNullOrEmpty(org.VerificationDocumentPath))
                return NotFound();

            var fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                org.VerificationDocumentPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(fullPath))
                return NotFound("Document file not found on server.");

            var extension = Path.GetExtension(fullPath).ToLower();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(fileBytes, contentType);
        }


        public class RejectModel
        {
            public string Reason { get; set; }
        }
    }
}