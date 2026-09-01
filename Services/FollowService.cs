using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;

//By ANGEL
namespace OpenHandsVolunteerPlatform.Services
{
    public class FollowService
    {
        private readonly ApplicationDbContext _context;

        public FollowService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task FollowAsync(string volunteerId, string organizationId)
        {
            var already = await _context.Follows
                .AnyAsync(f => f.VolunteerId == volunteerId && f.OrganizationId == organizationId);

            if (already) return;

            _context.Follows.Add(new Follow
            {
                VolunteerId = volunteerId,
                OrganizationId = organizationId
            });

            await _context.SaveChangesAsync();
        }

        public async Task UnfollowAsync(string volunteerId, string organizationId)
        {
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.VolunteerId == volunteerId && f.OrganizationId == organizationId);

            if (follow == null) return;

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsFollowingAsync(string volunteerId, string organizationId)
        {
            return await _context.Follows
                .AnyAsync(f => f.VolunteerId == volunteerId && f.OrganizationId == organizationId);
        }

        public async Task<List<Organization>> GetFollowedOrganizationsAsync(string volunteerId)
        {
            return await _context.Follows
                .Where(f => f.VolunteerId == volunteerId)
                .Include(f => f.Organization)
                .Select(f => f.Organization)
                .ToListAsync();
        }

        public async Task<int> GetFollowerCountAsync(string organizationId)
        {
            return await _context.Follows
                .CountAsync(f => f.OrganizationId == organizationId);
        }
    }
}
