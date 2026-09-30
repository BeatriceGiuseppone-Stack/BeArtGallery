using BeArtGallery.Authorization;
using BeArtGallery.Data;
using BeArtGallery.Models;
using BeArtGallery.Models.Enums;
using BeArtGallery.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers
{
    public class GalleryActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GalleryActivitiesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // INDEX PUBBLICO
        public async Task<IActionResult> Index(bool showCancelled = false)
        {
            var now = DateTime.Now;

            var baseQuery = _context.GalleryActivities.AsNoTracking().Where(a => a.StartDateTime >= now);

            var cancelledCount = await baseQuery.CountAsync(a => a.Status == GalleryActivityStatus.Cancelled);

            if (!showCancelled)
            {
                baseQuery = baseQuery.Where(a => a.Status != GalleryActivityStatus.Cancelled);
            }

            var activities = await baseQuery.OrderBy(a => a.Status == GalleryActivityStatus.Cancelled ? 1 : 0).ThenBy(a => a.StartDateTime)
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
                    Status = a.Status,
                    CancellationReason = a.CancellationReason,
                    CancelledAt = a.CancelledAt,
                    HasBookings = a.Bookings.Any(),
                    BookedParticipants = a.Bookings.Where(b => b.Status == BookingStatus.Confirmed).Sum(b => (int?)b.NumberOfParticipants) ?? 0
                })
                .ToListAsync();

            ViewBag.ShowCancelled = showCancelled;
            ViewBag.CancelledCount = cancelledCount;

            return View(activities);
        }

        // DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activity = await _context.GalleryActivities.AsNoTracking().Where(a => a.Id == id)
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
                    Status = a.Status,
                    HasBookings = a.Bookings.Any(),
                    CancellationReason = a.CancellationReason,
                    CancelledAt = a.CancelledAt,
                    BookedParticipants = a.Bookings.Where(b => b.Status == BookingStatus.Confirmed).Sum(b => (int?)b.NumberOfParticipants) ?? 0
                }).FirstOrDefaultAsync();

            if (activity == null)
            {
                return NotFound();
            }

            return View(activity);
        }

        // CREATE - GET
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public IActionResult Create()
        {
            return View();
        }

        // CREATE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> Create(
            [Bind(
                "Title," +
                "Description," +
                "ActivityType," +
                "StartDateTime," +
                "DurationMinutes," +
                "MaxParticipants," +
                "Location"
            )]
            GalleryActivity galleryActivity)
        {
            // Le attività non possono avere data minore ad oggi
            if (galleryActivity.StartDateTime <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(galleryActivity.StartDateTime), "La data dell'attività deve essere futura.");
            }

            if (!ModelState.IsValid)
            {
                return View(galleryActivity);
            }

            // inizializziamo lo stato
            galleryActivity.Status = GalleryActivityStatus.Scheduled;

            _context.GalleryActivities.Add(galleryActivity);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Attività creata correttamente.";

            return RedirectToAction(nameof(Index));
        }

         // EDIT - GET
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var galleryActivity = await _context.GalleryActivities.FindAsync(id);

            if (galleryActivity == null)
            {
                return NotFound();
            }

            // Un'attività annullata mantiene il suo storico
            //e non viene modificata con il normale Edit.
            if (galleryActivity.Status == GalleryActivityStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Non è possibile modificare un'attività annullata.";

                return RedirectToAction(nameof(Index));
            }

            // Non modifichiamo eventi già iniziati/conclusi.
            if (galleryActivity.StartDateTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Non è possibile modificare un'attività già iniziata o conclusa.";

                return RedirectToAction(nameof(Index));
            }

            return View(galleryActivity);
        }

        // EDIT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
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
                "Location"
            )]
            GalleryActivity galleryActivity)
        {
            if (id == null || id != galleryActivity.Id)
            {
                return NotFound();
            }

            var existingActivity =  await _context.GalleryActivities.FirstOrDefaultAsync(a => a.Id == id);

            if (existingActivity == null)
            {
                return NotFound();
            }

            // Non modifichiamo attività annullate
            if (existingActivity.Status == GalleryActivityStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Non è possibile modificare un'attività annullata.";

                return RedirectToAction(nameof(Index));
            }

            // Non modifichiamo attività già iniziate
            if (existingActivity.StartDateTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Non è possibile modificare un'attività già iniziata o conclusa.";

                return RedirectToAction(nameof(Index));
            }

            // La nuova data deve essere futura
            if (galleryActivity.StartDateTime <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(galleryActivity.StartDateTime), "La data dell'attività deve essere futura.");
            }

            // Controllo capienza
            var bookedParticipants = await _context.Bookings.Where(b => b.GalleryActivityId == existingActivity.Id &&
                        b.Status == BookingStatus.Confirmed).SumAsync(b => (int?)b.NumberOfParticipants) ?? 0;

            if (galleryActivity.MaxParticipants < bookedParticipants)
            {
                ModelState.AddModelError(nameof(galleryActivity.MaxParticipants), $"La capienza non può essere inferiore ai " +
                    $"{bookedParticipants} partecipanti già prenotati.");
            }

            if (!ModelState.IsValid)
            {
                return View(galleryActivity);
            }

            try
            {
                // Aggiorniamo SOLO i dati modificabili.
                existingActivity.Title = galleryActivity.Title;
                existingActivity.Description = galleryActivity.Description;
                existingActivity.ActivityType = galleryActivity.ActivityType;
                existingActivity.StartDateTime = galleryActivity.StartDateTime;
                existingActivity.DurationMinutes = galleryActivity.DurationMinutes;
                existingActivity.MaxParticipants = galleryActivity.MaxParticipants;
                existingActivity.Location = galleryActivity.Location;

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

            TempData["SuccessMessage"] = "Attività modificata correttamente.";

            return RedirectToAction(nameof(Index));
        }

        // DELETE - GET
        // Eliminazione consentita SOLO se non è mai
        // esistita alcuna prenotazione.
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var galleryActivity = await _context.GalleryActivities.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

            if (galleryActivity == null)
            {
                return NotFound();
            }

            var hasBookings = await _context.Bookings.AnyAsync(b => b.GalleryActivityId == id);

            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Non è possibile eliminare questa attività " +
                    "perché esistono prenotazioni associate. " + "Puoi annullare l'attività mantenendo lo storico.";

                return RedirectToAction(nameof(Index));
            }

            return View(galleryActivity);
        }

        // DELETE - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var galleryActivity = await _context.GalleryActivities.FindAsync(id);

            if (galleryActivity == null)
            {
                return NotFound();
            }

            var hasBookings = await _context.Bookings.AnyAsync(b => b.GalleryActivityId == id);

            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Non è possibile eliminare questa attività " + "perché esistono prenotazioni associate.";

                return RedirectToAction(nameof(Index));
            }

            _context.GalleryActivities.Remove(galleryActivity);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Attività eliminata correttamente.";

            return RedirectToAction(nameof(Index));
        }

        // CANCEL ATTIVITÀ - GET
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var activity = await _context.GalleryActivities.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
            {
                return NotFound();
            }

            if (activity.Status == GalleryActivityStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "L'attività risulta già annullata.";

                return RedirectToAction(nameof(Index));
            }

            if (activity.StartDateTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Non è possibile annullare un'attività già iniziata o conclusa.";

                return RedirectToAction(nameof(Index));
            }

            var confirmedBookings = await _context.Bookings.Where(b => b.GalleryActivityId == id &&
                        b.Status == BookingStatus.Confirmed).ToListAsync();

            var model = new CancelActivityViewModel
                {
                    ActivityId = activity.Id,
                    Title = activity.Title,
                    StartDateTime = activity.StartDateTime,
                    ConfirmedBookings = confirmedBookings.Count,
                    ConfirmedParticipants = confirmedBookings.Sum(b => b.NumberOfParticipants)
                };

            return View(model);
        }

        // CANCEL ATTIVITÀ - POST
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(
            CancelActivityViewModel model)
        {
            var activity = await _context.GalleryActivities.FirstOrDefaultAsync(a => a.Id == model.ActivityId);

            if (activity == null)
            {
                return NotFound();
            }

            // Recuperiamo sempre i dati reali dal DB.
            var confirmedBookings = await _context.Bookings.Where(b => b.GalleryActivityId == activity.Id &&
                        b.Status == BookingStatus.Confirmed).ToListAsync();

            // Ripopoliamo i dati non provenienti dal form.
            // Servono se ModelState non è valido
            // e dobbiamo mostrare nuovamente la View.
            model.Title = activity.Title;
            model.StartDateTime = activity.StartDateTime;
            model.ConfirmedBookings = confirmedBookings.Count;
            model.ConfirmedParticipants = confirmedBookings.Sum(b => b.NumberOfParticipants);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (activity.Status == GalleryActivityStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "L'attività risulta già annullata.";

                return RedirectToAction(nameof(Index));
            }

            if (activity.StartDateTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Non è possibile annullare un'attività già iniziata o conclusa.";

                return RedirectToAction(nameof(Index));
            }

            var currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Challenge();
            }

            var cancelledAt = DateTime.UtcNow;

            var cancellationReason = model.CancellationReason.Trim();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // ANNULLAMENTO ATTIVITÀ
            activity.Status = GalleryActivityStatus.Cancelled;
            activity.CancelledAt = cancelledAt;
            activity.CancelledByUserId = currentUserId;
            activity.CancellationReason = cancellationReason;

            // ANNULLAMENTO AUTOMATICO PRENOTAZIONI
            foreach (var booking in confirmedBookings)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.CancelledAt = cancelledAt;
                booking.CancelledByUserId = currentUserId;
                booking.CancellationType = BookingCancellationType.ActivityCancelled;
                booking.CancellationReason = cancellationReason;
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"L'attività \"{activity.Title}\" è stata annullata. " +
                $"{confirmedBookings.Count} prenotazioni sono state aggiornate.";

            return RedirectToAction(nameof(Index));
        }

        // HELPER
        private bool GalleryActivityExists(int id)
        {
            return _context.GalleryActivities
                .Any(a => a.Id == id);
        }
    }
}