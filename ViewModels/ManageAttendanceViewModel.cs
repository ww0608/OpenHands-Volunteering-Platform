using Microsoft.AspNetCore.Mvc;
using OpenHandsVolunteerPlatform.Models.Enums;

//By WW
namespace OpenHandsVolunteerPlatform.ViewModels
{
    public class ManageAttendanceViewModel
    {
        public string OpportunityId { get; set; }
        public string OpportunityTitle { get; set; }
        public DateTime EventDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Location { get; set; }

        public List<VolunteerAttendanceViewModel> Volunteers { get; set; } = new();

        // Summary statistics
        public int TotalVolunteers => Volunteers.Count;
        public int PresentCount => Volunteers.Count(v => v.AttendanceStatus == AttendanceStatus.Present);
        public int NoShowCount => Volunteers.Count(v => v.AttendanceStatus == AttendanceStatus.NoShow);
        public int PendingCount => Volunteers.Count(v => v.AttendanceStatus == AttendanceStatus.Pending);
        public double AttendanceRate => TotalVolunteers > 0
            ? Math.Round((PresentCount / (double)TotalVolunteers) * 100, 1)
            : 0;
    }

    public class VolunteerAttendanceViewModel
    {
        public string ApplicationId { get; set; }
        public string VolunteerId { get; set; }
        public string VolunteerName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int Age { get; set; }
        public AttendanceStatus AttendanceStatus { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public TimeSpan? TotalHours
        {
            get
            {
                if (CheckInTime.HasValue && CheckOutTime.HasValue)
                {
                    return CheckOutTime.Value - CheckInTime.Value;
                }
                return null;
            }
        }
    }
}
