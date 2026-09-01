using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;

namespace OpenHandsVolunteerPlatform.Services
{
    public class OpportunityReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OpportunityReminderService> _logger;

        // Run once every 24 hours
        private readonly TimeSpan _interval = TimeSpan.FromHours(24);

        public OpportunityReminderService(
            IServiceProvider serviceProvider,
            ILogger<OpportunityReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Opportunity Reminder Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendReminders();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Opportunity Reminder Service.");
                }

                // Wait 24 hours before running again
                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task SendReminders()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            // Target date = 3 days from today
            var targetDate = DateTime.Today.AddDays(3);

            // Find all applications for opportunities happening in 3 days
            var applications = await context.Applications
                .Include(a => a.Opportunity)
                    .ThenInclude(o => o.Organization)
                .Include(a => a.Volunteer)
                    .ThenInclude(v => v.User)
                .Where(a => a.Status == ApplicationStatus.Applied
                         && a.Opportunity.Date.Date == targetDate)
                .ToListAsync();

            _logger.LogInformation(
                "Sending reminders for {Count} applications on {Date}",
                applications.Count, targetDate.ToString("dd MMM yyyy"));

            foreach (var app in applications)
            {
                var email = app.Volunteer?.User?.Email;
                if (string.IsNullOrEmpty(email)) continue;

                try
                {
                    var emailBody = BuildReminderEmail(app);

                    await emailService.SendEmailAsync(
                        email,
                        $"Reminder: {app.Opportunity.Title} is in 3 days!",
                        emailBody
                    );

                    // Also create in-app notification
                    context.Notifications.Add(new Notification
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        UserId = app.Volunteer.UserId,
                        Type = NotificationType.Reminder,
                        Message = $"Reminder: \"{app.Opportunity.Title}\" is happening in 3 days on {app.Opportunity.Date:dd MMM yyyy}.",
                        RelatedId = app.OpportunityId,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });

                    _logger.LogInformation(
                        "Reminder sent to {Email} for {Opportunity}",
                        email, app.Opportunity.Title);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send reminder to {Email}", email);
                }
            }

            await context.SaveChangesAsync();
        }

        private string BuildReminderEmail(Application app)
        {
            var opportunity = app.Opportunity;
            var volunteer = app.Volunteer;
            var org = opportunity.Organization;

            return $@"
                <div style='font-family:sans-serif; max-width:600px; margin:0 auto;'>
                    <div style='background:#8b92d1; padding:2rem;
                                border-radius:16px 16px 0 0; text-align:center;'>
                        <h1 style='color:white; margin:0;'>OpenHands</h1>
                    </div>
                    <div style='background:white; padding:2rem; border:1px solid #edf2f9;'>

                        <h2 style='color:#3f4079; margin-bottom:0.5rem;'>
                            ⏰ Reminder: Your opportunity is in 3 days!
                        </h2>

                        <p style='color:#4b5563;'>
                            Hi <strong>{volunteer.Name}</strong>, just a friendly reminder
                            that you are registered for the following opportunity:
                        </p>

                        <div style='background:#f5f7ff; border-radius:12px;
                                    padding:1.5rem; margin:1.5rem 0;'>
                            <h3 style='color:#3f4079; margin:0 0 1rem;'>
                                {opportunity.Title}
                            </h3>
                            <table style='width:100%; border-collapse:collapse;'>
                                <tr>
                                    <td style='padding:0.4rem 0; color:#8e94a7;
                                               font-size:0.85rem; width:120px;'>
                                        📅 Date
                                    </td>
                                    <td style='padding:0.4rem 0; color:#2d3e4f;
                                               font-weight:600;'>
                                        {opportunity.Date:dd MMMM yyyy}
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding:0.4rem 0; color:#8e94a7;
                                               font-size:0.85rem;'>
                                        🕐 Time
                                    </td>
                                    <td style='padding:0.4rem 0; color:#2d3e4f;
                                               font-weight:600;'>
                                        {opportunity.StartTime:hh\:mm} - {opportunity.EndTime:hh\:mm}
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding:0.4rem 0; color:#8e94a7;
                                               font-size:0.85rem;'>
                                        📍 Location
                                    </td>
                                    <td style='padding:0.4rem 0; color:#2d3e4f;
                                               font-weight:600;'>
                                        {opportunity.Location}
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding:0.4rem 0; color:#8e94a7;
                                               font-size:0.85rem;'>
                                        🏢 Organization
                                    </td>
                                    <td style='padding:0.4rem 0; color:#2d3e4f;
                                               font-weight:600;'>
                                        {org?.OrganizationName ?? "-"}
                                    </td>
                                </tr>
                            </table>
                        </div>

                        <div style='background:#fef3e7; border:1px solid #fde68a;
                                    border-radius:12px; padding:1rem; margin-bottom:1.5rem;'>
                            <p style='color:#b85e00; margin:0; font-size:0.9rem;'>
                                <strong>⚠️ Please note:</strong> If you can no longer attend,
                                please withdraw your application as soon as possible so
                                waitlisted volunteers can take your spot.
                            </p>
                        </div>

                        <p style='color:#4b5563; font-size:0.9rem;'>
                            Thank you for volunteering with OpenHands. We look forward
                            to seeing you there!
                        </p>

                    </div>
                    <div style='background:#f5f7ff; padding:1rem;
                                border-radius:0 0 16px 16px; text-align:center;'>
                        <p style='color:#8e94a7; font-size:0.85rem; margin:0;'>
                            OpenHands Volunteer Platform
                        </p>
                    </div>
                </div>";
        }
    }
}