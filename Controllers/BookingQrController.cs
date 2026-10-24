using BeArtGallery.Authorization;
using BeArtGallery.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace BeArtGallery.Controllers
{
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


        // =================================================
        // GENERAZIONE QR PRENOTAZIONE
        // =================================================

        [HttpGet]
        public async Task<IActionResult> Image(
            int id)
        {
            var booking =
                await _context.Bookings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b =>
                        b.Id == id);


            if (booking == null)
            {
                return NotFound();
            }


            var currentUserId =
                _userManager.GetUserId(User);


            var isStaffOrAdmin =
                User.IsInRole(RoleNames.Staff) ||
                User.IsInRole(RoleNames.Admin);


            // Il visitatore può vedere solamente
            // il QR delle proprie prenotazioni.

            if (!isStaffOrAdmin &&
                booking.UserId != currentUserId)
            {
                return Forbid();
            }


            if (string.IsNullOrWhiteSpace(
                booking.QrToken))
            {
                return NotFound();
            }


            // Questo sarà il link aperto
            // dallo Staff quando scansiona il QR.

            var checkInUrl =
                Url.Action(
                    action: "Scan",
                    controller: "BookingCheckIn",
                    values: new
                    {
                        token =
                            booking.QrToken
                    },
                    protocol:
                        Request.Scheme);


            if (string.IsNullOrWhiteSpace(
                checkInUrl))
            {
                return BadRequest();
            }


            using var generator =
                new QRCodeGenerator();


            using var qrData =
                generator.CreateQrCode(
                    checkInUrl,
                    QRCodeGenerator.ECCLevel.Q);

            var qrCode =
                new PngByteQRCode(
                    qrData);

            var qrBytes = qrCode.GetGraphic( pixelsPerModule: 20);

            return File(
                qrBytes,
                "image/png");
        }
    }
}