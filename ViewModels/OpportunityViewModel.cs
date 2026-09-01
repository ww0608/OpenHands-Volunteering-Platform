using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;

namespace OpenHandsVolunteerPlatform.ViewModels
{
    public class OpportunityViewModel
    {
        public Opportunity Opportunity { get; set; }

        // Time-based status
        public OpportunityStatus TimeStatus { get; set; }
        public void CalculateTimeStatus()
        {
            var now = DateTime.Now;
            var today = now.Date;
            var currentTime = now.TimeOfDay;

            bool isToday = Opportunity.Date.Date == today;
            bool isFutureDate = Opportunity.Date.Date > today;

            bool isOngoing = isToday &&
                Opportunity.StartTime <= currentTime &&
                Opportunity.EndTime > currentTime;

            bool isUpcoming = isFutureDate ||
                (isToday && Opportunity.StartTime > currentTime);

            TimeStatus = isOngoing
                ? OpportunityStatus.Ongoing
                : isUpcoming
                    ? OpportunityStatus.Upcoming
                    : OpportunityStatus.Past;
        }
        // UI helpers
        public string StatusClass =>
            TimeStatus switch
            {
                OpportunityStatus.Ongoing => "status-ongoing",
                OpportunityStatus.Upcoming => "status-upcoming",
                OpportunityStatus.Past => "status-past",
                _ => ""
            };

        public bool ShowAttendance =>
            TimeStatus == OpportunityStatus.Ongoing;

        public bool ShowManagement =>
            TimeStatus == OpportunityStatus.Ongoing ||
            TimeStatus == OpportunityStatus.Upcoming;

        public bool ShowEdit =>
            TimeStatus == OpportunityStatus.Upcoming;

        public bool ShowDelete =>
            TimeStatus == OpportunityStatus.Upcoming;

        public bool ShowRetrospective =>
            TimeStatus == OpportunityStatus.Past;

        public int AppliedCount { get; set; }
        public int SpotsLeft { get; set; }

        public DateTime Now { get; set; }
    }
}