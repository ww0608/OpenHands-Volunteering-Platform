using OpenHandsVolunteerPlatform.Models.Enums;
using System.ComponentModel.DataAnnotations;

//By WW
namespace OpenHandsVolunteerPlatform.ViewModels
{
    // View Model for VIEWING profile
    public class VolProfileViewModel
    {
        public string VolunteerId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int Age { get; set; }
        public AvailabilityType Availability { get; set; }

        public string AvailabilityDisplay
        {
            get
            {
                return Availability switch
                {
                    AvailabilityType.Weekday => "Weekdays",
                    AvailabilityType.Weekend => "Weekends",
                    AvailabilityType.Morning => "Mornings",
                    AvailabilityType.Afternoon => "Afternoons",
                    AvailabilityType.Evening => "Evenings",
                    _ => "Flexible"
                };
            }
        }

        // THESE COME DIRECTLY FROM DATABASE - NO CALCULATION
        public int CompletedEvents { get; set; }
        public int TotalHours { get; set; }
        public int NoShowCount { get; set; }

        // CALCULATE CREDIT LEVEL FROM DATABASE VALUES
        public CreditLevel CreditLevel
        {
            get
            {
                // 🔴 Inactive (strict failure condition)
                if (NoShowCount >= 3 && CompletedEvents < 2)
                {
                    return CreditLevel.Inactive;
                }

                // 🟡 Core (High)
                // must meet BOTH conditions
                if (CompletedEvents >= 5 && NoShowCount < 3)
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

        public string CreditLevelClass
        {
            get
            {
                return CreditLevel switch
                {
                    CreditLevel.Inactive => "credit-low",
                    CreditLevel.Growing => "credit-average",
                    CreditLevel.Core => "credit-high",
                    _ => "credit-unknown"
                };
            }
        }

        public int AppliedCount { get; set; }
        public int WaitlistCount { get; set; }
        public DateTime JoinedDate { get; set; }
        public DateTime? LastActiveDate { get; set; }

        // MILESTONE - BASED ON COMPLETED EVENTS
        public string NextMilestone
        {
            get
            {
                // Inactive users (must recover)
                if (NoShowCount >= 3 && CompletedEvents < 2)
                {
                    int needed = 2 - CompletedEvents;
                    return $"⚠️ Complete {needed} more {(needed == 1 ? "event" : "events")} to recover from Inactive status.";
                }

                // Already Core
                if (CompletedEvents >= 5)
                {
                    return "🏆 Excellent! You're at the highest credit level. Keep it up!";
                }

                // Progress to Core
                int remaining = 5 - CompletedEvents;
                return $"⭐ Complete {remaining} more {(remaining == 1 ? "event" : "events")} to reach Core level.";
            }
        }

        public int ProgressToNextMilestone
        {
            get
            {
                // Recovery progress (Inactive users)
                if (NoShowCount >= 3 && CompletedEvents < 2)
                {
                    return Math.Min(100, (CompletedEvents * 100) / 2);
                }

                // Core complete
                if (CompletedEvents >= 5)
                    return 100;

                // Normal progress
                return Math.Min(100, (CompletedEvents * 100) / 5);
            }
        }
    }

    // View Model for EDITING profile
    public class VolEditViewModel
    {
        public string VolunteerId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Age is required")]
        [Range(18, 65, ErrorMessage = "Age must be between 18 and 65")]
        [Display(Name = "Age")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Please select your availability")]
        [Display(Name = "Availability")]
        public AvailabilityType Availability { get; set; }
    }
}