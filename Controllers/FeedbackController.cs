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
    /// <summary>
    /// Feedback dei visitatori dopo una visita o un evento
    /// </summary>
    [Authorize]
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TimeProvider _timeProvider;

        public FeedbackController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            TimeProvider timeProvider)
        {
            _context = context;
            _userManager = userManager;
            _timeProvider = timeProvider;
        }

        // Visitatore: lascia un feedback

        // GET: /Feedback/Create?bookingId=5
        // Form con le stelle e il commento
        [HttpGet]
        public async Task<IActionResult> Create(int bookingId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // Controlla che la prenotazione possa ricevere un feedback
            var (booking, error) = await LoadFeedbackableBookingAsync(bookingId, userId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Index", "Bookings");
            }

            return View(new VisitFeedbackFormViewModel
            {
                BookingId = booking.Id,
                ActivityTitle = booking.GalleryActivity!.Title,
                StartDateTime = booking.GalleryActivity.StartDateTime
            });
        }


        // POST: /Feedback/Create
        // Salva il feedback
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VisitFeedbackFormViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // I controlli si ripetono sul server: i dati del browser possono essere modificati
            var (booking, error) = await LoadFeedbackableBookingAsync(model.BookingId, userId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Index", "Bookings");
            }

            model.ActivityTitle = booking.GalleryActivity!.Title;
            model.StartDateTime = booking.GalleryActivity.StartDateTime;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Salva voto, commento e data
            _context.VisitFeedbacks.Add(new VisitFeedback
            {
                BookingId = booking.Id,
                UserId = userId,
                Rating = model.Rating!.Value,
                Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim(),
                CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // L'indice univoco su BookingId blocca un secondo feedback
                // anche se il form viene inviato due volte
                TempData["ErrorMessage"] = "Hai già lasciato un feedback per questa attività.";

                return RedirectToAction("Index", "Bookings");
            }

            TempData["SuccessMessage"] = "Grazie! Il tuo feedback è stato registrato.";

            return RedirectToAction("Index", "Bookings");
        }


        // Staff e Admin: elenco dei feedback

        // GET: /Feedback?rating=5
        // Elenco dei feedback con media e filtro per voto
        [HttpGet]
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> Index(int? rating)
        {
            var model = new VisitFeedbackManagementViewModel
            {
                RatingFilter = rating is >= 1 and <= 5 ? rating : null
            };

            // Numero di feedback e media dei voti
            model.TotalFeedbacks = await _context.VisitFeedbacks.CountAsync();

            if (model.TotalFeedbacks > 0)
            {
                model.AverageRating = Math.Round( await _context.VisitFeedbacks.AverageAsync(f => (double)f.Rating), 1);
            }

            // Quanti feedback per ogni voto (calcolato dal database con GROUP BY)
            var distribution = await _context.VisitFeedbacks.GroupBy(f => f.Rating).Select(g => new { Rating = g.Key, Count = g.Count() })
                    .ToListAsync();

            foreach (var item in distribution)
            {
                if (item.Rating is >= 1 and <= 5)
                {
                    model.RatingDistribution[item.Rating - 1] = item.Count;
                }
            }

            // Applica il filtro per voto, se scelto
            var query = _context.VisitFeedbacks.AsNoTracking();

            if (model.RatingFilter.HasValue)
            {
                query = query.Where(f => f.Rating == model.RatingFilter.Value);
            }

            model.Feedbacks = await query.OrderByDescending(f => f.CreatedAt)
                    .Select(f => new VisitFeedbackListItemViewModel
                    {
                        Id = f.Id,
                        // Prenotazione e attività ci sono sempre (chiavi esterne obbligatorie)
                        ActivityTitle = f.Booking!.GalleryActivity!.Title,
                        ActivityStartDateTime = f.Booking.GalleryActivity.StartDateTime,
                        UserEmail = f.User != null && f.User.Email != null ? f.User.Email : string.Empty,
                        Rating = f.Rating,
                        Comment = f.Comment,
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync();

            return View(model);
        }


        // Controlla se la prenotazione può ricevere un feedback: deve essere
        // dell'utente, confermata, con check-in fatto e senza feedback
        private async Task<(Booking? Booking, string Error)> LoadFeedbackableBookingAsync(int bookingId, string userId)
        {
            var booking = await _context.Bookings.Include(b => b.GalleryActivity).Include(b => b.Feedback)
                    .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null || booking.GalleryActivity == null)
            {
                return (null, "Prenotazione non trovata.");
            }

            if (booking.Status != BookingStatus.Confirmed || !booking.CheckedInAt.HasValue)
            {
                return (null, "Puoi lasciare un feedback solo dopo aver partecipato all'attività.");
            }

            if (booking.Feedback != null)
            {
                return (null, "Hai già lasciato un feedback per questa attività.");
            }

            return (booking, string.Empty);
        }
    }
}