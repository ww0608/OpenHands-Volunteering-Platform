//By ANGEL
namespace OpenHandsVolunteerPlatform.Models
{
    public class Follow
    {
        public string FollowId { get; set; } = Guid.NewGuid().ToString();

        public string VolunteerId { get; set; }
        public Volunteer Volunteer { get; set; }

        public string OrganizationId { get; set; }
        public Organization Organization { get; set; }

        public DateTime FollowedAt { get; set; } = DateTime.Now;
    }
}
