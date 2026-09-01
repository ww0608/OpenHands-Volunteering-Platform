using OpenHandsVolunteerPlatform.Models;

namespace OpenHandsVolunteerPlatform.ViewModels
{
    // ViewModels/ApplyViewModel.cs
    public class ApplyViewModel
    {
        public Opportunity Opportunity { get; set; }
        public Volunteer Volunteer { get; set; }
        public bool HasConflicts { get; set; }
        public List<Opportunity> ConflictingOpportunities { get; set; } = new();

        // Waitlist info
        public int CurrentVolunteers { get; set; }
        public int WaitlistCount { get; set; }
        public bool HasAvailableSlot { get; set; }
        public bool IsWaitlistFull { get; set; }
        public int MaxWaitlistSize { get; set; }
    }

    // ViewModels/WaitlistApplicationViewModel.cs
    public class WaitlistApplicationViewModel
    {
        public Application Application { get; set; }
        public int WaitlistPosition { get; set; }
        public int TotalWaitlist { get; set; }
    }

    // ViewModels/VolunteerApplicationsViewModel.cs
    public class VolunteerApplicationsViewModel
    {
        public List<Application> UpcomingApplications { get; set; } = new();
        public List<Application> PastApplications { get; set; } = new();
        public List<Application> OngoingApplications { get; set; } = new();
        public List<WaitlistApplicationViewModel> WaitlistApplications { get; set; } = new();
        public List<Application> WithdrawnApplications { get; set; } = new();

        //By ANGEL
        public bool IsFollowing { get; set; }
        public int FollowerCount { get; set; }

    }
}
