using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OpenHandsVolunteerPlatform.Data;
using OpenHandsVolunteerPlatform.Models;
using OpenHandsVolunteerPlatform.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

//namespace OpenHandsVolunteerPlatform.Controllers
//{
//    public class VolunteerController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public VolunteerController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        // GET: Volunteer
//        public async Task<IActionResult> Index()
//        {
//            var applicationDbContext = _context.Applications.Include(a => a.Opportunity).Include(a => a.Volunteer);
//            return View(await applicationDbContext.ToListAsync());
//        }

//        // GET: Volunteer/Details/5
//        public async Task<IActionResult> Details(string id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var application = await _context.Applications
//                .Include(a => a.Opportunity)
//                .Include(a => a.Volunteer)
//                .FirstOrDefaultAsync(m => m.ApplicationId == id);
//            if (application == null)
//            {
//                return NotFound();
//            }

//            return View(application);
//        }

//        // GET: Volunteer/Create
//        public IActionResult Create()
//        {
//            ViewData["OpportunityId"] = new SelectList(_context.Opportunities, "OpportunityId", "OpportunityId");
//            ViewData["VolunteerId"] = new SelectList(_context.Volunteers, "VolunteerId", "VolunteerId");
//            return View();
//        }

//        // POST: Volunteer/Create
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create([Bind("ApplicationId,OpportunityId,VolunteerId,Status,AppliedAt,AttendanceStatus")] Application application)
//        {
//            if (ModelState.IsValid)
//            {
//                _context.Add(application);
//                await _context.SaveChangesAsync();
//                return RedirectToAction(nameof(Index));
//            }
//            ViewData["OpportunityId"] = new SelectList(_context.Opportunities, "OpportunityId", "OpportunityId", application.OpportunityId);
//            ViewData["VolunteerId"] = new SelectList(_context.Volunteers, "VolunteerId", "VolunteerId", application.VolunteerId);
//            return View(application);
//        }

//        // GET: Volunteer/Edit/5
//        public async Task<IActionResult> Edit(string id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var application = await _context.Applications.FindAsync(id);
//            if (application == null)
//            {
//                return NotFound();
//            }
//            ViewData["OpportunityId"] = new SelectList(_context.Opportunities, "OpportunityId", "OpportunityId", application.OpportunityId);
//            ViewData["VolunteerId"] = new SelectList(_context.Volunteers, "VolunteerId", "VolunteerId", application.VolunteerId);
//            return View(application);
//        }

//        // POST: Volunteer/Edit/5
//        // To protect from overposting attacks, enable the specific properties you want to bind to.
//        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(string id, [Bind("ApplicationId,OpportunityId,VolunteerId,Status,AppliedAt,AttendanceStatus")] Application application)
//        {
//            if (id != application.ApplicationId)
//            {
//                return NotFound();
//            }

//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    _context.Update(application);
//                    await _context.SaveChangesAsync();
//                }
//                catch (DbUpdateConcurrencyException)
//                {
//                    if (!ApplicationExists(application.ApplicationId))
//                    {
//                        return NotFound();
//                    }
//                    else
//                    {
//                        throw;
//                    }
//                }
//                return RedirectToAction(nameof(Index));
//            }
//            ViewData["OpportunityId"] = new SelectList(_context.Opportunities, "OpportunityId", "OpportunityId", application.OpportunityId);
//            ViewData["VolunteerId"] = new SelectList(_context.Volunteers, "VolunteerId", "VolunteerId", application.VolunteerId);
//            return View(application);
//        }

//        // GET: Volunteer/Delete/5
//        public async Task<IActionResult> Delete(string id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var application = await _context.Applications
//                .Include(a => a.Opportunity)
//                .Include(a => a.Volunteer)
//                .FirstOrDefaultAsync(m => m.ApplicationId == id);
//            if (application == null)
//            {
//                return NotFound();
//            }

//            return View(application);
//        }

//        // POST: Volunteer/Delete/5
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(string id)
//        {
//            var application = await _context.Applications.FindAsync(id);
//            if (application != null)
//            {
//                _context.Applications.Remove(application);
//            }

//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        private bool ApplicationExists(string id)
//        {
//            return _context.Applications.Any(e => e.ApplicationId == id);
//        }
//    }
//}
namespace OpenHandsVolunteerPlatform.Controllers
{
    [Authorize(Roles = "Volunteer")]
    public class VolunteerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VolunteerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Browse opportunities
        public async Task<IActionResult> Index()
        {
            var opportunities = await _context.Opportunities
                .Include(o => o.Organization)
                .Where(o => o.Status == OpportunityOpenStatus.Open)
                .ToListAsync();

            return View(opportunities);
        }

        // View opportunity details
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
                return NotFound();

            var opportunity = await _context.Opportunities
                .Include(o => o.Organization)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null)
                return NotFound();

            return View(opportunity);
        }

        // Apply for opportunity
        [HttpPost]
        public async Task<IActionResult> Apply(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.UserId == userId);

            if (volunteer == null)
                return BadRequest("Volunteer profile not found.");

            var opportunity = await _context.Opportunities
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null)
                return NotFound();

            // prevent duplicate application
            bool alreadyApplied = await _context.Applications
                .AnyAsync(a => a.OpportunityId == id && a.VolunteerId == volunteer.VolunteerId);

            if (alreadyApplied)
            {
                TempData["Error"] = "You have already applied for this opportunity.";
                return RedirectToAction("Details", new { id });
            }

            var application = new Application
            {
                ApplicationId = Guid.NewGuid().ToString(),
                OpportunityId = opportunity.OpportunityId,
                VolunteerId = volunteer.VolunteerId,
                Status = ApplicationStatus.Applied,
                AppliedAt = DateTime.Now
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Application submitted successfully.";

            return RedirectToAction("Details", new { id });
        }
    }
}