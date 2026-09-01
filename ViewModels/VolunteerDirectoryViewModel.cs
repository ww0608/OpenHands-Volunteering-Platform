using Microsoft.AspNetCore.Mvc;
using OpenHandsVolunteerPlatform.Models.Enums;

//By WW
namespace OpenHandsVolunteerPlatform.ViewModels
{
    public class VolunteerDirectoryViewModel
    {
        public List<VolunteerDirectoryItemViewModel> Volunteers { get; set; } = new();
        public int TotalCount => Volunteers.Count;
        public int HighCount => Volunteers.Count(v => v.CreditLevel == CreditLevel.Core);
        public int AverageCount => Volunteers.Count(v => v.CreditLevel == CreditLevel.Growing);
        public int LowCount => Volunteers.Count(v => v.CreditLevel == CreditLevel.Inactive);
    }

    public class VolunteerDirectoryItemViewModel
    {
        public string VolunteerId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int Age { get; set; }
        public DateTime JoinedDate { get; set; }

        // Remove the setter for CreditLevel - make it calculate automatically
        public CreditLevel CreditLevel
        {
            get
            {
                // Calculate based on stats
                if (NoShowCount >= 3)
                    return CreditLevel.Inactive;
                if (CompletedEvents >= 5)
                    return CreditLevel.Core;
                return CreditLevel.Growing;
            }
        }

        public string CreditLevelDisplay
        {
            get
            {
                return CreditLevel switch
                {
                    CreditLevel.Core => "HIGH",
                    CreditLevel.Growing => "AVERAGE",
                    CreditLevel.Inactive => "LOW",
                    _ => "UNKNOWN"
                };
            }
        }

        public string CreditLevelClass
        {
            get
            {
                return CreditLevel switch
                {
                    CreditLevel.Core => "credit-high",
                    CreditLevel.Growing => "credit-average",
                    CreditLevel.Inactive => "credit-low",
                    _ => "credit-unknown"
                };
            }
        }

        // These are the actual stats from database
        public int CompletedEvents { get; set; }
        public int TotalHours { get; set; }
        public int NoShowCount { get; set; }

        public DateTime? LastAppliedDate { get; set; }
        public string LastOpportunity { get; set; }
    }
}