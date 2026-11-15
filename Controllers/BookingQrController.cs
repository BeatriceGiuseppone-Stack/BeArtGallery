using BeArtGallery.Authorization;
using BeArtGallery.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace BeArtGallery.Controllers
{
    // Crea l'immagine del QR code di una prenotazione
    [Authorize]
    public class BookingQrController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public BookingQrController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // Restituisce il QR come immagine PNG

        [HttpGet]
        public async Task<IActionResult> Image(int id)
        {
            var booking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            // Utente collegato e suo ruolo
            var currentUserId = _userManager.GetUserId(User);

            var isStaffOrAdmin = User.IsInRole(RoleNames.Staff) || User.IsInRole(RoleNames.Admin);

            // Un visitatore può vedere solo il QR delle sue prenotazioni

            if (!isStaffOrAdmin && booking.UserId != currentUserId)
            {
                return Forbid();
            }

            // Prenotazione senza codice QR
            if (string.IsNullOrWhiteSpace(booking.QrToken))
            {
                return NotFound();
            }

            // Indirizzo che si apre quando lo staff scansiona il QR

            var checkInUrl = Url.Action(action: "Scan", controller: "BookingCheckIn",
                    values: new{ token = booking.QrToken }, protocol: Request.Scheme);

            if (string.IsNullOrWhiteSpace(checkInUrl))
            {
                return BadRequest();
            }

            // Crea il QR code con la libreria QRCoder
            using var generator = new QRCodeGenerator();

            using var qrData = generator.CreateQrCode(checkInUrl, QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(qrData);

            var qrBytes = qrCode.GetGraphic( pixelsPerModule: 20);

            return File(qrBytes, "image/png");
        }
    }
}