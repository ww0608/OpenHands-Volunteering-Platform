using Microsoft.AspNetCore.Mvc;
using OpenHandsVolunteerPlatform.Models.Enums;

namespace OpenHandsVolunteerPlatform.ViewModels
{
    public class AdminUserViewModel
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime JoinedDate { get; set; }
        public int CompletedEvents { get; set; }
        public int NoShowCount { get; set; }
        public int TotalHours { get; set; }
        public CreditLevel CreditLevel { get; set; }
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
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdminUserDetailViewModel
    {
        public string UserId { get; set; }
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
        public DateTime JoinedDate { get; set; }
        public int CompletedEvents { get; set; }
        public int TotalHours { get; set; }
        public int NoShowCount { get; set; }
        public CreditLevel CreditLevel { get; set; }
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
        public List<UserApplicationViewModel> Applications { get; set; } = new();
    }
}



//using Microsoft.AspNetCore.Mvc;
//using OpenHandsVolunteerPlatform.Models.Enums;

////By ANGEL
//namespace OpenHandsVolunteerPlatform.ViewModels
//{
//    public class AdminUserViewModel
//    {
//        public string UserId { get; set; }
//        public string Name { get; set; }
//        public string Email { get; set; }
//        public DateTime JoinedDate { get; set; }
//        public int CompletedEvents { get; set; }
//        public int NoShowCount { get; set; }
//        public int TotalHours { get; set; }
//        public CreditLevel CreditLevel { get; set; }
//        public string CreditLevelDisplay
//        {
//            get
//            {
//                return CreditLevel switch
//                {
//                    CreditLevel.Core => "HIGH",
//                    CreditLevel.Growing => "AVERAGE",
//                    CreditLevel.Inactive => "LOW",
//                    _ => "UNKNOWN"
//                };
//            }
//        }
//        public string CreditLevelClass
//        {
//            get
//            {
//                return CreditLevel switch
//                {
//                    CreditLevel.Core => "credit-high",
//                    CreditLevel.Growing => "credit-average",
//                    CreditLevel.Inactive => "credit-low",
//                    _ => "credit-unknown"
//                };
//            }
//        }
//        public string Role { get; set; }
//        public bool IsActive { get; set; }
//    }

//    public class AdminUserDetailViewModel
//    {
//        public string UserId { get; set; }
//        public string Name { get; set; }
//        public string Email { get; set; }
//        public string Phone { get; set; }
//        public int Age { get; set; }
//        public AvailabilityType Availability { get; set; }
//        public string AvailabilityDisplay
//        {
//            get
//            {
//                return Availability switch
//                {
//                    AvailabilityType.Weekday => "Weekdays",
//                    AvailabilityType.Weekend => "Weekends",
//                    AvailabilityType.Morning => "Mornings",
//                    AvailabilityType.Afternoon => "Afternoons",
//                    AvailabilityType.Evening => "Evenings",
//                    _ => "Flexible"
//                };
//            }
//        }
//        public DateTime JoinedDate { get; set; }
//        public int CompletedEvents { get; set; }
//        public int TotalHours { get; set; }
//        public int NoShowCount { get; set; }
//        public CreditLevel CreditLevel { get; set; }
//        public string CreditLevelDisplay
//        {
//            get
//            {
//                return CreditLevel switch
//                {
//                    CreditLevel.Core => "HIGH",
//                    CreditLevel.Growing => "AVERAGE",
//                    CreditLevel.Inactive => "LOW",
//                    _ => "UNKNOWN"
//                };
//            }
//        }
//        public List<UserApplicationViewModel> Applications { get; set; } = new();
//    }
//}

