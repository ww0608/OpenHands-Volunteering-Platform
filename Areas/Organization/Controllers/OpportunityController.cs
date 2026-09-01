using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using OpenHandsVolunteerPlatform.Services;
using OpenHandsVolunteerPlatform.ViewModels;
using System.Security.Claims;

namespace OpenHandsVolunteerPlatform.Areas.Organization.Controllers
{
    [Area("Organization")]
    public class OpportunityController : Controller
    {
        //By WW
        private readonly ApplicationDbContext _context;
        //By ANGEL
        private readonly CertificateService _certificateService;
        private readonly EmailService _emailService;
        private readonly ILogger<OpportunityController> _logger;

        public OpportunityController(
            ApplicationDbContext context,
            CertificateService certificateService, //By ANGEL
            EmailService emailService,
            ILogger<OpportunityController> logger)
        {
            _context = context;
            _certificateService = certificateService;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: Organization/Opportunity
        public async Task<IActionResult> Index(string status = "Ongoing", string search = "", string dateFilter = "all", string weekendFilter = "all", string location = "")
        {
            // Get current organization ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (currentOrg == null)
            {
                return RedirectToAction("Create", "Profile");
            }

            var orgId = currentOrg.OrganizationId;

            // Get organization approval status for the view
            ViewBag.IsOrganizationApproved = currentOrg.VerificationStatus == VerificationStatus.Approved;

            // Only get opportunities belonging to this organization
            var opportunities = _context.Opportunities
                .Where(o => o.OrganizationId == orgId)
                .Select(o => new Opportunity
                {
                    OpportunityId = o.OpportunityId,
                    Title = o.Title,
                    Description = o.Description,
                    Date = o.Date,
                    StartTime = o.StartTime,
                    EndTime = o.EndTime,
                    Location = o.Location,
                    VolunteersNeeded = o.VolunteersNeeded,
                    ApplicationCloseDate = o.ApplicationCloseDate,

                    Status = o.ApplicationCloseDate < DateTime.Now ? OpportunityOpenStatus.Close : OpportunityOpenStatus.Open
                });

            var opportunitiesList = await opportunities.ToListAsync();

            // Apply filtering based on selected status tab

            var today = DateTime.Now.Date;
            var currentTime = DateTime.Now.TimeOfDay;

            // Convert string to enum safely, default to Ongoing if invalid
            OpportunityStatus selectedStatus = OpportunityStatus.Ongoing;

            // map incoming string to enum correctly
            if (!string.IsNullOrEmpty(status))
            {
                selectedStatus = status.ToLower() switch
                {
                    "upcoming" => OpportunityStatus.Upcoming,
                    "ongoing" => OpportunityStatus.Ongoing,
                    "past" => OpportunityStatus.Past,
                    _ => OpportunityStatus.Ongoing
                };
            }

            // Inline filtering using enum
            opportunitiesList = selectedStatus switch
            {
                OpportunityStatus.Ongoing =>
                    opportunitiesList
                        .Where(o =>
                            o.Date.Date == today &&
                            o.StartTime <= currentTime &&
                            o.EndTime > currentTime)
                        .ToList(),

                OpportunityStatus.Upcoming =>
                    opportunitiesList
                        .Where(o =>
                            o.Date.Date > today ||
                            (o.Date.Date == today && o.StartTime > currentTime))
                        .ToList(),

                OpportunityStatus.Past =>
                    opportunitiesList
                        .Where(o =>
                            o.Date.Date < today ||
                            (o.Date.Date == today && o.EndTime <= currentTime))
                        .ToList(),

                _ =>
                    opportunitiesList
                        .Where(o =>
                            o.Date.Date == today &&
                            o.StartTime <= currentTime &&
                            o.EndTime > currentTime)
                        .ToList()
            };


            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                opportunitiesList = opportunitiesList
                    .Where(o => o.Title.ToLower().Contains(search) ||
                                 o.Description.ToLower().Contains(search) ||
                               o.Location.ToLower().Contains(search))
                    .ToList();
            }

            // Apply date filter
            if (!string.IsNullOrEmpty(dateFilter) && dateFilter != "all")
            {
                switch (dateFilter)
                {
                    case "today":
                        opportunitiesList = opportunitiesList.Where(o => o.Date.Date == today).ToList();
                        break;
                    case "week":
                        var weekStart = today.AddDays(-(int)today.DayOfWeek);
                        opportunitiesList = opportunitiesList.Where(o => o.Date.Date >= weekStart && o.Date.Date <= weekStart.AddDays(6)).ToList();
                        break;
                    case "month":
                        opportunitiesList = opportunitiesList.Where(o => o.Date.Year == today.Year && o.Date.Month == today.Month).ToList();
                        break;
                }
            }

            // Apply weekend/weekday filter
            if (!string.IsNullOrEmpty(weekendFilter) && weekendFilter != "all")
            {
                if (weekendFilter == "weekend")
                {
                    opportunitiesList = opportunitiesList.Where(o => o.Date.DayOfWeek == DayOfWeek.Saturday || o.Date.DayOfWeek == DayOfWeek.Sunday).ToList();
                }
                else if (weekendFilter == "weekday")
                {
                    opportunitiesList = opportunitiesList.Where(o => o.Date.DayOfWeek != DayOfWeek.Saturday && o.Date.DayOfWeek != DayOfWeek.Sunday).ToList();
                }
            }

            //// Apply location filter
            //if (!string.IsNullOrEmpty(location))
            //{
            //    location = location.ToLower();
            //    opportunitiesList = opportunitiesList
            //        .Where(o => o.Location.ToLower().Contains(location))
            //        .ToList();
            //}

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentDateFilter = dateFilter;
            ViewBag.CurrentWeekendFilter = weekendFilter;
            ViewBag.CurrentLocation = location;

            return View(opportunitiesList);
        }        

        // GET: Organization/Opportunity/Details/5
        //By WW
        public async Task<IActionResult> Details(string id)
        {
            var opportunity = _context.Opportunities
                .Include(o => o.Applications)
                .FirstOrDefault(o => o.OpportunityId == id);

            if (id == null || opportunity == null)
            {
                TempData["ErrorMessage"] = "This opportunity no longer exists.";
                return RedirectToAction("Index");
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

            var appliedCount = opportunity.Applications?
                .Count(a => a.Status == ApplicationStatus.Applied) ?? 0;

            var vm = new OpportunityViewModel
            {
                Opportunity = opportunity,
                //TimeStatus = timeStatus,
                AppliedCount = appliedCount,
                SpotsLeft = opportunity.VolunteersNeeded - appliedCount,
                Now = now
            };
            vm.CalculateTimeStatus();
            return View(vm);
        }

        //GET: Organization/Opportunity/Create/5
        [Authorize(Roles = "Organization")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (org == null)
            {
                TempData["ErrorMessage"] = "Organization profile not found. Please complete your organization profile first.";
                return RedirectToAction("Index", "Profile");
            }

            if (org.VerificationStatus != VerificationStatus.Approved)
            {
                TempData["ErrorMessage"] = "Your organization is pending verification. You can only create opportunities after approval.";
                return RedirectToAction("Index");
            }

            return View(new Opportunity
            {
                ApplicationCloseDate = DateTime.Today,
                Date = DateTime.Today.AddDays(1)
            });
        }


        //POST: Organization/Opportunity/Create/5
        [Authorize(Roles = "Organization")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Opportunity newOpportunity, RecurrenceType recurrence = RecurrenceType.None, int recurrenceCount = 0) //By ANGEL
        {
            //get logged in user id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //find organization that belongs to this user
            var org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (org == null)
            {
                ModelState.AddModelError("", "Organization profile not found.");
                return View(newOpportunity);
            }

            if (org.VerificationStatus != VerificationStatus.Approved)
            {
                ModelState.AddModelError("", "Your organization is pending verification.");
                return View(newOpportunity);
            }

            // -------- VALIDATION --------
            newOpportunity.OrganizationId = org.OrganizationId;
            newOpportunity.Status = OpportunityOpenStatus.Open;

            //CLEAR INVALID MODELSTATE
            ModelState.Remove("OrganizationId");
            ModelState.Remove("OpportunityId");
            ModelState.Remove("Organization");

            var eventDateTime = newOpportunity.Date.Date.Add(newOpportunity.StartTime);

            // 1. Event date cannot be in the past
            if (eventDateTime <= DateTime.Now)
            {
                ModelState.AddModelError("Date", "Event cannot be in the past.");
            }
            // 2. Start time must be before end time
            if (newOpportunity.StartTime >= newOpportunity.EndTime)
            {
                ModelState.AddModelError("", "Start time must be earlier than end time.");
            }
            // 3. Prevent extremely short events (minimum 30 minutes)
            if ((newOpportunity.EndTime - newOpportunity.StartTime).TotalMinutes < 30)
            {
                ModelState.AddModelError("", "Event must be at least 30 minutes long.");
            }
            // 4. Application closing date cannot be after event date
            if (newOpportunity.ApplicationCloseDate > newOpportunity.Date)
            {
                ModelState.AddModelError("ApplicationCloseDate", "Application close date must be before the event date.");
            }
            // 5. Application closing date cannot be in the past
            if (newOpportunity.ApplicationCloseDate.Date.AddDays(1).AddSeconds(-1) < DateTime.Now)
            {
                ModelState.AddModelError("ApplicationCloseDate", "Application close date cannot be in the past.");
            }
            // 6. Volunteers needed must be reasonable
            if (newOpportunity.VolunteersNeeded <= 0)
            {
                ModelState.AddModelError("VolunteersNeeded", "At least 1 volunteer is required.");
            }
            // 7. Check recurring
            if (recurrence != RecurrenceType.None)
            {
                if (recurrenceCount <= 0 || recurrenceCount > 10)
                {
                    ModelState.AddModelError("", "Recurrence count must be between 1 and 10.");
                }
            }
            // If validation fails
            if (!ModelState.IsValid)
            {
                return View(newOpportunity);
            }

            var opportunity = new Opportunity()
            {
                OpportunityId = Guid.NewGuid().ToString(),

                OrganizationId = org.OrganizationId,

                Title = newOpportunity.Title,
                Description = newOpportunity.Description,
                Date = newOpportunity.Date,
                StartTime = newOpportunity.StartTime,
                EndTime = newOpportunity.EndTime,
                Location = newOpportunity.Location,
                VolunteersNeeded = newOpportunity.VolunteersNeeded,
                ApplicationCloseDate = newOpportunity.ApplicationCloseDate.Date.AddDays(1).AddSeconds(-1),

                Status = OpportunityOpenStatus.Open,
                //By WW
                IsEmergency = newOpportunity.IsEmergency,
                MinimumCreditLevel = newOpportunity.MinimumCreditLevel,
                AutoCloseWhenFull = newOpportunity.AutoCloseWhenFull
            };

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync(); //save opportunity

            //By ANGEL
            // Create recurring opportunities if requested
            if (recurrence != RecurrenceType.None) {
                for (int i = 1; i <= recurrenceCount; i++)
                {
                    var nextDate = recurrence == RecurrenceType.Weekly
                        ? opportunity.Date.AddDays(7 * i)
                        : opportunity.Date.AddMonths(i);

                    var closeDate = nextDate.AddDays(-1);

                    var recurring = new Opportunity
                    {
                        OpportunityId = Guid.NewGuid().ToString(),
                        OrganizationId = org.OrganizationId,
                        Title = opportunity.Title,
                        Description = opportunity.Description,
                        Date = nextDate,
                        StartTime = opportunity.StartTime,
                        EndTime = opportunity.EndTime,
                        Location = opportunity.Location,
                        VolunteersNeeded = opportunity.VolunteersNeeded,
                        ApplicationCloseDate = closeDate,
                        Status = OpportunityOpenStatus.Open
                    };

                    _context.Opportunities.Add(recurring);
                }
                await _context.SaveChangesAsync();  //save opportunity
            }

            // ww fix
            // Determine which tab to redirect to based on event date and time
            string redirectStatus = (OpportunityStatus.Upcoming).ToString();
            var now = DateTime.Now;
            var today = now.Date;
            var currentTime = now.TimeOfDay;

            bool isToday = opportunity.Date.Date == today;
            bool isFutureDate = opportunity.Date.Date > today;

            bool isOngoing = isToday && opportunity.StartTime <= currentTime && opportunity.EndTime > currentTime;

            bool isFuture = isFutureDate || (isToday && opportunity.StartTime > currentTime);

            if (isOngoing) {
                redirectStatus = (OpportunityStatus.Ongoing).ToString();  // Still ongoing
            } else if (isFuture) {
                redirectStatus = (OpportunityStatus.Upcoming).ToString(); // Future event
            } else {
                redirectStatus = (OpportunityStatus.Past).ToString(); // Past event
            }

            //Temp data message
            if (recurrence != RecurrenceType.None && recurrenceCount > 0) {
                TempData["SuccessMessage"] =
                    $"Created {recurrenceCount + 1} opportunities ({recurrence} recurrence).";
            } else {
                TempData["SuccessMessage"] = "Opportunity created successfully!";
            }

            // Notify followers of this organization
            var followers = _context.Follows
                .Where(f => f.OrganizationId == org.OrganizationId)
                .Include(f => f.Volunteer)
                    .ThenInclude(v => v.User)
                .ToList();

            foreach (var follow in followers)
            {
                // In-app notification (already exists)
                _context.Notifications.Add(new Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    UserId = follow.Volunteer.UserId,
                    Type = NotificationType.NewOpportunity,
                    Message = $"New opportunity posted: \"{opportunity.Title}\"",
                    RelatedId = opportunity.OpportunityId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync(); //save notification

            var emailTasks = followers
                .Where(f => f.Volunteer.User?.Email != null)
                .Select(f =>
                {
                    var emailBody = _emailService.NewOpportunityTemplate(
                        org.OrganizationName,
                        opportunity.Title,
                        opportunity.Date.ToString("dd MMMM yyyy"),
                        $"https://example.com/Volunteer/Opportunities/Details/{opportunity.OpportunityId}"
                    );

                    return _emailService.SendEmailAsync(
                        f.Volunteer.User.Email,
                        $"New Opportunity: {opportunity.Title}",
                        emailBody
                    );
                });

            await Task.WhenAll(emailTasks).ContinueWith(t => { });

            return RedirectToAction("Index", new { status = redirectStatus });
        }


        // GET: Organization/Opportunity/Edit/5
        [Authorize(Roles = "Organization")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id, string status = "Ongoing")
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index");

            //By WW
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.UserId == userId);

            if (org == null || org.VerificationStatus != VerificationStatus.Approved)
            {
                TempData["ErrorMessage"] = "Your organization is pending verification. You cannot edit opportunities until approved.";
                return RedirectToAction("Index");
            }

            var opportunity = await _context.Opportunities
                .Include(o => o.Applications) // Include related applications
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null)
                return RedirectToAction("Index");

            // Optional: ensure current org owns this opportunity
            var orgId = _context.Organizations.FirstOrDefault(o => o.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))?.OrganizationId;
            if (orgId != opportunity.OrganizationId)
                return Forbid();

            ViewBag.ReturnStatus = status;  // Store the return status
            return View(opportunity);
        }

        // POST: Organization/Opportunity/Edit/5
        [Authorize(Roles = "Organization")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Opportunity editModel, string action, string status = "Ongoing" )
        {
            if (id != editModel.OpportunityId)
                return NotFound();

            var opportunity = await _context.Opportunities
                .Include(o => o.Applications)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null)
            {
                TempData["ErrorMessage"] = "Opportunity not found.";
                return RedirectToAction("Index");
            }

            // Ensure only owner organization can edit
            var orgId = _context.Organizations.FirstOrDefault(o => o.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))?.OrganizationId;
            if (orgId != opportunity.OrganizationId)
                return Forbid();

            // -------- VALIDATION --------
           //CLEAR INVALID MODELSTATE
            ModelState.Remove("OrganizationId");
            ModelState.Remove("OpportunityId");
            ModelState.Remove("Organization");

            // 1. Event date cannot be in the past
            var eventDateTime = editModel.Date.Date.Add(editModel.StartTime);

            if (eventDateTime <= DateTime.Now)
            {
                ModelState.AddModelError("Date", "Event date cannot be in the past.");
            }
            // 2. Start time must be before end time
            if (editModel.StartTime >= editModel.EndTime)
            {
                ModelState.AddModelError("", "Start time must be earlier than end time.");
            }
            // 3. Prevent extremely short events (minimum 30 minutes)
            if ((editModel.EndTime - editModel.StartTime).TotalMinutes < 30)
            {
                ModelState.AddModelError("", "Event must be at least 30 minutes long.");
            }
            // 4. Application closing date cannot be after event date
            if (editModel.ApplicationCloseDate > editModel.Date)
            {
                ModelState.AddModelError("ApplicationCloseDate", "Application close date must be before the event date.");
            }
            // 5. Application closing date cannot be in the past
            if (editModel.ApplicationCloseDate.Date.AddDays(1).AddSeconds(-1) < DateTime.Now)
            {
                ModelState.AddModelError("ApplicationCloseDate", "Application close date cannot be in the past.");
            }
            // 6. Volunteers needed must be reasonable
            if (editModel.VolunteersNeeded <= 0)
            {
                ModelState.AddModelError("VolunteersNeeded", "At least 1 volunteer is required.");
            }

            // If validation fails
            if (!ModelState.IsValid)
            {
                return View(opportunity);
            }

            // UPDATE opportunity
            try
            {
                opportunity.Title = editModel.Title;
                opportunity.Description = editModel.Description;
                opportunity.Date = editModel.Date;
                opportunity.StartTime = editModel.StartTime;
                opportunity.EndTime = editModel.EndTime;
                opportunity.Location = editModel.Location;
                opportunity.VolunteersNeeded = editModel.VolunteersNeeded;
                opportunity.ApplicationCloseDate = editModel.ApplicationCloseDate.Date.AddDays(1).AddSeconds(-1);
                opportunity.Status = editModel.Status;
                //By WW
                opportunity.IsEmergency = editModel.IsEmergency;
                opportunity.MinimumCreditLevel = editModel.MinimumCreditLevel;
                opportunity.AutoCloseWhenFull = editModel.AutoCloseWhenFull;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Opportunity updated successfully.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Opportunity could not be updated.";
            }

            return RedirectToAction("Details", new { id = opportunity.OpportunityId, status = status });
        }


        // GET: Organization/Opportunity/Delete/5
        [Authorize(Roles = "Organization")]
        public async Task<IActionResult> Delete(string id, string status = "Ongoing")
        {
            if (id == null)
            {
                return NotFound();
            }

            var opportunity = await _context.Opportunities
                .Include(o => o.Organization)
                .FirstOrDefaultAsync(m => m.OpportunityId == id);
            if (opportunity == null)
            {
                return NotFound();
            }

            ViewBag.ReturnStatus = status;
            return View(opportunity);
        }

        // POST: Organization/Opportunity/Delete/5
        [Authorize(Roles = "Organization")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, string status = "Ongoing")
        {
            var opportunity = await _context.Opportunities.FindAsync(id);
            if (opportunity != null)
            {
                string opportunityTitle = opportunity.Title;  // Store title for message
                _context.Opportunities.Remove(opportunity);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Opportunity \"{opportunityTitle}\" has been deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Opportunity not found.";
            }

            return RedirectToAction("Index", new { status = status });
        }


        // GET: Organization/Opportunity/IssueCertificates/5
        [Authorize(Roles = "Organization")]
        public async Task<IActionResult> IssueCertificates(string id)
        {
            var opportunity = await _context.Opportunities
                .Include(o => o.Applications)
                    .ThenInclude(a => a.Volunteer)
                .Include(o => o.Organization)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null) return NotFound();

            var attendees = opportunity.Applications
                .Where(a => a.AttendanceStatus == AttendanceStatus.Present)
                .Select(a => a.Volunteer)
                .Where(v => v != null)
                .ToList();

            var existingCertificates = await _context.Certificates
                .Where(c => c.OpportunityId == id)
                .Select(c => c.VolunteerId)
                .ToListAsync();

            // Fix: use full type name to avoid namespace conflict
            var volunteersWithoutCertificates = new List<OpenHandsVolunteerPlatform.Models.Volunteer>();
            foreach (var v in attendees)
            {
                if (!existingCertificates.Contains(v.VolunteerId))
                    volunteersWithoutCertificates.Add(v);
            }

            if (!volunteersWithoutCertificates.Any())
            {
                TempData["ErrorMessage"] = "All volunteers already have certificates for this event.";
                return RedirectToAction("Details", new { id });
            }

            ViewBag.Opportunity = opportunity;
            return View(volunteersWithoutCertificates);
        }

        // POST: Organization/Opportunity/GenerateCertificate
        [Authorize(Roles = "Organization")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateCertificate(string opportunityId, string volunteerId)
        {
            var opportunity = await _context.Opportunities
                .Include(o => o.Organization)
                .FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);

            var volunteer = await _context.Volunteers
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.VolunteerId == volunteerId);

            if (opportunity == null || volunteer == null) return NotFound();

            var html = await _certificateService.GenerateCertificateHtml(
                volunteer, opportunity, opportunity.Organization);

            var fileName = $"Certificate_{volunteer.Name.Replace(" ", "_")}_{opportunity.Title.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.html";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "certificates", fileName);

            var directory = Path.GetDirectoryName(filePath);
            if (directory != null && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await System.IO.File.WriteAllTextAsync(filePath, html);

            await _certificateService.SaveCertificateRecord(
                volunteer.VolunteerId,
                opportunity.OrganizationId,
                opportunity.OpportunityId,
                $"/certificates/{fileName}");

            // Send certificate email to the volunteer
            if (volunteer.User?.Email != null)
            {
                try
                {
                    var certUrl = $"{Request.Scheme}://{Request.Host}/certificates/{fileName}";

                    var emailBody = $@"
                    <div style='font-family:sans-serif; max-width:600px; margin:0 auto;'>
                        <div style='background:#8b92d1; padding:2rem; border-radius:16px 16px 0 0; text-align:center;'>
                            <h1 style='color:white; margin:0;'>OpenHands</h1>
                        </div>
                        <div style='background:white; padding:2rem; border:1px solid #edf2f9;'>
                            <h2 style='color:#f0a500;'>🏆 Certificate of Participation</h2>
                            <p style='color:#4b5563;'>Hi <strong>{volunteer.Name}</strong>,</p>
                            <p style='color:#4b5563;'>
                                Congratulations! Your certificate of participation for
                                <strong>{opportunity.Title}</strong> on
                                <strong>{opportunity.Date:dd MMMM yyyy}</strong> has been issued.
                            </p>
                            <a href='{certUrl}'
                               style='display:inline-block; background:#f0a500; color:white;
                                      padding:0.75rem 2rem; border-radius:12px; text-decoration:none;
                                      font-weight:600; margin-top:1rem;'>
                                View Certificate
                            </a>
                            <p style='color:#6b7280; font-size:0.85rem; margin-top:1.5rem;'>
                                You can also find your certificate in the My Applications section
                                of your OpenHands account.
                            </p>
                        </div>
                        <div style='background:#f5f7ff; padding:1rem; border-radius:0 0 16px 16px; text-align:center;'>
                            <p style='color:#8e94a7; font-size:0.85rem; margin:0;'>OpenHands Volunteer Platform</p>
                        </div>
                    </div>";

                    await _emailService.SendEmailAsync(
                        volunteer.User.Email,
                        $"Your Certificate for {opportunity.Title} is Ready!",
                        emailBody);

                    // Also create an in-app notification
                    _context.Notifications.Add(new Notification
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        UserId = volunteer.UserId,
                        Type = NotificationType.CertificateIssued,
                        Message = $"Your certificate for \"{opportunity.Title}\" has been issued. Click to view.",
                        RelatedId = opportunity.OpportunityId,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send certificate email to {Email}", volunteer.User.Email);
                    // Don't throw — email failure should not break the certificate issuance
                }
            }

            TempData["SuccessMessage"] = $"Certificate issued to {volunteer.Name}.";
            return RedirectToAction("IssueCertificates", new { id = opportunityId });
        }

    }
}
