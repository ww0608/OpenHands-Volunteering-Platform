using Microsoft.AspNetCore.Identity;

namespace OpenHandsVolunteerPlatform.Models
{
    //ADDITIONAL FIELD for ALL USERS, extend from IdentityUser
    public class ApplicationUser : IdentityUser
    {
        // Additional Identity Properties
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public Volunteer? Volunteer { get; set; }
        public Organization? Organization { get; set; }
    }
}