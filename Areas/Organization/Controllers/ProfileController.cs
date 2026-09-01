using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using OpenHandsVolunteerPlatform.ViewModels;
using System.Security.Claims;
using VerificationStatusEnum = OpenHandsVolunteerPlatform.Models.Enums.VerificationStatus;

namespace OpenHandsVolunteerPlatform.Areas.Organization.Controllers
{
    [Area("Organization")]
    [Authorize(Roles = "Organization")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ProfileController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Organization/Profile
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var organization = await _context.Organizations
                .Include(o => o.User)
                .Include(o => o.Opportunities)
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organization == null)
            {
                return RedirectToAction("Create", "Profile", new { area = "" });
            }

            // Calculate statistics
            var now = DateTime.Now;
            var viewModel = new OrgProfileViewModels
            {
                OrganizationId = organization.OrganizationId,
                OrganizationName = organization.OrganizationName,
                OrganizationType = organization.OrganizationType, // ww fix
                Email = organization.User?.Email ?? "",
                Phone = organization.Phone ?? "",
                Mission = organization.Mission,
                LogoPath = organization.LogoPath,
                VerificationDocumentPath = organization.VerificationDocumentPath,
                VerificationStatus = organization.VerificationStatus,
                VerifiedAt = organization.VerifiedAt,
                RejectionReason = organization.RejectionReason,
                CreatedAt = organization.CreatedAt,

                TotalOpportunities = organization.Opportunities?.Count ?? 0,
                UpcomingOpportunities = organization.Opportunities?
                    .Count(o => o.Date > now) ?? 0,
                CompletedOpportunities = organization.Opportunities?
                    .Count(o => o.Date < now) ?? 0,
                TotalVolunteers = await GetTotalVolunteers(organization.OrganizationId)
            };

            return View(viewModel);
        }

        // GET: Organization/Profile/Edit
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var organization = await _context.Organizations
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organization == null)
            {
                return NotFound();
            }

            var viewModel = new OrganizationEditViewModel
            {
                OrganizationId = organization.OrganizationId,
                OrganizationName = organization.OrganizationName,
                OrganizationType = organization.OrganizationType, // ww fix
                Email = organization.User?.Email ?? "",
                Phone = organization.Phone ?? "",
                Mission = organization.Mission,
                ExistingLogoPath = organization.LogoPath,
                ExistingVerificationDocumentPath = organization.VerificationDocumentPath
            };

            return View(viewModel);
        }

        // POST: Organization/Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OrganizationEditViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            // Check terms acceptance
            if (!viewModel.AcceptPrivacyPolicy || !viewModel.AcceptTermsConditions)
            {
                ModelState.AddModelError("", "You must accept the Privacy Policy and Terms & Conditions");
                return View(viewModel);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var organization = await _context.Organizations
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (organization == null)
            {
                return NotFound();
            }

            // Update basic info
            organization.OrganizationName = viewModel.OrganizationName;
            organization.Phone = viewModel.Phone;
            organization.Mission = viewModel.Mission;
            //ww fix
            organization.OrganizationType = viewModel.OrganizationType;


            // Update email in AspNetUsers
            if (organization.User != null && organization.User.Email != viewModel.Email)
            {
                organization.User.Email = viewModel.Email;
                organization.User.UserName = viewModel.Email; // Update username too if it's email
                organization.User.NormalizedEmail = viewModel.Email.ToUpper();
                organization.User.NormalizedUserName = viewModel.Email.ToUpper();
            }

            // Handle logo upload
            if (viewModel.LogoFile != null && viewModel.LogoFile.Length > 0)
            {
                try
                {
                    // Delete old logo if exists
                    if (!string.IsNullOrEmpty(organization.LogoPath))
                    {
                        var oldLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, organization.LogoPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldLogoPath))
                        {
                            System.IO.File.Delete(oldLogoPath);
                        }
                    }

                    // Save new logo
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "logos");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.LogoFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.LogoFile.CopyToAsync(fileStream);
                    }

                    organization.LogoPath = $"/uploads/logos/{uniqueFileName}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading logo");
                    ModelState.AddModelError("LogoFile", "Error uploading logo. Please try again.");
                    return View(viewModel);
                }
            }

            // Handle verification document upload
            if (viewModel.VerificationDocumentFile != null && viewModel.VerificationDocumentFile.Length > 0)
            {
                try
                {
                    // Delete old document if exists
                    if (!string.IsNullOrEmpty(organization.VerificationDocumentPath))
                    {
                        var oldDocPath = Path.Combine(_webHostEnvironment.WebRootPath, organization.VerificationDocumentPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldDocPath))
                        {
                            System.IO.File.Delete(oldDocPath);
                        }
                    }

                    // Save new document
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "certificates");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.VerificationDocumentFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.VerificationDocumentFile.CopyToAsync(fileStream);
                    }

                    organization.VerificationDocumentPath = $"/uploads/certificates/{uniqueFileName}";

                    // Reset verification status if document is updated
                    if (organization.VerificationStatus == VerificationStatusEnum.Rejected)
                    {
                        organization.VerificationStatus = VerificationStatusEnum.Pending;
                        organization.RejectionReason = null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading verification document");
                    ModelState.AddModelError("VerificationDocumentFile", "Error uploading document. Please try again.");
                    return View(viewModel);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Profile updated successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile");
                TempData["ErrorMessage"] = "Error updating profile. Please try again.";
                return View(viewModel);
            }

            return RedirectToAction(nameof(Index));
        }
        // GET: Organization/Profile/VerificationStatus
        public IActionResult VerificationStatus()
        {
            return PartialView("_VerificationStatus");
        }

        private async Task<int> GetTotalVolunteers(string organizationId)
        {
            return await _context.Applications
                .Include(a => a.Opportunity)
                .Where(a => a.Opportunity.OrganizationId == organizationId)
                .Where(a => a.Status == ApplicationStatus.Applied)
                .Select(a => a.VolunteerId)
                .Distinct()
                .CountAsync();
        }
    }
}