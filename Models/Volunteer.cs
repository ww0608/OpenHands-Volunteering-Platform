using OpenHandsVolunteerPlatform.Models.Enums;
using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace OpenHandsVolunteerPlatform.Models
{
    public class Volunteer
    {
        public string VolunteerId { get; set; }

        public string UserId { get; set; }
    
        public ApplicationUser User { get; set; } //For Entity Framework navigation (Include queries)

        public string Name { get; set; }

        [Range(18, 65)]
        public int Age { get; set; }

        public string Phone { get; set; }

        public AvailabilityType Availability { get; set; }

        // Credit Score (auto-calculated)
        public CreditLevel CreditLevel { get; set; } = CreditLevel.Growing;
        public int TotalHours { get; set; } = 0;
        public int CompletedCount { get; set; } = 0;
        public int NoShowCount { get; set; } = 0;

        //By WW
        public void UpdatedCreditScore()
        {
            if (NoShowCount >= 3)
            {
                CreditLevel = CreditLevel.Inactive;
            }
            else if (CompletedCount >= 5)
            {
                CreditLevel = CreditLevel.Core;
            }
            else
            {
                CreditLevel = CreditLevel.Growing;
            }
        }


        // Navigation Properties
        public ICollection<Application> Applications { get; set; } = new List<Application>();

        //By WW
        public ICollection<CreditScoreHistory> CreditScoreHistories { get; set; } = new List<CreditScoreHistory>();

        //By ANGEL
        public ICollection<Follow> Follows { get; set; } = new List<Follow>();
        public ICollection<OrganizationRating> Ratings { get; set; } = new List<OrganizationRating>();

    }
}