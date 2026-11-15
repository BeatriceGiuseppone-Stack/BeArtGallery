using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BeArtGallery.ViewModels;
using BeArtGallery.Models.Enums;
using BeArtGallery.Models;
using BeArtGallery.Data;
using BeArtGallery.Authorization;

// Controller del catalogo delle opere: elenco, scheda, preferiti,
// richieste di informazioni e gestione (creazione, modifica, eliminazione)
public class ArtworksController : Controller
{
    // Database, cartelle del sito e utenti
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<ApplicationUser> _userManager;

    // Formati di immagine accettati per le opere
    private static readonly string[] AllowedExtensions =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private const long MaxImageSize = 5 * 1024 * 1024; // 5 MB

    public ArtworksController(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _environment = environment;
        _userManager = userManager;
    }


    // Catalogo

    // GET: Artworks
    // Numero di opere mostrate in ogni pagina del catalogo
    public const int CatalogPageSize = 9;

    public async Task<IActionResult> Index(int page = 1)
    {
        // Conta le opere e calcola quante pagine servono
        var totalCount = await _context.Artworks.CountAsync();

        var totalPages = Math.Max( 1, (int)Math.Ceiling(totalCount / (double)CatalogPageSize));

        // Se la pagina richiesta non esiste si usa la più vicina valida
        page = Math.Clamp(page, 1, totalPages);

        // Prende dal database solo le opere della pagina, in ordine di titolo
        var artworks = await _context.Artworks.AsNoTracking().Include(a => a.Artist).OrderBy(a => a.Title)
            .ThenBy(a => a.Id).Skip((page - 1) * CatalogPageSize).Take(CatalogPageSize).ToListAsync();

        // Dati per la paginazione nella vista
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;
        ViewBag.PageSize = CatalogPageSize;

        return View(artworks);
    }


    // Scheda dell'opera

    // GET: Artworks/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // Prepara i dati della scheda (opera, preferito, opere simili, form)
        var viewModel = await BuildArtworkDetailsViewModelAsync(id.Value);

        if (viewModel == null)
        {
            return NotFound();
        }
        // Registra la visualizzazione per le statistiche
        await TrackArtworkViewAsync(id.Value);

        return View(viewModel);
    }


    // Preferiti

    // GET: Artworks/Favorites
    // Elenco delle opere salvate dall'utente
    [Authorize]
    public async Task<IActionResult> Favorites()
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var favorites = await _context.ArtworkFavorites.AsNoTracking().Where(f => f.UserId == userId)
            .Include(f => f.Artwork).ThenInclude(a => a!.Artist).OrderByDescending(f => f.CreatedAt).ToListAsync();

        return View(favorites);
    }


    // POST: Artworks/ToggleFavorite/5
    // Aggiunge l'opera ai preferiti o la toglie se c'è già
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> ToggleFavorite(int id, bool fromFavorites = false)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        // Controlla che l'opera esista
        var artworkExists = await _context.Artworks.AnyAsync(a => a.Id == id);

        if (!artworkExists)
        {
            return NotFound();
        }

        // Cerca se l'opera è già tra i preferiti dell'utente
        var favorite = await _context.ArtworkFavorites.FirstOrDefaultAsync(f => f.ArtworkId == id && f.UserId == userId);

        if (favorite == null)
        {
            var newFavorite = new ArtworkFavorite
            {
                ArtworkId = id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ArtworkFavorites.Add(newFavorite);
        }
        else
        {
            _context.ArtworkFavorites.Remove(favorite);
        }

        await _context.SaveChangesAsync();

        // Torna alla pagina da cui è partito il clic
        if (fromFavorites)
        {
            return RedirectToAction(nameof(Favorites));
        }

        return RedirectToAction( nameof(Details), new{id});
    }


    // Richieste di informazioni

    // POST: Artworks/RequestInformation
    // Salva la richiesta di informazioni inviata dal visitatore
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestInformation( ArtworkInquiryViewModel model)
    {
        // Legge titolo e artista: vengono salvati anche nella richiesta,
        // così restano leggibili se l'opera viene eliminata
        var artworkInfo = await _context.Artworks.AsNoTracking().Where(a => a.Id == model.ArtworkId)
            .Select(a => new
            {
                a.Title,
                ArtistName = a.Artist != null ? a.Artist.FirstName + " " + a.Artist.LastName : null
            }).FirstOrDefaultAsync();

        if (artworkInfo == null)
        {
            return NotFound();
        }

        // Se il form ha errori si mostra di nuovo la scheda con i messaggi
        if (!ModelState.IsValid)
        {
            var viewModel = await BuildArtworkDetailsViewModelAsync( model.ArtworkId, model);

            if (viewModel == null)
            {
                return NotFound();
            }

            return View( "Details", viewModel);
        }

        // Se l'utente ha fatto l'accesso, la richiesta viene collegata al suo account
        string? userId = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            userId = _userManager.GetUserId(User);
        }

        // Crea la nuova richiesta con stato "Nuova"
        var inquiry = new ArtworkInquiry
        {
            ArtworkId = model.ArtworkId,
            ArtworkTitle = artworkInfo.Title,
            ArtistName = artworkInfo.ArtistName,
            UserId = userId,
            Name = model.Name.Trim(),
            Email = model.Email.Trim(),
            Message = model.Message.Trim(),
            CreatedAt = DateTime.UtcNow,
            Status = ArtworkInquiryStatus.New
        };

        _context.ArtworkInquiries.Add(inquiry);

        await _context.SaveChangesAsync();

        // Messaggio di conferma mostrato nella scheda dell'opera
        TempData["InquirySuccess"] =
            "La tua richiesta è stata inviata correttamente. La galleria ti risponderà all'indirizzo email indicato.";

        return RedirectToAction( nameof(Details), new {id = model.ArtworkId});
    }


    // Creazione di un'opera

    // GET: Artworks/Create
    // Pagina con il form vuoto
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> Create()
    {
        await LoadArtistsAsync();

        return View();
    }


    // POST: Artworks/Create
    // Salva la nuova opera con la sua immagine
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> Create(
    [Bind("Id,Title,Year,Technique,Description,IsOnDisplay,WidthCm,HeightCm,AvailabilityStatus,ArtistId")]
    Artwork artwork, IFormFile? imageFile)
    {
        // L'immagine è obbligatoria
        if (imageFile == null || imageFile.Length == 0)
        {
            ModelState.AddModelError("imageFile", "Seleziona un'immagine dell'opera.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Salva l'immagine nella cartella del sito e poi l'opera nel database
                artwork.ImageUrl = await SaveImageAsync(imageFile!);

                _context.Add(artwork);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            // Immagine troppo grande o formato non valido
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
            }
        }

        // Ricarica l'elenco degli artisti e mostra di nuovo il form
        await LoadArtistsAsync(artwork.ArtistId);

        return View(artwork);
    }


    // Modifica di un'opera

    // GET: Artworks/Edit/5
    // Pagina di modifica con i dati attuali
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var artwork = await _context.Artworks.FindAsync(id);

        if (artwork == null)
        {
            return NotFound();
        }

        await LoadArtistsAsync(artwork.ArtistId);

        return View(artwork);
    }


    // POST: Artworks/Edit/5
    // Salva le modifiche all'opera
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> Edit(int? id,
    [Bind("Id,Title,Year,Technique,Description,IsOnDisplay,WidthCm,HeightCm,AvailabilityStatus,ArtistId")]
    Artwork artwork, IFormFile? imageFile)
    {
        if (id != artwork.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadArtistsAsync(artwork.ArtistId);

            return View(artwork);
        }

        // Carica l'opera dal database e copia i nuovi valori
        var existingArtwork = await _context.Artworks.FindAsync(id);

        if (existingArtwork == null)
        {
            return NotFound();
        }

        existingArtwork.Title = artwork.Title;
        existingArtwork.Year = artwork.Year;
        existingArtwork.Technique = artwork.Technique;
        existingArtwork.Description = artwork.Description;
        existingArtwork.IsOnDisplay = artwork.IsOnDisplay;
        existingArtwork.ArtistId = artwork.ArtistId;
        existingArtwork.AvailabilityStatus = artwork.AvailabilityStatus;
        existingArtwork.WidthCm = artwork.WidthCm;
        existingArtwork.HeightCm = artwork.HeightCm;

        // Se arriva una nuova immagine si sostituisce quella vecchia
        string? newImageUrl = null;
        string? oldImageUrl = existingArtwork.ImageUrl;

        try
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                newImageUrl = await SaveImageAsync(imageFile);

                existingArtwork.ImageUrl = newImageUrl;
            }

            await _context.SaveChangesAsync();

            // La vecchia immagine si cancella solo dopo il salvataggio riuscito
            if (newImageUrl != null)
            {
                DeleteImage(oldImageUrl);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            // In caso di errore si cancella l'immagine appena caricata
            if (newImageUrl != null)
            {
                DeleteImage(newImageUrl);
            }

            ModelState.AddModelError( "imageFile", ex.Message);

            await LoadArtistsAsync(artwork.ArtistId);

            return View(artwork);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ArtworkExists(artwork.Id))
            {
                return NotFound();
            }

            throw;
        }
    }


    // Eliminazione di un'opera

    // Messaggio mostrato quando ci sono richieste ancora aperte
    public const string OpenInquiryDeleteError = "Non si può eliminare l'opera: una richiesta di informazioni " +
        "è ancora in corso, chiudere prima la conversazione.";


    // GET: Artworks/Delete/5
    // Pagina di conferma dell'eliminazione
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var artwork = await _context.Artworks.AsNoTracking().Include(a => a.Artist).FirstOrDefaultAsync(a => a.Id == id);

        if (artwork == null)
        {
            return NotFound();
        }

        // Conta le richieste aperte (che bloccano l'eliminazione)
        // e quelle chiuse (che restano consultabili)
        ViewBag.OpenInquiries = await _context.ArtworkInquiries.AsNoTracking()
            .CountAsync(i => i.ArtworkId == artwork.Id && i.Status != ArtworkInquiryStatus.Closed);

        ViewBag.ClosedInquiries = await _context.ArtworkInquiries.AsNoTracking()
            .CountAsync(i => i.ArtworkId == artwork.Id && i.Status == ArtworkInquiryStatus.Closed);

        return View(artwork);
    }


    // POST: Artworks/Delete/5
    // Elimina l'opera dopo la conferma
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var artwork = await _context.Artworks.FindAsync(id);

        if (artwork == null)
        {
            return NotFound();
        }

        // Richieste di informazioni collegate all'opera
        var inquiries = await _context.ArtworkInquiries.Where(i => i.ArtworkId == artwork.Id).ToListAsync();

        // Se c'è una conversazione ancora aperta l'opera non si può eliminare
        if (inquiries.Any(i => i.Status != ArtworkInquiryStatus.Closed))
        {
            TempData["ErrorMessage"] = OpenInquiryDeleteError;

            return RedirectToAction( nameof(Details), new{id = artwork.Id});
        }

        string? imageUrl = artwork.ImageUrl;

        // Le richieste chiuse non si cancellano: tengono titolo e artista
        // e perdono solo il collegamento all'opera (ArtworkId = null)
        var artistName = await _context.Artists.Where(a => a.Id == artwork.ArtistId).Select(a => a.FirstName + " " + a.LastName)
            .FirstOrDefaultAsync();

        foreach (var inquiry in inquiries)
        {
            inquiry.ArtworkTitle = artwork.Title;
            inquiry.ArtistName = artistName;
            inquiry.ArtworkId = null;
        }

        // Opera e richieste vengono salvate insieme con un solo SaveChanges
        _context.Artworks.Remove(artwork);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"L'opera \"{artwork.Title}\" è stata eliminata.";

        // Cancella anche il file dell'immagine
        DeleteImage(imageUrl);

        return RedirectToAction(nameof(Index));
    }


    // Metodi di supporto

    // Prepara tutti i dati per la scheda dell'opera
    private async Task<ArtworkDetailsViewModel?>
        BuildArtworkDetailsViewModelAsync(int artworkId, ArtworkInquiryViewModel? inquiryForm = null)
    {
        var artwork = await _context.Artworks.AsNoTracking().Include(a => a.Artist)
            .Include(a => a.AudioGuides).FirstOrDefaultAsync(a => a.Id == artworkId);

        if (artwork == null)
        {
            return null;
        }

        // L'opera è tra i preferiti dell'utente?

        var isFavorite = false;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                isFavorite = await _context.ArtworkFavorites.AsNoTracking().AnyAsync(f => f.ArtworkId == artworkId &&
                        f.UserId == userId);
            }
        }


        // Altre opere dello stesso artista (massimo 3)

        var relatedArtworks = await _context.Artworks.AsNoTracking().Where(a => a.ArtistId == artwork.ArtistId &&
                a.Id != artwork.Id).OrderByDescending(a => a.IsOnDisplay).ThenByDescending(a => a.Year)
            .Take(3).ToListAsync();


        // Form per la richiesta di informazioni

        if (inquiryForm == null)
        {
            inquiryForm = new ArtworkInquiryViewModel
            {
                ArtworkId = artwork.Id
            };

            // Se l'utente ha fatto l'accesso, l'email è già compilata
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    inquiryForm.Email = user.Email ?? string.Empty;
                }
            }
        }


        return new ArtworkDetailsViewModel
        {
            Artwork = artwork,
            IsFavorite = isFavorite,
            Inquiry = inquiryForm,
            RelatedArtworks = relatedArtworks
        };
    }


    // Controlla se un'opera esiste
    private bool ArtworkExists(int? id)
    {
        return _context.Artworks.Any(a => a.Id == id);
    }


    // Prepara il menu a tendina con gli artisti per i form
    private async Task LoadArtistsAsync(int? selectedArtistId = null)
    {
        var artists = await _context.Artists.OrderBy(a => a.LastName).ThenBy(a => a.FirstName)
            .Select(a => new
            {
                a.Id,
                FullName = a.FirstName + " " + a.LastName
            }).ToListAsync();

        ViewBag.ArtistId = new SelectList( artists, "Id", "FullName", selectedArtistId);
    }


    // Salva l'immagine caricata in wwwroot/images/artworks
    // e restituisce il percorso da salvare nel database
    private async Task<string> SaveImageAsync(IFormFile imageFile)
    {
        // Controlla dimensione e formato del file
        if (imageFile.Length > MaxImageSize)
        {
            throw new InvalidOperationException( "L'immagine non può superare i 5 MB.");
        }

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException( "Formato non supportato. Usa JPG, JPEG, PNG o WEBP.");
        }

        string folderPath = Path.Combine(_environment.WebRootPath, "images", "artworks");

        Directory.CreateDirectory(folderPath);

        // Nome casuale per evitare file con lo stesso nome
        string fileName = $"{Guid.NewGuid():N}{extension}";

        string filePath = Path.Combine( folderPath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);

        await imageFile.CopyToAsync(stream);

        return $"/images/artworks/{fileName}";
    }


    // Cancella il file di un'immagine non più usata
    private void DeleteImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        string relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

        string fullPath = Path.Combine(_environment.WebRootPath, relativePath);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }

    // Registra una visualizzazione dell'opera per le statistiche della dashboard
    private async Task TrackArtworkViewAsync(int artworkId)
    {
        // Le visite di Staff e Admin non vengono contate

        if (User.IsInRole(RoleNames.Staff) || User.IsInRole(RoleNames.Admin))
        {
            return;
        }

        // Scrive un valore in sessione per avere un id di sessione stabile

        HttpContext.Session.SetString( "BeArtGalleryAnalytics", "active");

        var sessionId = HttpContext.Session.Id;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);

        // Non conta di nuovo la stessa opera vista nella stessa sessione
        // negli ultimi 30 minuti (per esempio quando si ricarica la pagina)

        var alreadyViewed = await _context.ArtworkViews.AsNoTracking().AnyAsync(v => v.ArtworkId == artworkId &&
                    v.SessionId == sessionId && v.ViewedAt >= thirtyMinutesAgo);

        if (alreadyViewed)
        {
            return;
        }

        // Utente collegato, se ha fatto l'accesso
        string? userId = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            userId = _userManager.GetUserId(User);
        }

        // Salva la visualizzazione
        var artworkView = new ArtworkView
            {
                ArtworkId = artworkId,
                UserId = userId,
                SessionId = sessionId,
                ViewedAt = DateTime.UtcNow
            };

        _context.ArtworkViews.Add(artworkView);

        await _context.SaveChangesAsync();
    }
}