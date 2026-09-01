using OpenHandsVolunteerPlatform.Models.Enums;

//By ANGEL
namespace OpenHandsVolunteerPlatform.Models
{
    public class Notification
    {
        public string NotificationId { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public NotificationType Type { get; set; }

        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public string? RelatedId { get; set; } // e.g. OpportunityId or ApplicationId

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
