using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Services;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace OpenHandsVolunteerPlatform.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            EmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        [BindProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Email);

            // Always redirect to confirmation page even if user not found
            // This prevents email enumeration attacks
            if (user == null)
                return RedirectToPage("./ForgotPasswordConfirmation");

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Build reset link
            var resetLink = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new
                {
                    area = "Identity",
                    email = Email,
                    code = encodedToken
                },
                protocol: Request.Scheme);

            // Send email
            try
            {
                var emailBody = _emailService.PasswordResetTemplate(
                    HtmlEncoder.Default.Encode(resetLink));

                await _emailService.SendEmailAsync(
                    Email,
                    "Reset Your OpenHands Password",
                    emailBody);
            }
            catch (Exception)
            {
                // Don't expose email errors to the user
            }

            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}