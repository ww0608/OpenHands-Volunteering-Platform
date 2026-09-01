using OpenHandsVolunteerPlatform.Models.Enums;
using OpenHandsVolunteerPlatform.ViewModels;

namespace OpenHandsVolunteerPlatform.ViewModels

{

    public class AdminOrgViewModel

    {

        public required string OrganizationId { get; set; }

        public required string OrganizationName { get; set; }

        public required string Email { get; set; }

        public DateTime JoinedDate { get; set; }

        public int EventCount { get; set; }

        public string? VerificationDocumentPath { get; set; }

        public VerificationStatus VerificationStatus { get; set; }

        public string StatusDisplay => VerificationStatus switch
        {
            VerificationStatus.Approved => "Approved",
            VerificationStatus.Pending => "Pending",
            VerificationStatus.Rejected => "Rejected",
            _ => VerificationStatus.ToString()
        };

    }

}


//using OpenHandsVolunteerPlatform.Models.Enums;
//using OpenHandsVolunteerPlatform.ViewModels;

////By ANGEL
//namespace OpenHandsVolunteerPlatform.ViewModels

//{
//    public class AdminOrgViewModel

//    {

//        public required string OrganizationId { get; set; }

//        public required string OrganizationName { get; set; }

//        public required string Email { get; set; }

//        public DateTime JoinedDate { get; set; }

//        public int EventCount { get; set; }

//        public string? VerificationDocumentPath { get; set; }

//        public VerificationStatus VerificationStatus { get; set; }

//        //ANGEL:
//        public string StatusDisplay => VerificationStatus switch
//        {
//            VerificationStatus.Approved => "Approved",
//            VerificationStatus.Pending => "Pending",
//            VerificationStatus.Rejected => "Rejected",
//            _ => VerificationStatus.ToString()
//        };

//        //WW: (conflict)
//        public string StatusDisplay
//        {
//            get
//            {
//                return VerificationStatus switch
//                {
//                    VerificationStatus.Approved => "Approved",
//                    VerificationStatus.Pending => "Pending",
//                    VerificationStatus.Rejected => "Rejected",
//                    _ => "Unknown"
//                };
//            }
//        }


//    }

//}
