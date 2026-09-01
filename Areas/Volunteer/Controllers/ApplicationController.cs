using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.ViewModels;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using System.Security.Claims;
//By ANGEL
using OpenHandsVolunteerPlatform.Services;


[Area("Volunteer")]
[Authorize(Roles = "Volunteer")]
public class ApplicationController : Controller
{
    //By ANGEL
    private readonly EmailService _emailService;
    private readonly ApplicationDbContext _context;
    //private const int MAX_WAITLIST_SIZE = 5;

    //By ANGEL
    // Combined Constructor
    public ApplicationController(
        ApplicationDbContext context,
        EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // ww fix 10% waitlist
    private int GetMaxWaitlistSize(int volunteersNeeded)
    {
        // Pure 10% of volunteers needed, rounded up
        return (int)Math.Ceiling(volunteersNeeded * 0.10);
    }

    // Helper methods to calculate current state
    private async Task<(int currentVolunteers, int waitlistCount)> GetOpportunityCounts(string opportunityId)
    {
        var applications = await _context.Applications
            .Where(a => a.OpportunityId == opportunityId)
            .ToListAsync();

        var currentVolunteers = applications.Count(a => a.Status == ApplicationStatus.Applied);
        var waitlistCount = applications.Count(a => a.Status == ApplicationStatus.Waitlist);

        return (currentVolunteers, waitlistCount);
    }

    private bool HasAvailableSlot(int currentVolunteers, int volunteersNeeded)
    {
        return currentVolunteers < volunteersNeeded;
    }

    //fix by WW
    private bool IsWaitlistFull(int waitlistCount, int volunteersNeeded)
    {
        int maxWaitlist = GetMaxWaitlistSize(volunteersNeeded);
        return waitlistCount >= maxWaitlist;
    }
    //private bool IsWaitlistFull(int waitlistCount)
    //{
    //    return waitlistCount >= MAX_WAITLIST_SIZE;
    //}

    // GET: /Volunteer/Application/Apply/5
    [HttpGet]
    public async Task<IActionResult> Apply(string id)
    {
        var opportunity = await _context.Opportunities
            .Include(o => o.Organization)
                       .ThenInclude(org => org.User)
           .Include(o => o.Applications)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opportunity == null)
            return NotFound();

        // Block application if org is locked or user deleted
        var orgUser = opportunity.Organization?.User;
        bool orgIsLocked = orgUser == null
          || (orgUser.LockoutEnd.HasValue &&
        orgUser.LockoutEnd > DateTimeOffset.UtcNow);

        if (orgIsLocked)
        {
            TempData["ErrorMessage"] = "This organization is currently inactive. You cannot apply for their opportunities.";
            return RedirectToAction("Index", "Opportunities");
        }

        // Check if opportunity is still open
        if (opportunity.Status != OpportunityOpenStatus.Open)
        {
            TempData["ErrorMessage"] = "This opportunity is no longer accepting applications.";
            return RedirectToAction("Details", "Opportunities", new { id });
        }

        // Check if application deadline has passed
        if (opportunity.ApplicationCloseDate < DateTime.Now)
        {
            TempData["ErrorMessage"] = "Sorry, the application deadline has passed.";
            return RedirectToAction("Details", "Opportunities", new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var volunteer = await _context.Volunteers
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.UserId == userId);

        if (volunteer == null)
        {
            return RedirectToAction("Create", "Profile", new { area = "", userType = "Volunteer" });
        }

        //By WW
        // EMERGENCY MODE: Check credit score requirement FIRST
        if (opportunity.IsEmergency && opportunity.MinimumCreditLevel.HasValue)
        {
            int volunteerLevel = (int)volunteer.CreditLevel;
            int requiredLevel = (int)opportunity.MinimumCreditLevel.Value;

            if (volunteerLevel < requiredLevel)
            {
                string requiredLevelText = requiredLevel == 3 ? "HIGH" : "AVERAGE";
                string currentLevelText = volunteerLevel == 3 ? "HIGH" : volunteerLevel == 2 ? "AVERAGE" : "LOW";

                TempData["ErrorMessage"] = $"This emergency opportunity requires {requiredLevelText} credit level. Your current credit level is {currentLevelText}. You cannot apply.";
                return RedirectToAction("Details", "Opportunities", new { id });
            }
        }

        // Check if already applied
        var existingApplication = await _context.Applications
            .FirstOrDefaultAsync(a => a.OpportunityId == id && a.VolunteerId == volunteer.VolunteerId);

        if (existingApplication != null)
        {
            TempData["ErrorMessage"] = existingApplication.Status == ApplicationStatus.Waitlist
                ? "You are already on the waitlist for this opportunity."
                : "You have already applied for this opportunity.";
            return RedirectToAction(nameof(Index));
        }

        // ⭐ CONFLICT DETECTION - Strict check for any overlapping applications
        var conflictingApplications = await _context.Applications
            .Include(a => a.Opportunity)
            .Where(a => a.VolunteerId == volunteer.VolunteerId)
            .Where(a => a.Status == ApplicationStatus.Applied) // Only check active applications
            .Where(a => a.Opportunity.Date.Date == opportunity.Date.Date)
            .Where(a => a.Opportunity.StartTime < opportunity.EndTime &&
                       a.Opportunity.EndTime > opportunity.StartTime)
            .ToListAsync();

        // Get current counts
        var (currentVolunteers, waitlistCount) = await GetOpportunityCounts(id);

        var viewModel = new ApplyViewModel
        {
            Opportunity = opportunity,
            Volunteer = volunteer,
            HasConflicts = conflictingApplications.Any(),
            ConflictingOpportunities = conflictingApplications.Select(a => a.Opportunity).ToList(),
            CurrentVolunteers = currentVolunteers,
            WaitlistCount = waitlistCount,
            HasAvailableSlot = HasAvailableSlot(currentVolunteers, opportunity.VolunteersNeeded),
            // ww fix 10% waitlist
            IsWaitlistFull = IsWaitlistFull(waitlistCount, opportunity.VolunteersNeeded),
            MaxWaitlistSize = GetMaxWaitlistSize(opportunity.VolunteersNeeded)
            //IsWaitlistFull = IsWaitlistFull(waitlistCount),
            //MaxWaitlistSize = MAX_WAITLIST_SIZE
        };

        // If there are conflicts, show the view with warning but don't allow application
        return View(viewModel);
    }

    // POST: /Volunteer/Application/ConfirmApply/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmApply(string id)
    {
        var opportunity = await _context.Opportunities
            .Include(o => o.Organization)
            .ThenInclude(org => org.User)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opportunity == null)
            return NotFound();
        var orgUser = opportunity?.Organization?.User;

        bool orgIsLocked = orgUser == null || (orgUser.LockoutEnd.HasValue && orgUser.LockoutEnd > DateTimeOffset.UtcNow);

        if (orgIsLocked)
        {
            TempData["ErrorMessage"] = "This organization is currently inactive.";
            return RedirectToAction("Index", "Opportunities");
        }

        // Double-check deadline
        if (opportunity.ApplicationCloseDate < DateTime.Now)
        {
            TempData["ErrorMessage"] = "Sorry, the application deadline has passed.";
            return RedirectToAction("Details", "Opportunity", new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var volunteer = await _context.Volunteers
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.UserId == userId);

        if (volunteer == null)
            return NotFound();

        //By WW
        // EMERGENCY MODE: Check credit score requirement before applying
        if (opportunity.IsEmergency && opportunity.MinimumCreditLevel.HasValue)
        {
            int volunteerLevel = (int)volunteer.CreditLevel;
            int requiredLevel = (int)opportunity.MinimumCreditLevel.Value;

            if (volunteerLevel < requiredLevel)
            {
                string requiredLevelText = requiredLevel == 3 ? "HIGH" : "AVERAGE";
                TempData["ErrorMessage"] = $"This emergency opportunity requires {requiredLevelText} credit level. You do not meet the requirement.";
                return RedirectToAction("Details", "Opportunities", new { id });
            }
        }

        // EMERGENCY MODE: Check if emergency opportunity is full (no waitlist)
        if (opportunity.IsEmergency)
        {
            var emergencyFullCheck = await _context.Applications
                .CountAsync(a => a.OpportunityId == id && a.Status == ApplicationStatus.Applied);

            if (emergencyFullCheck >= opportunity.VolunteersNeeded)
            {
                TempData["ErrorMessage"] = "This emergency opportunity is full.";
                return RedirectToAction("Details", "Opportunities", new { id });
            }
        }


        // ⭐ CONFLICT DETECTION - Strict check - BLOCK if conflict exists
        var hasConflict = await _context.Applications
            .Include(a => a.Opportunity)
            .Where(a => a.VolunteerId == volunteer.VolunteerId)
            .Where(a => a.Status == ApplicationStatus.Applied)
            .Where(a => a.Opportunity.Date.Date == opportunity.Date.Date)
            .Where(a => a.Opportunity.StartTime < opportunity.EndTime &&
                       a.Opportunity.EndTime > opportunity.StartTime)
            .AnyAsync();

        if (hasConflict)
        {
            TempData["ErrorMessage"] = "Cannot apply due to schedule conflict with an existing approved application.";
            return RedirectToAction(nameof(Apply), new { id });
        }

        // Check if already applied (double-check for race conditions)
        var existingApplication = await _context.Applications
            .FirstOrDefaultAsync(a => a.OpportunityId == id && a.VolunteerId == volunteer.VolunteerId);

        if (existingApplication != null)
        {
            TempData["ErrorMessage"] = "You have already applied.";
            return RedirectToAction(nameof(Index));
        }

        // Get current counts
        var (currentVolunteerCount, waitlistCount) = await GetOpportunityCounts(id);

        // Use transaction for data consistency
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var application = new Application
            {
                ApplicationId = Guid.NewGuid().ToString(),
                OpportunityId = id,
                VolunteerId = volunteer.VolunteerId,
                AppliedAt = DateTime.Now,
                AttendanceStatus = AttendanceStatus.Pending
            };

            // EMERGENCY MODE: No waitlist for emergency opportunities
            if (opportunity.IsEmergency)
            {
                if (HasAvailableSlot(currentVolunteerCount, opportunity.VolunteersNeeded))
                {
                    application.Status = ApplicationStatus.Applied;
                    TempData["SuccessMessage"] = "Successfully applied for emergency opportunity!";
                }
                else
                {
                    TempData["ErrorMessage"] = "This emergency opportunity is full.";
                    await transaction.RollbackAsync();
                    return RedirectToAction("Details", "Opportunities", new { id });
                }
            }
            // Normal opportunities have waitlist
            else
            {
                if (HasAvailableSlot(currentVolunteerCount, opportunity.VolunteersNeeded))
                {
                    application.Status = ApplicationStatus.Applied;
                    TempData["SuccessMessage"] = "Application submitted successfully! You're registered for this opportunity.";
                }
                //else if (!IsWaitlistFull(waitlistCount))
                //{
                //    application.Status = ApplicationStatus.Waitlist;
                //    var waitlistPosition = waitlistCount + 1;
                //    TempData["InfoMessage"] = $"The opportunity is currently full. You've been added to the waitlist (position {waitlistPosition} of {MAX_WAITLIST_SIZE}).";
                //}
                else if (!IsWaitlistFull(waitlistCount, opportunity.VolunteersNeeded))
                {
                    application.Status = ApplicationStatus.Waitlist;
                    var waitlistPosition = waitlistCount + 1;
                    int maxWaitlist = GetMaxWaitlistSize(opportunity.VolunteersNeeded);
                    TempData["InfoMessage"] = $"The opportunity is currently full. You've been added to the waitlist (position {waitlistPosition} of {maxWaitlist}).";
                }
                else
                {
                    TempData["ErrorMessage"] = "Sorry, this opportunity is full and the waitlist has reached maximum capacity.";
                    await transaction.RollbackAsync();
                    return RedirectToAction("Details", "Opportunity", new { id });
                }
            }


            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            //By ANGEL

            // Notify the organization
            var org = await _context.Organizations
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrganizationId == opportunity.OrganizationId);

            if (org != null)
            {
                var statusText = application.Status == ApplicationStatus.Waitlist
                    ? "added to waitlist for"
                    : "applied for";

                _context.Notifications.Add(new Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    UserId = org.UserId,
                    Type = NotificationType.NewOpportunity,
                    Message = $"{volunteer.Name} has {statusText} \"{opportunity.Title}\".",
                    RelatedId = application.OpportunityId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }
            //end ANGEL

            await transaction.CommitAsync();
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "An error occurred while processing your application.";
            return RedirectToAction(nameof(Apply), new { id });
        }
    }

    // GET: /Volunteer/Application
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var volunteer = await _context.Volunteers
            .Include(v => v.Applications)
                .ThenInclude(a => a.Opportunity)
                    .ThenInclude(o => o.Organization)
            .FirstOrDefaultAsync(v => v.UserId == userId);

        if (volunteer == null)
            return NotFound();

        // Get all applications
        var allApplications = volunteer.Applications.ToList();

        //fix by NAGEL
        // Load certificates and attach to applications
        var opportunityIds = allApplications.Select(a => a.OpportunityId).ToList();
        var certificates = await _context.Certificates
            .Where(c => c.VolunteerId == volunteer.VolunteerId
                     && opportunityIds.Contains(c.OpportunityId))
            .ToListAsync();

        foreach (var app in allApplications)
        {
            app.Certificate = certificates
                .FirstOrDefault(c => c.OpportunityId == app.OpportunityId);
        }

        // Calculate waitlist positions for waitlisted applications
        var waitlistApplications = new List<WaitlistApplicationViewModel>();
        foreach (var app in allApplications.Where(a => a.Status == ApplicationStatus.Waitlist))
        {
            var position = await _context.Applications
                .Where(a => a.OpportunityId == app.OpportunityId)
                .Where(a => a.Status == ApplicationStatus.Waitlist)
                .Where(a => a.AppliedAt <= app.AppliedAt)
                .CountAsync();

            waitlistApplications.Add(new WaitlistApplicationViewModel
            {
                Application = app,
                WaitlistPosition = position,
                TotalWaitlist = await _context.Applications
                    .CountAsync(a => a.OpportunityId == app.OpportunityId &&
                                    a.Status == ApplicationStatus.Waitlist)
            });
        }

        var viewModel = new VolunteerApplicationsViewModel
        {

            UpcomingApplications = allApplications
                .Where(a =>
                    a.Status == ApplicationStatus.Applied &&
                    (a.Opportunity.Date.Add(a.Opportunity.StartTime) > DateTime.Now))
                .OrderBy(a => a.Opportunity.Date)
                .ThenBy(a => a.Opportunity.StartTime)
                .ToList(),

            PastApplications = allApplications
                .Where(a =>
                    a.Status == ApplicationStatus.Applied &&
                    (a.Opportunity.Date.Add(a.Opportunity.EndTime) < DateTime.Now))
                .OrderByDescending(a => a.Opportunity.Date)
                .ToList(),

            OngoingApplications = allApplications
                .Where(a =>
                    a.Status == ApplicationStatus.Applied &&
                    a.Opportunity.Date.Add(a.Opportunity.StartTime) <= DateTime.Now &&
                    a.Opportunity.Date.Add(a.Opportunity.EndTime) >= DateTime.Now)
                .OrderBy(a => a.Opportunity.Date)
                .ThenBy(a => a.Opportunity.StartTime)
                .ToList(),

            WaitlistApplications = waitlistApplications
                .OrderBy(w => w.WaitlistPosition)
                .ToList(),

            WithdrawnApplications = allApplications
                .Where(a => a.Status == ApplicationStatus.Withdrawn ||
                           a.Status == ApplicationStatus.Removed)
                .OrderByDescending(a => a.AppliedAt)
                .ToList()
        };

        return View(viewModel);
    }

    // POST: /Volunteer/Application/Withdraw/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.UserId == userId);

        var application = await _context.Applications
            .Include(a => a.Opportunity)
            .FirstOrDefaultAsync(a => a.ApplicationId == id &&
                                     a.VolunteerId == volunteer.VolunteerId);

        if (application == null)
            return NotFound();

        // Can only withdraw if opportunity is in the future
        if (application.Opportunity.Date < DateTime.Today)
        {
            TempData["ErrorMessage"] = "Cannot withdraw from past opportunities.";
            return RedirectToAction(nameof(Index));
        }

        // Can only withdraw if not already withdrawn/removed
        if (application.Status != ApplicationStatus.Applied &&
            application.Status != ApplicationStatus.Waitlist)
        {
            TempData["ErrorMessage"] = "This application cannot be withdrawn.";
            return RedirectToAction(nameof(Index));
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var wasApplied = application.Status == ApplicationStatus.Applied;
            var opportunityId = application.OpportunityId;

            application.Status = ApplicationStatus.Withdrawn;
            //By ANGEL
            application.WithdrawnAt = DateTime.Now;
            await _context.SaveChangesAsync();

            //By ANGEL
            // Notify the organization
            var withdrawnOpportunity = await _context.Opportunities
                .FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);

            var orgOfWithdrawn = await _context.Organizations
                .FirstOrDefaultAsync(o => o.OrganizationId == withdrawnOpportunity.OrganizationId);

            if (orgOfWithdrawn != null && withdrawnOpportunity != null)
            {
                _context.Notifications.Add(new Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    UserId = orgOfWithdrawn.UserId,
                    Type = NotificationType.NewOpportunity,
                    Message = $"{volunteer.Name} has withdrawn from \"{withdrawnOpportunity.Title}\".",
                    RelatedId = application.OpportunityId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            if (wasApplied)
            {
                // Check if we can promote from waitlist
                await PromoteFromWaitlist(opportunityId);
            }

            await transaction.CommitAsync();
            TempData["SuccessMessage"] = "Application withdrawn successfully.";
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Error withdrawing application.";
        }

        return RedirectToAction(nameof(Index));
    }


    // Helper method to promote volunteers from waitlist
    private async Task PromoteFromWaitlist(string opportunityId)
    {
        var opportunity = await _context.Opportunities
            .FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);

        if (opportunity == null) return;

        // Get current counts
        var (currentApplied, waitlistCount) = await GetOpportunityCounts(opportunityId);

        // While there are slots and people on waitlist
        int maxWaitlist = GetMaxWaitlistSize(opportunity.VolunteersNeeded);
        while (HasAvailableSlot(currentApplied, opportunity.VolunteersNeeded) && waitlistCount > 0)
        {
            // Get the earliest waitlist application
            var waitlistApp = await _context.Applications
                .Where(a => a.OpportunityId == opportunityId)
                .Where(a => a.Status == ApplicationStatus.Waitlist)
                .OrderBy(a => a.AppliedAt)
                .FirstOrDefaultAsync();

            if (waitlistApp != null)
            {
                // Promote to applied
                waitlistApp.Status = ApplicationStatus.Applied;
                currentApplied++;
                waitlistCount--;

                //By ANGEL
                // In-app notification (already exists)
                var promotedVolunteer = await _context.Volunteers
                    .Include(v => v.User)
                    .FirstOrDefaultAsync(v => v.VolunteerId == waitlistApp.VolunteerId);

                if (promotedVolunteer != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        UserId = promotedVolunteer.UserId,
                        Type = NotificationType.WaitlistPromoted,
                        Message = $"Good news! You've been promoted from the waitlist for \"{opportunity.Title}\".",
                        RelatedId = waitlistApp.ApplicationId,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });

                    // Email notification
                    if (promotedVolunteer.User?.Email != null)
                    {
                        var emailBody = _emailService.WaitlistPromotedTemplate(
                            promotedVolunteer.Name,
                            opportunity.Title,
                            opportunity.Date.ToString("dd MMMM yyyy")
                        );

                        await _emailService.SendEmailAsync(
                            promotedVolunteer.User.Email,
                            $"You're in! Promoted from waitlist for {opportunity.Title}",
                            emailBody
                        );
                    }
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    // API endpoint to check waitlist position (for AJAX refresh)
    [HttpGet]
    public async Task<IActionResult> GetWaitlistPosition(string applicationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(v => v.UserId == userId);

        var application = await _context.Applications
            .Include(a => a.Opportunity)  // Fix by WW
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId &&
                                     a.VolunteerId == volunteer.VolunteerId);

        if (application == null || application.Status != ApplicationStatus.Waitlist)
            return NotFound();

        var position = await _context.Applications
            .Where(a => a.OpportunityId == application.OpportunityId)
            .Where(a => a.Status == ApplicationStatus.Waitlist)
            .Where(a => a.AppliedAt <= application.AppliedAt)
            .CountAsync();

        var total = await _context.Applications
            .CountAsync(a => a.OpportunityId == application.OpportunityId &&
                            a.Status == ApplicationStatus.Waitlist);

        int maxWaitlist = GetMaxWaitlistSize(application.Opportunity.VolunteersNeeded);
        return Json(new { position, total, maxSize = maxWaitlist });
    }
}

