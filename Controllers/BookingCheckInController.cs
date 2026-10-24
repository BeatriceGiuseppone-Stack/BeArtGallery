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
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public class BookingCheckInController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;


        public BookingCheckInController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =================================================
        // SCANSIONE QR
        // =================================================

        [HttpGet]
        public async Task<IActionResult> Scan(
            string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return View(
                    new BookingCheckInViewModel
                    {
                        IsValid = false,

                        ErrorMessage =
                            "Il QR code non contiene una prenotazione valida."
                    });
            }


            var booking =
                await _context.Bookings
                    .AsNoTracking()
                    .Include(b => b.GalleryActivity)
                    .Include(b => b.User)
                    .Include(b => b.CheckedInByUser)
                    .FirstOrDefaultAsync(b =>
                        b.QrToken == token);


            if (booking == null)
            {
                return View(
                    new BookingCheckInViewModel
                    {
                        IsValid = false,

                        QrToken = token,

                        ErrorMessage =
                            "Prenotazione non trovata. Il QR potrebbe essere non valido."
                    });
            }


            var model =
                new BookingCheckInViewModel
                {
                    IsValid = true,

                    QrToken =
                        token,

                    BookingId =
                        booking.Id,

                    UserEmail =
                        booking.User?.Email
                        ?? string.Empty,

                    ActivityTitle =
                        booking.GalleryActivity?.Title
                        ?? string.Empty,

                    ActivityType =
                        booking.GalleryActivity?.ActivityType
                        ?? default,

                    StartDateTime =
                        booking.GalleryActivity?.StartDateTime
                        ?? default,

                    Location =
                        booking.GalleryActivity?.Location,

                    NumberOfParticipants =
                        booking.NumberOfParticipants,

                    Status =
                        booking.Status,

                    CheckedInAt =
                        booking.CheckedInAt,

                    CheckedInByEmail =
                        booking.CheckedInByUser?.Email
                };


            return View(model);
        }


        // =================================================
        // CONFERMA CHECK-IN
        // =================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(
            string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest();
            }


            var booking =
                await _context.Bookings
                    .Include(b => b.GalleryActivity)
                    .FirstOrDefaultAsync(b =>
                        b.QrToken == token);


            if (booking == null)
            {
                TempData["CheckInError"] =
                    "Prenotazione non trovata.";

                return RedirectToAction(
                    nameof(Scan),
                    new
                    {
                        token
                    });
            }


            // =================================================
            // PRENOTAZIONE ANNULLATA
            // =================================================

            if (booking.Status !=
                BookingStatus.Confirmed)
            {
                TempData["CheckInError"] =
                    "Non è possibile effettuare il check-in: la prenotazione non è confermata.";

                return RedirectToAction(
                    nameof(Scan),
                    new
                    {
                        token
                    });
            }


            // =================================================
            // QR GIÀ UTILIZZATO
            // =================================================

            if (booking.CheckedInAt.HasValue)
            {
                TempData["CheckInError"] =
                    "Il check-in per questa prenotazione è già stato effettuato.";

                return RedirectToAction(
                    nameof(Scan),
                    new
                    {
                        token
                    });
            }


            var currentUserId =
                _userManager.GetUserId(User);


            if (string.IsNullOrWhiteSpace(
                currentUserId))
            {
                return Challenge();
            }


            // =================================================
            // REGISTRA INGRESSO
            // =================================================

            booking.CheckedInAt =
                DateTime.UtcNow;


            booking.CheckedInByUserId =
                currentUserId;


            await _context.SaveChangesAsync();


            TempData["CheckInSuccess"] =
                "Ingresso registrato correttamente.";


            return RedirectToAction(
                nameof(Scan),
                new
                {
                    token
                });
        }
    }
}