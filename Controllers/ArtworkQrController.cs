using BeArtGallery.Authorization;
using BeArtGallery.Data;
using BeArtGallery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace BeArtGallery.Controllers
{
    // QR code delle opere: il visitatore lo inquadra in galleria e apre la scheda
    public class ArtworkQrController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;


        public ArtworkQrController( ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // GET: ArtworkQr/Scan/5
        // Pagina aperta dal QR: registra la scansione e porta alla scheda dell'opera

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Scan( int id)
        {
            var artworkExists = await _context.Artworks.AsNoTracking().AnyAsync(a => a.Id == id);

            if (!artworkExists)
            {
                return NotFound();
            }

            // Scrive un valore in sessione per avere un id di sessione stabile

            HttpContext.Session.SetString( "BeArtGalleryQr", "active");

            var sessionId = HttpContext.Session.Id;

            var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);

            // Non conta di nuovo la stessa scansione nella stessa sessione
            // negli ultimi 30 minuti

            var alreadyScanned = await _context.ArtworkQrScans.AsNoTracking().AnyAsync(s =>
                        s.ArtworkId == id &&  s.SessionId == sessionId && s.ScannedAt >= thirtyMinutesAgo);

            if (!alreadyScanned)
            {
                string? userId =  null;

                if (User.Identity?.IsAuthenticated == true)
                {
                    userId =  _userManager.GetUserId(User);
                }

                // Salva la scansione per le statistiche
                var scan =
                    new ArtworkQrScan
                    {
                        ArtworkId = id,
                        UserId = userId, 
                        SessionId = sessionId, 
                        ScannedAt = DateTime.UtcNow
                    }; 

                _context.ArtworkQrScans.Add(scan);
                 
                await _context.SaveChangesAsync();
            }
             
            return RedirectToAction( "Details", "Artworks",
                new  { id });
        }


        // GET: ArtworkQr/Image/5
        // Immagine del QR dell'opera (solo Staff e Admin)

        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        [HttpGet]
        public async Task<IActionResult> Image( int id)
        {
            var artwork = await _context.Artworks.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (artwork == null)
            {
                return NotFound();
            }

            // Indirizzo che il QR deve aprire
            var scanUrl = Url.Action(
                    action: nameof(Scan),
                    controller: "ArtworkQr",
                    values: new
                    {
                        id = artwork.Id
                    },
                    protocol: Request.Scheme);
 
            if (string.IsNullOrWhiteSpace( scanUrl))
            {
                return BadRequest();
            }  

            // Crea il QR code con la libreria QRCoder
            using var generator = new QRCodeGenerator();
 
            using var qrData = generator.CreateQrCode( scanUrl, QRCodeGenerator.ECCLevel.Q);
             
            var qrCode = new PngByteQRCode( qrData);
             
            var qrBytes = qrCode.GetGraphic( pixelsPerModule: 20);
             
            return File( qrBytes, "image/png");
        }


        // GET: ArtworkQr/Download/5
        // Scarica il QR in alta risoluzione, da stampare

        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        [HttpGet]
        public async Task<IActionResult> Download( int id)
        {
            var artwork =  await _context.Artworks.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
             
            if (artwork == null)
            {
                return NotFound();
            }
 
            var scanUrl = Url.Action(
                    action: nameof(Scan),
                    controller: "ArtworkQr",
                    values: new
                    {
                        id = artwork.Id
                    },
                    protocol: Request.Scheme);


            if (string.IsNullOrWhiteSpace( scanUrl))
            {
                return BadRequest();
            }
             
            using var generator = new QRCodeGenerator();
             
            using var qrData = generator.CreateQrCode( scanUrl, QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(qrData);

            var qrBytes = qrCode.GetGraphic( pixelsPerModule: 30);

            // Nome del file senza caratteri non validi
            var safeTitle = string.Join("-",  artwork.Title.Split( Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

            var fileName =  $"QR-{safeTitle}-{artwork.Id}.png";

            return File( qrBytes, "image/png", fileName);
        }
    }
}