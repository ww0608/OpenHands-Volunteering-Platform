using System.Net;
using System.Net.Mail;

//By ANGEL
namespace OpenHandsVolunteerPlatform.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var settings = _config.GetSection("EmailSettings");

            var fromEmail = settings["FromEmail"];
            var fromName = settings["FromName"];
            var smtpHost = settings["SmtpHost"];
            var smtpPort = int.Parse(settings["SmtpPort"] ?? "587");
            var smtpUser = settings["SmtpUser"];
            var smtpPassword = settings["SmtpPassword"];

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail!, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }

        // Email templates
        public string NewOpportunityTemplate(string orgName, string opportunityTitle, string opportunityDate, string link)
        {
            return $@"
                <div style='font-family:sans-serif; max-width:600px; margin:0 auto;'>
                    <div style='background:#8b92d1; padding:2rem; border-radius:16px 16px 0 0; text-align:center;'>
                        <h1 style='color:white; margin:0;'>OpenHands</h1>
                    </div>
                    <div style='background:white; padding:2rem; border:1px solid #edf2f9;'>
                        <h2 style='color:#3f4079;'>New Opportunity Posted!</h2>
                        <p style='color:#4b5563;'>
                            <strong>{orgName}</strong> just posted a new volunteer opportunity:
                        </p>
                        <div style='background:#f5f7ff; border-radius:12px; padding:1.5rem; margin:1rem 0;'>
                            <h3 style='color:#3f4079; margin:0 0 0.5rem;'>{opportunityTitle}</h3>
                            <p style='color:#8e94a7; margin:0;'>Date: {opportunityDate}</p>
                        </div>
                        <a href='{link}' style='display:inline-block; background:#8b92d1; color:white;
                                                padding:0.75rem 1.5rem; border-radius:12px; text-decoration:none;
                                                font-weight:600; margin-top:1rem;'>
                            View Opportunity
                        </a>
                    </div>
                    <div style='background:#f5f7ff; padding:1rem; border-radius:0 0 16px 16px; text-align:center;'>
                        <p style='color:#8e94a7; font-size:0.85rem; margin:0;'>
                            You received this because you follow {orgName} on OpenHands.
                        </p>
                    </div>
                </div>";
        }

        public string ApplicationStatusTemplate(string volunteerName, string opportunityTitle, bool approved)
        {
            var statusColor = approved ? "#2d7a2d" : "#e74c3c";
            var statusText = approved ? "Approved" : "Rejected";
            var message = approved
                ? "Congratulations! Your application has been approved. We look forward to seeing you at the event."
                : "Unfortunately your application was not accepted this time. Keep an eye out for other opportunities!";

            return $@"
                <div style='font-family:sans-serif; max-width:600px; margin:0 auto;'>
                    <div style='background:#8b92d1; padding:2rem; border-radius:16px 16px 0 0; text-align:center;'>
                        <h1 style='color:white; margin:0;'>OpenHands</h1>
                    </div>
                    <div style='background:white; padding:2rem; border:1px solid #edf2f9;'>
                        <h2 style='color:{statusColor};'>Application {statusText}</h2>
                        <p style='color:#4b5563;'>Hi <strong>{volunteerName}</strong>,</p>
                        <p style='color:#4b5563;'>
                            Your application for <strong>{opportunityTitle}</strong> has been
                            <strong style='color:{statusColor};'>{statusText.ToLower()}</strong>.
                        </p>
                        <p style='color:#4b5563;'>{message}</p>
                    </div>
                    <div style='background:#f5f7ff; padding:1rem; border-radius:0 0 16px 16px; text-align:center;'>
                        <p style='color:#8e94a7; font-size:0.85rem; margin:0;'>OpenHands Volunteer Platform</p>
                    </div>
                </div>";
        }

        public string WaitlistPromotedTemplate(string volunteerName, string opportunityTitle, string opportunityDate)
        {
            return $@"
                <div style='font-family:sans-serif; max-width:600px; margin:0 auto;'>
                    <div style='background:#8b92d1; padding:2rem; border-radius:16px 16px 0 0; text-align:center;'>
                        <h1 style='color:white; margin:0;'>OpenHands</h1>
                    </div>
                    <div style='background:white; padding:2rem; border:1px solid #edf2f9;'>
                        <h2 style='color:#2d7a2d;'>You're In! 🎉</h2>
                        <p style='color:#4b5563;'>Hi <strong>{volunteerName}</strong>,</p>
                        <p style='color:#4b5563;'>
                            Great news! A spot opened up and you've been promoted from the waitlist for:
                        </p>
                        <div style='background:#f5f7ff; border-radius:12px; padding:1.5rem; margin:1rem 0;'>
                            <h3 style='color:#3f4079; margin:0 0 0.5rem;'>{opportunityTitle}</h3>
                            <p style='color:#8e94a7; margin:0;'>Date: {opportunityDate}</p>
                        </div>
                        <p style='color:#4b5563;'>You are now confirmed as a volunteer for this event.</p>
                    </div>
                    <div style='background:#f5f7ff; padding:1rem; border-radius:0 0 16px 16px; text-align:center;'>
                        <p style='color:#8e94a7; font-size:0.85rem; margin:0;'>OpenHands Volunteer Platform</p>
                    </div>
                </div>";
        }

        public string OrgVerificationTemplate(string orgName, bool approved, string? rejectionReason)
        {
            var statusColor = approved ? "#2d7a2d" : "#e74c3c";
            var statusText = approved ? "Approved" : "Rejected";
            var bodyMessage = approved
                ? "Congratulations! Your organization has been <strong>verified and approved</strong>. You can now log in and start posting volunteer opportunities."
                : $"Unfortunately, your organization application was <strong>not approved</strong> at this time.<br/><br/>" +
                  (string.IsNullOrEmpty(rejectionReason)
                      ? "Please contact our team for further details."
                      : $"<strong>Reason:</strong> {rejectionReason}");

            return $@"
        <div style='font-family:sans-serif; max-width:600px; margin:0 auto;'>
            <div style='background:#8b92d1; padding:2rem; border-radius:16px 16px 0 0; text-align:center;'>
                <h1 style='color:white; margin:0;'>OpenHands</h1>
            </div>
            <div style='background:white; padding:2rem; border:1px solid #edf2f9;'>
                <h2 style='color:{statusColor};'>Organization {statusText}</h2>
                <p style='color:#4b5563;'>Hi <strong>{orgName}</strong>,</p>
                <p style='color:#4b5563;'>{bodyMessage}</p>
                {(approved ? @"<a href='https://yourdomain.com/Identity/Account/Login'
                    style='display:inline-block; background:#8b92d1; color:white;
                           padding:0.75rem 1.5rem; border-radius:12px;
                           text-decoration:none; font-weight:600; margin-top:1rem;'>
                    Log In Now
                </a>" : "")}
            </div>
            <div style='background:#f5f7ff; padding:1rem; border-radius:0 0 16px 16px; text-align:center;'>
                <p style='color:#8e94a7; font-size:0.85rem; margin:0;'>OpenHands Volunteer Platform</p>
            </div>
        </div>";
        }

        public string PasswordResetTemplate(string resetLink)
        {
            return $@"
        <div style='font-family:sans-serif; max-width:600px; margin:0 auto;'>
            <div style='background:#8b92d1; padding:2rem; border-radius:16px 16px 0 0; text-align:center;'>
                <h1 style='color:white; margin:0;'>OpenHands</h1>
            </div>
            <div style='background:white; padding:2rem; border:1px solid #edf2f9;'>
                <h2 style='color:#3f4079;'>Reset Your Password</h2>
                <p style='color:#4b5563;'>
                    We received a request to reset the password for your OpenHands account.
                    Click the button below to choose a new password.
                </p>
                <a href='{resetLink}'
                   style='display:inline-block; background:#8b92d1; color:white;
                          padding:0.75rem 2rem; border-radius:12px; text-decoration:none;
                          font-weight:600; margin:1rem 0;'>
                    Reset Password
                </a>
                <p style='color:#6b7280; font-size:0.85rem; margin-top:1.5rem;'>
                    If you did not request a password reset, you can safely ignore this email.
                    This link will expire in 24 hours.
                </p>
                <p style='color:#6b7280; font-size:0.8rem; word-break:break-all;'>
                    If the button above doesn't work, copy and paste this link into your browser:<br/>
                    <a href='{resetLink}' style='color:#8b92d1;'>{resetLink}</a>
                </p>
            </div>
            <div style='background:#f5f7ff; padding:1rem; border-radius:0 0 16px 16px; text-align:center;'>
                <p style='color:#8e94a7; font-size:0.85rem; margin:0;'>OpenHands Volunteer Platform</p>
            </div>
        </div>";
        }
    }
}