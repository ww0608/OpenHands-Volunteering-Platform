using OpenHandsVolunteerPlatform.Models.Enums;

//By ANGEL
namespace OpenHandsVolunteerPlatform.ViewModels
{
    public class UserApplicationViewModel
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string OpportunityTitle { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public ApplicationStatus Status { get; set; }
        public AttendanceStatus AttendanceStatus { get; set; }
        public DateTime AppliedAt { get; set; }
    }
}