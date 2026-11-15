using BeArtGallery.Authorization;
using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using BeArtGallery.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers
{
    // Prenotazioni: pagina del visitatore e gestione per lo staff.
    // Serve sempre l'accesso.
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Bookings
        // Le prenotazioni dell'utente collegato
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // Le prenotazioni create prima del QR code non hanno il codice:
            // lo genera adesso

            var bookingsWithoutQr = await _context.Bookings.Where(b => b.UserId == userId && b.QrToken == null).ToListAsync();

            if (bookingsWithoutQr.Any())
            {
                foreach (var booking in bookingsWithoutQr)
                {
                    booking.QrToken = Guid.NewGuid().ToString("N");
                }

                await _context.SaveChangesAsync();
            }

            // Carica le prenotazioni dell'utente con i dati dell'attività

            var bookings = await _context.Bookings.AsNoTracking().Include(b => b.GalleryActivity)
                    .Where(b => b.UserId == userId).OrderByDescending(b => b.GalleryActivity!.StartDateTime)
                    .Select(b => new MyBookingViewModel
                        {
                            BookingId = b.Id,
                            ActivityTitle = b.GalleryActivity != null ? b.GalleryActivity.Title : string.Empty,
                            ActivityType = b.GalleryActivity!.ActivityType,
                            StartDateTime = b.GalleryActivity.StartDateTime,
                            Location = b.GalleryActivity.Location,
                            NumberOfParticipants = b.NumberOfParticipants,
                            BookingDate = b.BookingDate,
                            Status = b.Status,
                            CancelledAt = b.CancelledAt,
                            CancellationType = b.CancellationType,
                            CancellationReason = b.CancellationReason,
                            QrToken = b.QrToken,
                            CheckedInAt = b.CheckedInAt,
                            HasFeedback = b.Feedback != null,
                            FeedbackRating = b.Feedback != null ? (int?)b.Feedback.Rating : null
                        }).ToListAsync();

            return View(bookings);
        }

        // GET: /Bookings/Manage
        // Elenco di tutte le prenotazioni con i filtri (solo Staff e Admin)
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> Manage(int? activityId, BookingStatus? status, DateTime? fromDate,
            DateTime? toDate, string? userEmail)
        {
            var query = _context.Bookings.AsNoTracking().Include(b => b.GalleryActivity).Include(b => b.User).AsQueryable();

            // Filtro per attività
            if (activityId.HasValue)
            {
                query = query.Where( b => b.GalleryActivityId == activityId.Value);
            }

            // Filtro per stato della prenotazione
            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            // Filtro per data dell'attività: dal giorno
            if (fromDate.HasValue)
            {
                query = query.Where(b => b.GalleryActivity != null && b.GalleryActivity.StartDateTime >= fromDate.Value);
            }

            // Filtro per data dell'attività: fino al giorno (compreso)
            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);

                query = query.Where(b => b.GalleryActivity != null && b.GalleryActivity.StartDateTime < endDate);
            }

            // Filtro per email del visitatore
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                query = query.Where(b => b.User != null && b.User.Email != null && b.User.Email.Contains(userEmail));
            }

            // Esegue la ricerca con i filtri scelti
            var bookings = await query.OrderByDescending(b => b.GalleryActivity!.StartDateTime)
                .Select(b => new BookingManagementViewModel
                {
                    BookingId = b.Id,
                    UserEmail = b.User != null ? b.User.Email ?? string.Empty : string.Empty,
                    ActivityTitle = b.GalleryActivity != null ? b.GalleryActivity.Title : string.Empty,
                    ActivityType = b.GalleryActivity!.ActivityType,
                    StartDateTime = b.GalleryActivity.StartDateTime,
                    Location = b.GalleryActivity.Location,
                    NumberOfParticipants = b.NumberOfParticipants,
                    BookingDate = b.BookingDate,
                    Status = b.Status
                })
                .ToListAsync();

            // Elenco delle attività per il menu del filtro
            var activities = await _context.GalleryActivities.AsNoTracking().OrderByDescending(a => a.StartDateTime)
                .Select(a => new BookingActivityFilterViewModel
                {
                    Id = a.Id,
                    Title = a.Title
                })
                .ToListAsync();

            var model = new BookingManagementPageViewModel
            {
                Bookings = bookings,
                Activities = activities,
                ActivityId = activityId,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                UserEmail = userEmail
            };

            return View(model);
        }

        // GET: /Bookings/CancelByStaff/5
        // Pagina in cui lo staff annulla una prenotazione indicando il motivo
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        [HttpGet]
        public async Task<IActionResult> CancelByStaff(int id)
        {
            var booking = await _context.Bookings.AsNoTracking().Include(b => b.User)
                .Include(b => b.GalleryActivity).FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Una prenotazione già annullata non si può annullare di nuovo
            if (booking.Status != BookingStatus.Confirmed)
            {
                TempData["ErrorMessage"] = "La prenotazione risulta già annullata.";

                return RedirectToAction(nameof(Manage));
            }

            // Non si può annullare se l'attività è già iniziata
            if (booking.GalleryActivity == null || booking.GalleryActivity.StartDateTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Non è possibile annullare una prenotazione relativa a un'attività già iniziata.";

                return RedirectToAction(nameof(Manage));
            }

            var model = new StaffCancelBookingViewModel
            {
                BookingId = booking.Id,
                UserEmail = booking.User?.Email ?? string.Empty,
                ActivityTitle = booking.GalleryActivity.Title,
                StartDateTime = booking.GalleryActivity.StartDateTime
            };

            return View(model);
        }

        // POST: /Bookings/CancelByStaff
        // Salva l'annullamento fatto dallo staff
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelByStaff(
            StaffCancelBookingViewModel model)
        {
            var booking = await _context.Bookings.Include(b => b.User).Include(b => b.GalleryActivity)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null)
            {
                return NotFound();
            }

            // Dati da mostrare di nuovo se il form ha errori
            model.UserEmail = booking.User?.Email ?? string.Empty;

            model.ActivityTitle = booking.GalleryActivity?.Title ?? string.Empty;

            model.StartDateTime = booking.GalleryActivity?.StartDateTime ?? default;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Ripete gli stessi controlli della pagina GET
            if (booking.Status != BookingStatus.Confirmed)
            {
                TempData["ErrorMessage"] = "La prenotazione risulta già annullata.";

                return RedirectToAction(nameof(Manage));
            }

            if (booking.GalleryActivity == null || booking.GalleryActivity.StartDateTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Non è possibile annullare questa prenotazione.";

                return RedirectToAction(nameof(Manage));
            }

            var currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Challenge();
            }

            // Segna la prenotazione come annullata e salva chi l'ha annullata e perché
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancelledByUserId = currentUserId;
            booking.CancellationType = BookingCancellationType.StaffOrAdmin;
            booking.CancellationReason = model.CancellationReason.Trim();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"La prenotazione di {model.UserEmail} è stata annullata.";

            return RedirectToAction(nameof(Manage));
        }
    }
}