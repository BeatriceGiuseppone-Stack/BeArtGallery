using BeArtGallery.Authorization;
using BeArtGallery.Data;
using BeArtGallery.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers
{
    // Controller degli artisti: elenco pubblico, scheda e gestione per lo staff
    public class ArtistsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ArtistsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Artists
        // Elenco pubblico degli artisti

        // Numero di artisti per pagina, come nel catalogo delle opere
        public const int ArtistsPageSize = 9;

        public async Task<IActionResult> Index(int page = 1)
        {
            // Conta gli artisti e calcola il numero di pagine
            var totalCount = await _context.Artists.CountAsync();

            var totalPages = Math.Max( 1, (int)Math.Ceiling(totalCount / (double)ArtistsPageSize));

            // Se la pagina richiesta non esiste si usa la più vicina valida
            page = Math.Clamp(page, 1, totalPages);

            // Artisti della pagina in ordine di cognome, con le loro opere
            var artists = await _context.Artists.AsNoTracking().Include(a => a.Artworks).OrderBy(a => a.LastName)
                .ThenBy(a => a.FirstName).ThenBy(a => a.Id).Skip((page - 1) * ArtistsPageSize).Take(ArtistsPageSize)
                .ToListAsync();

            // Dati per la paginazione nella vista
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = ArtistsPageSize;

            return View(artists);
        }


        // GET: Artists/Details/5
        // Scheda pubblica dell'artista con le sue opere

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var artist = await _context.Artists.AsNoTracking().Include(a => a.Artworks).FirstOrDefaultAsync(a => a.Id == id);

            if (artist == null)
            {
                return NotFound();
            }

            return View(artist);
        }

        // GET: Artists/Create
        // Form per un nuovo artista (solo Staff e Admin)

        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Artists/Create
        // Salva il nuovo artista

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> Create( [Bind( "FirstName," + "LastName," + "Nationality," +
                "BirthDate," + "DeathDate," + "Biography" )] Artist artist)
        {
            ValidateArtistDates(artist);

            if (!ModelState.IsValid)
            {
                return View(artist);
            }

            // Salva nel database
            _context.Artists.Add(artist);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Artista {artist.FirstName} {artist.LastName} creato correttamente.";

            return RedirectToAction(nameof(Index));
        }


        // GET: Artists/Edit/5
        // Form di modifica dell'artista

        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var artist = await _context.Artists.FindAsync(id);

            if (artist == null)
            {
                return NotFound();
            }

            return View(artist);
        }


        // POST: Artists/Edit/5
        // Salva le modifiche all'artista

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> Edit(
            int? id, [Bind( "Id," + "FirstName," + "LastName," + "Nationality," +
                "BirthDate," +  "DeathDate," + "Biography" )] Artist artist)
        {
            if (id == null || id != artist.Id)
            {
                return NotFound();
            }

            var existingArtist = await _context.Artists.FirstOrDefaultAsync(a => a.Id == id);

            if (existingArtist == null)
            {
                return NotFound();
            }


            ValidateArtistDates(artist);


            if (!ModelState.IsValid)
            {
                return View(artist);
            }

            try
            {
                // Copia i nuovi valori sull'artista salvato
                existingArtist.FirstName = artist.FirstName;

                existingArtist.LastName = artist.LastName;

                existingArtist.Nationality = artist.Nationality;

                existingArtist.BirthDate = artist.BirthDate;

                existingArtist.DeathDate = artist.DeathDate;

                existingArtist.Biography = artist.Biography;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ArtistExists(artist.Id))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["SuccessMessage"] = $"Artista {artist.FirstName} {artist.LastName} modificato correttamente.";

            return RedirectToAction(nameof(Index));
        }


        // GET: Artists/Delete/5
        // Pagina di conferma dell'eliminazione

        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var artist = await _context.Artists.AsNoTracking().Include(a => a.Artworks).FirstOrDefaultAsync(a => a.Id == id);

            if (artist == null)
            {
                return NotFound();
            }

            // Un artista con opere nel catalogo non si può eliminare
            if (artist.Artworks.Any())
            {
                TempData["ErrorMessage"] = "Non è possibile eliminare questo artista perché " +
                    "sono presenti opere associate nel catalogo.";

                return RedirectToAction(nameof(Index));
            }

            return View(artist);
        }


        // POST: Artists/Delete/5
        // Elimina l'artista dopo la conferma

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var artist = await _context.Artists.Include(a => a.Artworks).FirstOrDefaultAsync(a => a.Id == id);

            if (artist == null)
            {
                return NotFound();
            }

            // Il controllo si ripete anche qui: nascondere il pulsante
            // nella pagina non basta a impedire l'eliminazione

            if (artist.Artworks.Any())
            {
                TempData["ErrorMessage"] = "Non è possibile eliminare questo artista perché " +
                    "sono presenti opere associate nel catalogo.";

                return RedirectToAction(nameof(Index));
            }

            _context.Artists.Remove(artist);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Artista {artist.FirstName} {artist.LastName} eliminato correttamente.";

            return RedirectToAction(nameof(Index));
        }

        // Controlla che la data di morte non sia prima della data di nascita
        private void ValidateArtistDates(Artist artist)
        {
            if (artist.BirthDate.HasValue && artist.DeathDate.HasValue &&
                artist.DeathDate.Value < artist.BirthDate.Value)
            {
                ModelState.AddModelError(nameof(artist.DeathDate), "La data di morte non può precedere la data di nascita.");
            }
        }

        // Controlla se un artista esiste
        private bool ArtistExists(int id)
        {
            return _context.Artists.Any(a => a.Id == id);
        }
    }
}