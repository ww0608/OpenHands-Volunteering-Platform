using OpenHandsVolunteerPlatform.Models.Enums;

//By WW
namespace OpenHandsVolunteerPlatform.Models
{
    public class CreditScoreHistory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Which volunteer
        public string VolunteerId { get; set; }
        public Volunteer Volunteer { get; set; }

        // Old and new values
        public CreditLevel PreviousLevel { get; set; }
        public CreditLevel NewLevel { get; set; }

        // What caused the change
        public string Reason { get; set; } // e.g., "Completed event", "No-show", "Reached 5 completed events"

        // Which opportunity caused this (if applicable)
        public string? OpportunityId { get; set; }
        public Opportunity? Opportunity { get; set; }

        // When it happened
        public DateTime ChangedAt { get; set; } = DateTime.Now;

        // For organization to see
        public string OrganizationId { get; set; }
        public Organization Organization { get; set; }


        public CreditLevel CreditLevel
        {
            get
            {
                // 🔴 Inactive (strict failure condition)
                if (Volunteer.NoShowCount >= 3 && Volunteer.CompletedCount < 2)
                {
                    return CreditLevel.Inactive;
                }

                // 🟡 Core (High)
                // must meet BOTH conditions
                if (Volunteer.CompletedCount >= 5 && Volunteer.NoShowCount < 3)
                {
                    return CreditLevel.Core;
                }

                // 🔵 Growing (Average)
                // default OR penalized Core users
                return CreditLevel.Growing;
            }
        }

        public string CreditLevelDisplay
        {
            get
            {
                return CreditLevel switch
                {
                    CreditLevel.Inactive => "LOW",
                    CreditLevel.Growing => "AVERAGE",
                    CreditLevel.Core => "HIGH",
                    _ => "UNKNOWN"
                };
            }
        }
    }
}