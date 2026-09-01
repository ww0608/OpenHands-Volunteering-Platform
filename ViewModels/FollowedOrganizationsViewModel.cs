using OpenHandsVolunteerPlatform.Models;

//By ANGEL
namespace OpenHandsVolunteerPlatform.ViewModels
{
    public class FollowedOrganizationsViewModel
    {
        public List<FollowedOrgItem> FollowedOrgs { get; set; } = new();
    }

    public class FollowedOrgItem
    {
        public Organization Organization { get; set; } = new();
        public int FollowerCount { get; set; }
        public DateTime FollowedAt { get; set; }
    }
}