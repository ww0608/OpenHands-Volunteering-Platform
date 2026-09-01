using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using OpenHandsVolunteerPlatform.Services;
using OpenHandsVolunteerPlatform.ViewModels;
using System.Security.Claims;
using System.Text;

namespace OpenHandsVolunteerPlatform.Areas.Organization.Controllers
{
    [Area("Organization")]
    [Authorize(Roles = "Organization")]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttendanceController> _logger;
        private readonly ICreditScoreService _creditScoreService;

        public AttendanceController(ApplicationDbContext context, ILogger<AttendanceController> logger, ICreditScoreService creditScoreService)
        {
            _context = context;
            _logger = logger;
            _creditScoreService = creditScoreService;
        }

        // GET: Organization/Attendance/Manage/5
        public async Task<IActionResult> Manage(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var orgId = await GetCurrentOrganizationId();
            if (orgId == null)
                return NotFound();

            var opportunity = await _context.Opportunities
                .Include(o => o.Applications)
                    .ThenInclude(a => a.Volunteer)
                        .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(o => o.OpportunityId == id && o.OrganizationId == orgId);

            if (opportunity == null)
                return NotFound();

            if (opportunity.Date.Date > DateTime.Now.Date)
            {
                TempData["ErrorMessage"] = "Cannot manage attendance for future events.";
                return RedirectToAction("Details", "Opportunity", new { id });
            }

            var viewModel = new ManageAttendanceViewModel
            {
                OpportunityId = opportunity.OpportunityId,
                OpportunityTitle = opportunity.Title,
                EventDate = opportunity.Date,
                StartTime = opportunity.StartTime,
                EndTime = opportunity.EndTime,
                Location = opportunity.Location,
                Volunteers = opportunity.Applications
                    .Where(a => a.Status == ApplicationStatus.Applied)
                    .Select(a => new VolunteerAttendanceViewModel
                    {
                        ApplicationId = a.ApplicationId,
                        VolunteerId = a.VolunteerId,
                        VolunteerName = a.Volunteer?.Name ?? "Unknown",
                        Email = a.Volunteer?.User?.Email ?? "-",
                        Phone = a.Volunteer?.Phone ?? "-",
                        Age = a.Volunteer?.Age ?? 0,
                        AttendanceStatus = a.AttendanceStatus,
                        CheckInTime = a.CheckInTime,
                        CheckOutTime = null
                    }).ToList()
            };
            return View(viewModel);
        }

        // POST: Organization/Attendance/Update
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] List<AttendanceUpdateModel> updates)
        {
            try
            {
                if (updates == null || !updates.Any())
                {
                    return Json(new { success = false, message = "No updates provided" });
                }

                string? opportunityId = null;
                var affectedVolunteerIds = new HashSet<string>();

                foreach (var update in updates)
                {
                    var status = update.AttendanceStatus == "Present"
                        ? AttendanceStatus.Present
                        : update.AttendanceStatus == "Pending"
                            ? AttendanceStatus.Pending
                            : AttendanceStatus.NoShow;

                    var app = await _context.Applications
                        .FirstOrDefaultAsync(a => a.ApplicationId == update.ApplicationId);

                    if (app == null) continue;

                    // capture opportunity once
                    if (string.IsNullOrEmpty(opportunityId))
                    {
                        var appWithOpp = await _context.Applications
                            .FirstOrDefaultAsync(a => a.ApplicationId == update.ApplicationId);

                        opportunityId = appWithOpp?.OpportunityId;
                    }

                    // apply changes
                    app.AttendanceStatus = status;

                    if (!string.IsNullOrEmpty(update.CheckInTime))
                    {
                        var timeParts = update.CheckInTime.Split(':');
                        if (timeParts.Length == 3)
                        {
                            app.CheckInTime = DateTime.Today
                                .AddHours(int.Parse(timeParts[0]))
                                .AddMinutes(int.Parse(timeParts[1]))
                                .AddSeconds(int.Parse(timeParts[2]));
                        }
                    }
                    else
                    {
                        app.CheckInTime = null;
                    }

                    affectedVolunteerIds.Add(app.VolunteerId);
                }

                await _context.SaveChangesAsync();

                // 🔥 recompute derived stats AFTER saving
                foreach (var volunteerId in affectedVolunteerIds)
                {
                    await RecalculateVolunteerStats(volunteerId);
                }

                if (!string.IsNullOrEmpty(opportunityId))
                {
                    await UpdateCreditScoresForCompletedEvent(opportunityId);
                }

                return Json(new { success = true, message = "Attendance updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendance");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task RecalculateVolunteerStats(string volunteerId)
        {
            var volunteer = await _context.Volunteers
                .Include(v => v.Applications)
                    .ThenInclude(a => a.Opportunity)
                .FirstOrDefaultAsync(v => v.VolunteerId == volunteerId);

            if (volunteer == null) return;

            var apps = volunteer.Applications
                .Where(a => a.Status == ApplicationStatus.Applied);

            volunteer.CompletedCount = apps.Count(a => a.AttendanceStatus == AttendanceStatus.Present);
            volunteer.NoShowCount = apps.Count(a => a.AttendanceStatus == AttendanceStatus.NoShow);

            volunteer.TotalHours = apps
                .Where(a => a.AttendanceStatus == AttendanceStatus.Present)
                .Sum(a =>
                    (int)Math.Ceiling(
                        (a.Opportunity.EndTime - a.Opportunity.StartTime).TotalHours
                    )
                );

            volunteer.CreditLevel =
                volunteer.CompletedCount >= 20 ? CreditLevel.Core :
                volunteer.CompletedCount >= 5 ? CreditLevel.Growing :
                CreditLevel.Inactive;

            await _context.SaveChangesAsync();
        }

        // Helper method to update credit scores after event completion
        private async Task UpdateCreditScoresForCompletedEvent(string opportunityId)
        {
            var opportunity = await _context.Opportunities
                .Include(o => o.Applications)
                    .ThenInclude(a => a.Volunteer)
                .FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);

            if (opportunity == null)
                return;

            // Only update if event has ended
            var eventEndDateTime = opportunity.Date.Add(opportunity.EndTime);
            if (eventEndDateTime > DateTime.Now)
                return;

            foreach (var app in opportunity.Applications.Where(a => a.Status == ApplicationStatus.Applied))
            {
                if (app.Volunteer != null)
                {
                    string reason = "";

                    if (app.AttendanceStatus == AttendanceStatus.Present)
                    {
                        app.Volunteer.CompletedCount++;
                        var hours = (opportunity.EndTime - opportunity.StartTime).TotalHours;
                        app.Volunteer.TotalHours += (int)Math.Ceiling(hours);
                        reason = $"Attended event: {opportunity.Title}";
                    }
                    else if (app.AttendanceStatus == AttendanceStatus.NoShow)
                    {
                        app.Volunteer.NoShowCount++;
                        reason = $"No-show for event: {opportunity.Title}";
                    }

                    await _creditScoreService.UpdateCreditScore(
                        app.Volunteer,
                        reason,
                        opportunityId,
                        opportunity.OrganizationId
                    );
                }
            }
            await _context.SaveChangesAsync();
        }

        // GET: Organization/Attendance/Retrospective/5
        public async Task<IActionResult> Retrospective(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var orgId = await GetCurrentOrganizationId();
            if (orgId == null)
                return NotFound();

            var opportunity = await _context.Opportunities
                .Include(o => o.Applications)
                    .ThenInclude(a => a.Volunteer)
                        .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(o => o.OpportunityId == id && o.OrganizationId == orgId);

            if (opportunity == null)
                return NotFound();

            var now = DateTime.Now;

            bool notRetrospective =
                opportunity.Date.Date > now.Date ||
                (opportunity.Date.Date == now.Date && now.TimeOfDay <= opportunity.EndTime);

            if (notRetrospective)
            {
                return RedirectToAction("Manage", new { id });
            }

            var eventHours = (opportunity.EndTime - opportunity.StartTime).TotalHours;
            var appliedApplications = opportunity.Applications
                .Where(a => a.Status == ApplicationStatus.Applied)
                .ToList();

            int totalVolunteers = appliedApplications.Count;
            int presentCount = appliedApplications.Count(a => a.AttendanceStatus == AttendanceStatus.Present);
            int noShowCount = appliedApplications.Count(a => a.AttendanceStatus == AttendanceStatus.NoShow);
            int pendingCount = appliedApplications.Count(a => a.AttendanceStatus == AttendanceStatus.Pending);

            double attendanceRate = totalVolunteers > 0
                ? Math.Round((presentCount / (double)totalVolunteers) * 100, 1)
                : 0;

            int totalVolunteerHours = (int)(presentCount * eventHours);

            var creditBreakdown = new
            {
                High = appliedApplications.Count(a => a.Volunteer?.CreditLevel == CreditLevel.Core),
                Average = appliedApplications.Count(a => a.Volunteer?.CreditLevel == CreditLevel.Growing),
                Low = appliedApplications.Count(a => a.Volunteer?.CreditLevel == CreditLevel.Inactive)
            };

            var viewModel = new ManageAttendanceViewModel
            {
                OpportunityId = opportunity.OpportunityId,
                OpportunityTitle = opportunity.Title,
                EventDate = opportunity.Date,
                StartTime = opportunity.StartTime,
                EndTime = opportunity.EndTime,
                Location = opportunity.Location,
                Volunteers = appliedApplications
                    .Select(a => new VolunteerAttendanceViewModel
                    {
                        ApplicationId = a.ApplicationId,
                        VolunteerId = a.VolunteerId,
                        VolunteerName = a.Volunteer?.Name ?? "Unknown",
                        Email = a.Volunteer?.User?.Email ?? "-",
                        Phone = a.Volunteer?.Phone ?? "-",
                        Age = a.Volunteer?.Age ?? 0,
                        AttendanceStatus = a.AttendanceStatus
                    }).ToList()
            };

            ViewBag.EventHours = eventHours;
            ViewBag.TotalVolunteerHours = totalVolunteerHours;
            ViewBag.AttendanceRate = attendanceRate;
            ViewBag.PresentCount = presentCount;
            ViewBag.NoShowCount = noShowCount;
            ViewBag.PendingCount = pendingCount;
            ViewBag.CreditBreakdown = creditBreakdown;
            ViewBag.CompletedDate = opportunity.Date;

            return View(viewModel);
        }

        // GET: Organization/Attendance/ExportCsv/5
        public async Task<IActionResult> ExportCsv(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var orgId = await GetCurrentOrganizationId();
            if (orgId == null)
                return NotFound();

            var opportunity = await _context.Opportunities
                .Include(o => o.Applications)
                    .ThenInclude(a => a.Volunteer)
                        .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(o => o.OpportunityId == id && o.OrganizationId == orgId);

            if (opportunity == null)
                return NotFound();

            var volunteers = opportunity.Applications
                .Where(a => a.Status == ApplicationStatus.Applied)
                .Select(a => new
                {
                    Name = a.Volunteer?.Name ?? "Unknown",
                    Email = a.Volunteer?.User?.Email ?? "-",
                    Phone = a.Volunteer?.Phone ?? "-",
                    Age = a.Volunteer?.Age ?? 0,
                    Attendance = a.AttendanceStatus.ToString(),
                    CreditLevel = a.Volunteer?.CreditLevel.ToString() ?? "Unknown"
                }).ToList();

            var csv = new StringBuilder();
            csv.AppendLine("Name,Email,Phone,Age,Attendance Status,Credit Level");

            foreach (var v in volunteers)
            {
                csv.AppendLine($"{v.Name},{v.Email},{v.Phone},{v.Age},{v.Attendance},{v.CreditLevel}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"attendance_{opportunity.Title}_{DateTime.Now:yyyyMMdd}.csv");
        }

        // GET: Organization/Attendance/ExportDetailedCsv/5
        public async Task<IActionResult> ExportDetailedCsv(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var orgId = await GetCurrentOrganizationId();
            if (orgId == null)
                return NotFound();

            var opportunity = await _context.Opportunities
                .Include(o => o.Applications)
                    .ThenInclude(a => a.Volunteer)
                        .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(o => o.OpportunityId == id && o.OrganizationId == orgId);

            if (opportunity == null)
                return NotFound();

            var eventHours = (opportunity.EndTime - opportunity.StartTime).TotalHours;
            var appliedApplications = opportunity.Applications
                .Where(a => a.Status == ApplicationStatus.Applied)
                .ToList();

            var presentCount = appliedApplications.Count(a => a.AttendanceStatus == AttendanceStatus.Present);
            var noShowCount = appliedApplications.Count(a => a.AttendanceStatus == AttendanceStatus.NoShow);
            var attendanceRate = appliedApplications.Count > 0
                ? Math.Round((presentCount / (double)appliedApplications.Count) * 100, 1)
                : 0;

            var csv = new StringBuilder();

            csv.AppendLine($"Event Report: {opportunity.Title}");
            csv.AppendLine($"Date,{opportunity.Date:dd MMMM yyyy}");
            csv.AppendLine($"Time,{opportunity.StartTime:hh\\:mm} - {opportunity.EndTime:hh\\:mm}");
            csv.AppendLine($"Location,{opportunity.Location}");
            csv.AppendLine($"Total Volunteers,{appliedApplications.Count}");
            csv.AppendLine($"Present,{presentCount}");
            csv.AppendLine($"No-Show,{noShowCount}");
            csv.AppendLine($"Attendance Rate,{attendanceRate}%");
            csv.AppendLine($"Total Volunteer Hours,{presentCount * eventHours}");
            csv.AppendLine("");

            csv.AppendLine("Name,Email,Phone,Age,Credit Level,Attendance Status");

            foreach (var app in appliedApplications)
            {
                var volunteer = app.Volunteer;
                var creditLevel = volunteer?.CreditLevel.ToString() ?? "Unknown";
                var attendanceStatus = app.AttendanceStatus.ToString();

                csv.AppendLine($"{volunteer?.Name},{volunteer?.User?.Email},{volunteer?.Phone},{volunteer?.Age},{creditLevel},{attendanceStatus}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var filename = $"retrospective_{opportunity.Title}_{DateTime.Now:yyyyMMdd}.csv";

            return File(bytes, "text/csv", filename);
        }

        // GET: Organization/Attendance/ExportPdf/5
        public async Task<IActionResult> ExportPdf(string id)
        {
            //Fix by ANGEL
            if (string.IsNullOrEmpty(id)) return NotFound();

            var orgId = await GetCurrentOrganizationId();
            if (orgId == null) return NotFound();

            var opportunity = await _context.Opportunities
                .Include(o => o.Organization)
                .Include(o => o.Applications)
                    .ThenInclude(a => a.Volunteer)
                        .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(o => o.OpportunityId == id && o.OrganizationId == orgId);

            if (opportunity == null) return NotFound();

            var appliedApplications = opportunity.Applications
                .Where(a => a.Status == ApplicationStatus.Applied)
                .ToList();

            var presentCount = appliedApplications.Count(a => a.AttendanceStatus == AttendanceStatus.Present);
            var noShowCount = appliedApplications.Count(a => a.AttendanceStatus == AttendanceStatus.NoShow);
            var pendingCount = appliedApplications.Count(a => a.AttendanceStatus == AttendanceStatus.Pending);
            var attendanceRate = appliedApplications.Count > 0
                ? Math.Round((presentCount / (double)appliedApplications.Count) * 100, 1)
                : 0;

            var rows = new System.Text.StringBuilder();
            int i = 1;
            foreach (var app in appliedApplications)
            {
                var statusColor = app.AttendanceStatus == AttendanceStatus.Present ? "#2d7a2d"
                                : app.AttendanceStatus == AttendanceStatus.NoShow ? "#e74c3c"
                                : "#b85e00";
                rows.Append($@"
            <tr>
                <td>{i++}</td>
                <td><strong>{app.Volunteer?.Name ?? "-"}</strong></td>
                <td>{app.Volunteer?.User?.Email ?? "-"}</td>
                <td>{app.Volunteer?.Phone ?? "-"}</td>
                <td style='color:{statusColor}; font-weight:600;'>{app.AttendanceStatus}</td>
                <td>{app.CheckInTime?.ToString(@"hh\:mm") ?? "-"}</td>
            </tr>");
            }

            var html = $@"<!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8' />
                <title>Attendance Report — {opportunity.Title}</title>
                <style>
                    body {{ font-family: sans-serif; padding: 2rem; color: #2d3e4f; }}
                    h1   {{ color: #3f4079; font-size: 1.6rem; margin-bottom: 0.25rem; }}
                    .meta {{ color: #8e94a7; font-size: 0.9rem; margin-bottom: 2rem; }}
                    .stats {{ display: flex; gap: 2rem; margin-bottom: 2rem; }}
                    .stat  {{ background: #f5f7ff; border-radius: 12px; padding: 1rem 1.5rem; text-align:center; }}
                    .stat-num {{ font-size: 2rem; font-weight: 700; color: #3f4079; }}
                    .stat-lbl {{ font-size: 0.8rem; color: #8e94a7; text-transform: uppercase; }}
                    table {{ width: 100%; border-collapse: collapse; }}
                    th    {{ background: #3f4079; color: white; padding: 0.6rem 1rem; text-align: left; font-size: 0.85rem; }}
                    td    {{ padding: 0.6rem 1rem; border-bottom: 1px solid #edf2f9; font-size: 0.9rem; }}
                    tr:nth-child(even) td {{ background: #f8fafc; }}
                    .footer {{ margin-top: 2rem; color: #8e94a7; font-size: 0.8rem; text-align: center; }}
                    @media print {{
                        button {{ display: none; }}
                    }}
                </style>
            </head>
            <body>
                <h1>Attendance Report</h1>
                <div class='meta'>
                    <strong>{opportunity.Title}</strong> &nbsp;|&nbsp;
                    {opportunity.Organization?.OrganizationName} &nbsp;|&nbsp;
                    {opportunity.Date:dd MMMM yyyy} &nbsp;|&nbsp;
                    {opportunity.StartTime:hh\:mm} – {opportunity.EndTime:hh\:mm} &nbsp;|&nbsp;
                    {opportunity.Location}
                </div>

                <div class='stats'>
                    <div class='stat'><div class='stat-num'>{appliedApplications.Count}</div><div class='stat-lbl'>Total</div></div>
                    <div class='stat'><div class='stat-num' style='color:#2d7a2d'>{presentCount}</div><div class='stat-lbl'>Present</div></div>
                    <div class='stat'><div class='stat-num' style='color:#e74c3c'>{noShowCount}</div><div class='stat-lbl'>No-Show</div></div>
                    <div class='stat'><div class='stat-num' style='color:#b85e00'>{pendingCount}</div><div class='stat-lbl'>Pending</div></div>
                    <div class='stat'><div class='stat-num'>{attendanceRate}%</div><div class='stat-lbl'>Rate</div></div>
                </div>

                <table>
                    <thead>
                        <tr>
                            <th>#</th><th>Name</th><th>Email</th><th>Phone</th>
                            <th>Attendance</th><th>Check-In</th>
                        </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                </table>

                <div class='footer'>Generated on {DateTime.Now:dd MMMM yyyy, HH:mm} &nbsp;|&nbsp; OpenHands Volunteer Platform</div>

                <script>window.onload = function() {{ window.print(); }}</script>
            </body>
            </html>";

            return Content(html, "text/html");
        }

        // Helper method to get current organization ID
        private async Task<string?> GetCurrentOrganizationId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.UserId == userId);
            return org?.OrganizationId;
        }
    }

    // Model for batch updates from client
    public class AttendanceUpdateModel
    {
        public string ApplicationId { get; set; }
        public string AttendanceStatus { get; set; }
        public string CheckInTime { get; set; }
    }
}