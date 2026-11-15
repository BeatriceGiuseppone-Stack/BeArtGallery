using BeArtGallery.Authorization;
using BeArtGallery.Data;
using BeArtGallery.Services;
using BeArtGallery.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers
{
    // Check-in all'ingresso tramite QR code (solo Staff e Admin)
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public class BookingCheckInController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IBookingService _bookingService;

        public BookingCheckInController( ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IBookingService bookingService)
        {
            _context = context;
            _userManager = userManager;
            _bookingService = bookingService;
        }


        // Pagina aperta quando lo staff scansiona il QR del visitatore

        [HttpGet]
        public async Task<IActionResult> Scan(string? token)
        {
            // QR senza codice
            if (string.IsNullOrWhiteSpace(token))
            {
                return View( new BookingCheckInViewModel
                    {
                        IsValid = false,
                        ErrorMessage = "Il QR code non contiene una prenotazione valida."
                    });
            }


            // Cerca la prenotazione con il codice del QR
            var booking = await _context.Bookings.AsNoTracking().Include(b => b.GalleryActivity)
                    .Include(b => b.User).Include(b => b.CheckedInByUser)
                    .FirstOrDefaultAsync(b => b.QrToken == token);

            if (booking == null)
            {
                return View( new BookingCheckInViewModel
                    {
                        IsValid = false,
                        QrToken = token,
                        ErrorMessage = "Prenotazione non trovata. Il QR potrebbe essere non valido."
                    });
            }

            // Dati della prenotazione da mostrare allo staff
            var model = new BookingCheckInViewModel
                {
                    IsValid = true,
                    QrToken = token,
                    BookingId = booking.Id,
                    UserEmail = booking.User?.Email ?? string.Empty,
                    ActivityTitle = booking.GalleryActivity?.Title ?? string.Empty,
                    ActivityType = booking.GalleryActivity?.ActivityType ?? default,
                    StartDateTime = booking.GalleryActivity?.StartDateTime ?? default,
                    Location = booking.GalleryActivity?.Location,
                    NumberOfParticipants = booking.NumberOfParticipants,
                    Status = booking.Status,
                    CheckedInAt = booking.CheckedInAt,
                    CheckedInByEmail = booking.CheckedInByUser?.Email
                };

            return View(model);
        }


        // Conferma dell'ingresso del visitatore

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm( string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest();
            }

            // Utente dello staff che registra l'ingresso
            var currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Challenge();
            }

            // I controlli del check-in (prenotazione confermata, QR non già usato)
            // sono nel BookingService
            var result = await _bookingService.CheckInAsync( token, currentUserId);

            if (result.Success)
            {
                TempData["CheckInSuccess"] = result.Message;
            }
            else
            {
                TempData["CheckInError"] = result.Message;
            }

            // Torna alla pagina del QR con l'esito
            return RedirectToAction(nameof(Scan), new {token});
        }
    }
}