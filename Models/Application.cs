using OpenHandsVolunteerPlatform.Models.Enums;

namespace OpenHandsVolunteerPlatform.Models
{
    public class Application
    {
        public string ApplicationId { get; set; }

        public string OpportunityId { get; set; }
        public Opportunity Opportunity { get; set; }

        public string? VolunteerId { get; set; } //string? is to avoid cascade delete error [delete a volunteer fails if they still have applications]
        public Volunteer Volunteer { get; set; }

        public ApplicationStatus Status { get; set; } 

        public DateTime AppliedAt { get; set; } = DateTime.Now;

        //By ANGEL
        public DateTime? WithdrawnAt { get; set; }

        public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Pending;

        public Certificate? Certificate { get; set; }

        //By WW
        public DateTime? CheckInTime { get; set; } //attendace submitted time
    }
}