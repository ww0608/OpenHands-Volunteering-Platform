using System.ComponentModel.DataAnnotations;

//By ANGEL
namespace OpenHandsVolunteerPlatform.Models
{
    public class OrganizationRating
    {
        public string RatingId { get; set; } = Guid.NewGuid().ToString();

        public string VolunteerId { get; set; }
        public Volunteer Volunteer { get; set; }

        public string OrganizationId { get; set; }
        public Organization Organization { get; set; }

        public string? OpportunityId { get; set; } // which event they attended

        [Range(1, 5)]
        public int Stars { get; set; }

        public string? Comment { get; set; }

        public DateTime RatedAt { get; set; } = DateTime.Now;
    }
}
