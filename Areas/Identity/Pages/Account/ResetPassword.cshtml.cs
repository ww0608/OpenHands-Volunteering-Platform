using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using OpenHandsVolunteerPlatform.Models;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OpenHandsVolunteerPlatform.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [BindProperty]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        [BindProperty]
        public string Code { get; set; }

        public IActionResult OnGet(string email = null, string code = null)
        {
            if (code == null || email == null)
            {
                return BadRequest("A code and email must be supplied for password reset.");
            }

            Email = email;
            Code = code;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            // Decode the token
            var decodedToken = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(Code));

            //var result = await _userManager.ResetPasswordAsync(
            //    user, decodedToken, Password);

            //if (result.Succeeded)
            //    return RedirectToPage("./ResetPasswordConfirmation");

            // Check if new password is same as current password
            var passwordCheck = await _userManager.CheckPasswordAsync(user, Password);
            if (passwordCheck)
            {
                ModelState.AddModelError(string.Empty,
                    "New password cannot be the same as your current password.");
                return Page();
            }

            var result = await _userManager.ResetPasswordAsync(
                user, decodedToken, Password);

            if (result.Succeeded)
                return RedirectToPage("./ResetPasswordConfirmation");

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }
    }
}