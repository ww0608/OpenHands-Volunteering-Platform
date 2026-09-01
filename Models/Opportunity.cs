using OpenHandsVolunteerPlatform.Models.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace OpenHandsVolunteerPlatform.Models
{
    public class Opportunity
    {
        public string OpportunityId { get; set; }

        //org details
        public string OrganizationId { get; set; }
        public Organization Organization { get; set; }

        //event details
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Location { get; set; }
        public int VolunteersNeeded { get; set; }

        public DateTime ApplicationCloseDate { get; set; }
        public OpportunityOpenStatus Status { get; set; } = OpportunityOpenStatus.Open;

        //By WW
        // Emergency Mode Properties
        public bool IsEmergency { get; set; } = false;
        public CreditLevel? MinimumCreditLevel { get; set; } // Only volunteers with this credit level or higher can apply
        public bool AutoCloseWhenFull { get; set; } = true; // Emergency opportunities auto-close when full (no waitlist)


        // Navigation Properties
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}