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
    // Visite ed eventi: elenco pubblico, scheda e gestione per lo staff
    public class GalleryActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GalleryActivitiesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: GalleryActivities
        // Elenco delle attività. Lo staff può vedere anche quelle passate e annullate.
        public async Task<IActionResult> Index(bool showCancelled = false, bool showPast = false)
        {
            var now = DateTime.Now;

            // Solo Staff e Admin possono vedere lo storico
            var canViewHistory = User.IsInRole(RoleNames.Staff) || User.IsInRole(RoleNames.Admin);

            // Un visitatore non può vedere le attività passate nemmeno cambiando l'indirizzo
            if (!canViewHistory)
            {
                showPast = false;
            }

            var query = _context.GalleryActivities.AsNoTracking().AsQueryable();

            // Di base si mostrano solo le attività future
            if (!showPast)
            {
                query = query.Where(a => a.StartDateTime >= now);
            }

            // Le attività annullate si vedono solo se richiesto
            if (!showCancelled)
            {
                query = query.Where(a => a.Status != GalleryActivityStatus.Cancelled);
            }

            // Ordina mettendo in fondo le annullate, poi per data,
            // e calcola i posti già prenotati
            var activities = await query.OrderBy(a => a.Status == GalleryActivityStatus.Cancelled ? 1 : 0)
                    .ThenBy(a => a.StartDateTime) 
                    .Select(a =>
                        new GalleryActivityViewModel
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
                            BookedParticipants = a.Bookings.Where(b => b.Status == BookingStatus.Confirmed)
                                    .Sum(b => (int?)b.NumberOfParticipants) ?? 0
                        }).ToListAsync();

            // Conta le attività annullate per il pulsante del filtro
            var cancelledQuery = _context.GalleryActivities.AsNoTracking().Where(a => a.Status == GalleryActivityStatus.Cancelled);

            if (!showPast)
            {
                cancelledQuery = cancelledQuery.Where(a => a.StartDateTime >= now);
            }

            var cancelledCount = await cancelledQuery.CountAsync();

            // Conta le attività passate (solo per Staff e Admin)
            var pastCount = 0;

            if (canViewHistory)
            {
                pastCount = await _context.GalleryActivities.AsNoTracking()
                        .CountAsync(a => a.StartDateTime < now);
            }

            // Valori che servono alla vista per i filtri
            ViewBag.ShowCancelled = showCancelled;

            ViewBag.CancelledCount = cancelledCount;

            ViewBag.ShowPast = showPast;

            ViewBag.PastCount = pastCount;

            ViewBag.CanViewHistory = canViewHistory;

            return View(activities);
        }

        // GET: GalleryActivities/Details/5
        // Scheda dell'attività con i posti prenotati
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

        // GET: GalleryActivities/Create
        // Form per una nuova attività (solo Staff e Admin)
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: GalleryActivities/Create
        // Salva la nuova attività
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
            // La data dell'attività deve essere futura
            if (galleryActivity.StartDateTime <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(galleryActivity.StartDateTime), "La data dell'attività deve essere futura.");
            }

            if (!ModelState.IsValid)
            {
                return View(galleryActivity);
            }

            // Una nuova attività parte sempre come "programmata"
            galleryActivity.Status = GalleryActivityStatus.Scheduled;

            _context.GalleryActivities.Add(galleryActivity);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Attività creata correttamente.";

            return RedirectToAction(nameof(Index));
        }

         // GET: GalleryActivities/Edit/5
         // Form di modifica dell'attività
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

            // Un'attività annullata non si modifica, così resta il suo storico
            if (galleryActivity.Status == GalleryActivityStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Non è possibile modificare un'attività annullata.";

                return RedirectToAction(nameof(Index));
            }

            // Un'attività già iniziata o finita non si modifica
            if (galleryActivity.StartDateTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Non è possibile modificare un'attività già iniziata o conclusa.";

                return RedirectToAction(nameof(Index));
            }

            return View(galleryActivity);
        }

        // POST: GalleryActivities/Edit/5
        // Salva le modifiche all'attività
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> Edit( int? id,
            [Bind(
                "Id," +
                "Title," +
                "Description," +
                "ActivityType," +
                "StartDateTime," +
                "DurationMinutes," +
                "MaxParticipants," +
                "Location"
            )] GalleryActivity galleryActivity)
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

            // Ripete i controlli: niente modifiche ad attività annullate
            if (existingActivity.Status == GalleryActivityStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Non è possibile modificare un'attività annullata.";

                return RedirectToAction(nameof(Index));
            }

            // Niente modifiche ad attività già iniziate
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

            // I posti non possono essere meno dei partecipanti già prenotati
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
                // Aggiorna solo i campi che si possono modificare
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

        // GET: GalleryActivities/Delete/5
        // Un'attività si può eliminare solo se non ha mai avuto prenotazioni,
        // altrimenti va annullata
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

        // POST: GalleryActivities/Delete/5
        // Elimina l'attività dopo la conferma (ripetendo il controllo)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
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

        // GET: GalleryActivities/Cancel/5
        // Pagina per annullare un'attività indicando il motivo
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var activity = await _context.GalleryActivities.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
            {
                return NotFound();
            }

            // Già annullata
            if (activity.Status == GalleryActivityStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "L'attività risulta già annullata.";

                return RedirectToAction(nameof(Index));
            }

            // Non si annulla un'attività già iniziata
            if (activity.StartDateTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Non è possibile annullare un'attività già iniziata o conclusa.";

                return RedirectToAction(nameof(Index));
            }

            // Prenotazioni confermate che verranno annullate
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

        // POST: GalleryActivities/Cancel
        // Annulla l'attività e tutte le sue prenotazioni
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel( CancelActivityViewModel model)
        {
            var activity = await _context.GalleryActivities.FirstOrDefaultAsync(a => a.Id == model.ActivityId);

            if (activity == null)
            {
                return NotFound();
            }

            // Prenotazioni confermate prese dal database
            var confirmedBookings = await _context.Bookings.Where(b => b.GalleryActivityId == activity.Id &&
                        b.Status == BookingStatus.Confirmed).ToListAsync();

            // Dati da mostrare di nuovo se il form ha errori
            model.Title = activity.Title;
            model.StartDateTime = activity.StartDateTime;
            model.ConfirmedBookings = confirmedBookings.Count;
            model.ConfirmedParticipants = confirmedBookings.Sum(b => b.NumberOfParticipants);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Ripete i controlli della pagina GET
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

            // Attività e prenotazioni si annullano insieme in una transazione
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Annulla l'attività
            activity.Status = GalleryActivityStatus.Cancelled;
            activity.CancelledAt = cancelledAt;
            activity.CancelledByUserId = currentUserId;
            activity.CancellationReason = cancellationReason;

            // Annulla in automatico tutte le prenotazioni confermate
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

        // Controlla se un'attività esiste
        private bool GalleryActivityExists(int id)
        {
            return _context.GalleryActivities.Any(a => a.Id == id);
        }
    }
}