using System.ComponentModel.DataAnnotations;

//By ANGEL
namespace OpenHandsVolunteerPlatform.ViewModels
{
    public class RatingViewModel
    {
        public string OrganizationId { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string OpportunityTitle { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Please select 1 to 5 stars.")]
        public int Stars { get; set; }
        public string? Comment { get; set; }
        public bool HasRated { get; set; }
        public int? ExistingRating { get; set; }
    }
}