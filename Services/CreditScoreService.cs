using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;

//By WW
namespace OpenHandsVolunteerPlatform.Services
{
    public interface ICreditScoreService
    {
        Task<CreditLevel> CalculateCreditLevel(Volunteer volunteer);
        Task UpdateCreditScore(Volunteer volunteer, string reason, string? opportunityId = null, string? organizationId = null);
        Task<List<CreditScoreHistory>> GetCreditScoreHistory(string volunteerId, string organizationId);
    }

    public class CreditScoreService : ICreditScoreService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreditScoreService> _logger;

        public CreditScoreService(ApplicationDbContext context, ILogger<CreditScoreService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task<CreditLevel> CalculateCreditLevel(Volunteer volunteer)
        {
            // If 3 or more no-shows -> Inactive (LOW)
            if (volunteer.NoShowCount >= 3)
                return Task.FromResult(CreditLevel.Inactive);

            // If 5 or more completed events -> Core (HIGH)
            if (volunteer.CompletedCount >= 5)
                return Task.FromResult(CreditLevel.Core);

            // Otherwise -> Growing (AVERAGE)
            return Task.FromResult(CreditLevel.Growing);
        }

        public async Task UpdateCreditScore(Volunteer volunteer, string reason, string? opportunityId = null, string? organizationId = null)
        {
            var previousLevel = volunteer.CreditLevel;
            var newLevel = await CalculateCreditLevel(volunteer);

            // Update the volunteer's credit level
            volunteer.CreditLevel = newLevel;

            // Create history record
            var history = new CreditScoreHistory
            {
                VolunteerId = volunteer.VolunteerId,
                PreviousLevel = previousLevel,
                NewLevel = newLevel,
                Reason = reason,
                OpportunityId = opportunityId,
                OrganizationId = organizationId,
                ChangedAt = DateTime.Now
            };

            _context.CreditScoreHistories.Add(history);

            // IMPORTANT: Save changes
            await _context.SaveChangesAsync();
        }

        public async Task<List<CreditScoreHistory>> GetCreditScoreHistory(string volunteerId, string organizationId)
        {
            return await _context.CreditScoreHistories
                .Include(h => h.Opportunity)
                .Where(h => h.VolunteerId == volunteerId && h.OrganizationId == organizationId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }
    }
}