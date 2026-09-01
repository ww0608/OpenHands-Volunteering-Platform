// Services/IdentityEmailSender.cs
using Microsoft.AspNetCore.Identity.UI.Services;

namespace OpenHandsVolunteerPlatform.Services
{
    public class IdentityEmailSender : IEmailSender
    {
        private readonly EmailService _emailService;

        public IdentityEmailSender(EmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await _emailService.SendEmailAsync(email, subject, htmlMessage);
        }
    }
}