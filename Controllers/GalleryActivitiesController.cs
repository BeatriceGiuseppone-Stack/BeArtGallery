using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeArtGallery.Models;
using BeArtGallery.Data;
using BeArtGallery.ViewModels;
using BeArtGallery.Models.Enums;

public class GalleryActivitiesController : Controller
{
    private readonly ApplicationDbContext _context;

    public GalleryActivitiesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: GalleryActivities
    public async Task<IActionResult> Index()
    {
        var activities = await _context.GalleryActivities
            .Where(a => a.IsActive && a.StartDateTime >= DateTime.Now)
            .OrderBy(a => a.StartDateTime)
            .Select(a => new GalleryActivityViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                ActivityType = a.ActivityType,
                StartDateTime = a.StartDateTime,
                DurationMinutes = a.DurationMinutes,
                MaxParticipants = a.MaxParticipants,
                Location = a.Location,
                IsActive = a.IsActive,

                BookedParticipants = a.Bookings
                    .Where(b => b.Status == BookingStatus.Confirmed)
                    .Sum(b => (int?)b.NumberOfParticipants) ?? 0
            })
            .ToListAsync();

        return View(activities);
    }

    // GET: GalleryActivities/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var activity = await _context.GalleryActivities
            .Where(a => a.Id == id)
            .Select(a => new GalleryActivityViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                ActivityType = a.ActivityType,
                StartDateTime = a.StartDateTime,
                DurationMinutes = a.DurationMinutes,
                MaxParticipants = a.MaxParticipants,
                Location = a.Location,
                IsActive = a.IsActive,

                BookedParticipants = a.Bookings
                    .Where(b => b.Status == BookingStatus.Confirmed)
                    .Sum(b => (int?)b.NumberOfParticipants) ?? 0
            })
            .FirstOrDefaultAsync();

        if (activity == null)
        {
            return NotFound();
        }

        return View(activity);
    }
    // GET: GalleryActivities/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: GalleryActivities/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(
            "Id," +
            "Title," +
            "Description," +
            "ActivityType," +
            "StartDateTime," +
            "DurationMinutes," +
            "MaxParticipants," +
            "Location," +
            "IsActive"
        )]
        GalleryActivity galleryActivity)
    {
        if (ModelState.IsValid)
        {
            _context.Add(galleryActivity);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(galleryActivity);
    }

    // GET: GalleryActivities/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var galleryActivity =
            await _context.GalleryActivities.FindAsync(id);

        if (galleryActivity == null)
        {
            return NotFound();
        }

        return View(galleryActivity);
    }

    // POST: GalleryActivities/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind(
            "Id," +
            "Title," +
            "Description," +
            "ActivityType," +
            "StartDateTime," +
            "DurationMinutes," +
            "MaxParticipants," +
            "Location," +
            "IsActive"
        )]
        GalleryActivity galleryActivity)
    {
        if (id != galleryActivity.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(galleryActivity);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GalleryActivityExists(galleryActivity.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        return View(galleryActivity);
    }

    // GET: GalleryActivities/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var galleryActivity = await _context.GalleryActivities
            .Include(a => a.Bookings)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (galleryActivity == null)
        {
            return NotFound();
        }

        return View(galleryActivity);
    }

    // POST: GalleryActivities/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var galleryActivity =
            await _context.GalleryActivities.FindAsync(id);

        if (galleryActivity != null)
        {
            _context.GalleryActivities.Remove(galleryActivity);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool GalleryActivityExists(int? id)
    {
        return _context.GalleryActivities
            .Any(a => a.Id == id);
    }
}