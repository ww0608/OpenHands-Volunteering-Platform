using Microsoft.AspNetCore.Http;
using OpenHandsVolunteerPlatform.Models.Enums;
using System.ComponentModel.DataAnnotations;

//By WW
namespace OpenHandsVolunteerPlatform.ViewModels
{
    public class OrgProfileViewModels
    {
        public string OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string? OrganizationType { get; set; } // ww fix
        public string Email { get; set; }
        public string Phone { get; set; }
        public string? Mission { get; set; }
        public string? LogoPath { get; set; }
        public string? VerificationDocumentPath { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }

        // Statistics
        public int TotalOpportunities { get; set; }
        public int TotalVolunteers { get; set; }
        public int UpcomingOpportunities { get; set; }
        public int CompletedOpportunities { get; set; }
    }

    public class OrganizationEditViewModel
    {
        public string OrganizationId { get; set; }

        [Required(ErrorMessage = "Organization name is required")]
        [Display(Name = "ORGANIZATION NAME")]
        public string OrganizationName { get; set; }

        [Display(Name = "ORGANIZATION TYPE")]
        public string? OrganizationType { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "OFFICIAL EMAIL")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "OFFICIAL PHONE NUMBER")]
        public string Phone { get; set; }

        [Display(Name = "MISSION STATEMENT")]
        public string? Mission { get; set; }

        // For logo upload
        [Display(Name = "ORGANIZATION LOGO")]
        public IFormFile? LogoFile { get; set; }
        public string? ExistingLogoPath { get; set; }

        // For verification document upload
        [Display(Name = "ORGANIZATION REGISTRATION CERTIFICATE")]
        public IFormFile? VerificationDocumentFile { get; set; }
        public string? ExistingVerificationDocumentPath { get; set; }

        // Terms acceptance
        [Display(Name = "Privacy Policy")]
        public bool AcceptPrivacyPolicy { get; set; }

        [Display(Name = "Terms & Conditions")]
        public bool AcceptTermsConditions { get; set; }
    }
}