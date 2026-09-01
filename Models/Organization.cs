using OpenHandsVolunteerPlatform.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace OpenHandsVolunteerPlatform.Models
{
    public class Organization
    {
        public string OrganizationId { get; set; }

        public string UserId { get; set; }

        public ApplicationUser User { get; set; } = null!;

        // Organization Details
        public string OrganizationName { get; set; }

        public string? Mission { get; set; } = "-";

        public string Phone{ get; set; }

        // Verification
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

        public string LogoPath { get; set; }

        public string VerificationDocumentPath { get; set; }

        public string? RejectionReason { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();

        //By ANGEL
        public ICollection<Follow> Followers { get; set; } = new List<Follow>();
        public ICollection<OrganizationRating> Ratings { get; set; } = new List<OrganizationRating>();

        //ww fix
        // Organization Type (Non-Profit, Charity, Community Organization, etc.)
        public string? OrganizationType { get; set; }
    }
}