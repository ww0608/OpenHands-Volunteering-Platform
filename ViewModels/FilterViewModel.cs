//By WW
namespace OpenHandsVolunteerPlatform.ViewModels
{
    public class FilterViewModel
    {
        public string? Search { get; set; }
        public string? DateFilter { get; set; } // "today", "week", "month", "all"
        public string? WeekendFilter { get; set; } // "weekend", "weekday", "all"
        public string? Location { get; set; }
        public string? SortBy { get; set; } // "date", "volunteers", "title"
    }
}