using OpenHandsVolunteerPlatform.Models.Enums;

//By ANGEL
namespace OpenHandsVolunteerPlatform.Models
{
    public class Report
    {
        public string ReportId { get; set; } = Guid.NewGuid().ToString();

        public string ReporterId { get; set; } // UserId of the person reporting
        public ApplicationUser Reporter { get; set; }

        public ReportTargetType TargetType { get; set; }
        public string TargetId { get; set; } // OpportunityId or OrganizationId

        public string Reason { get; set; }
        public string? Details { get; set; }

        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public string? AdminNote { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
