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

public class ArtworksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<ApplicationUser> _userManager;

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


    // -------------------------------------------------
    // CATALOGO OPERE
    // -------------------------------------------------

    // GET: Artworks
    // Il catalogo mostra 9 opere per pagina (tre righe da tre).
    public const int CatalogPageSize = 9;

    public async Task<IActionResult> Index(int page = 1)
    {
        var totalCount = await _context.Artworks.CountAsync();

        var totalPages = Math.Max(
            1,
            (int)Math.Ceiling(totalCount / (double)CatalogPageSize));

        // Una pagina fuori intervallo (es. ?page=99) porta all'ultima valida.
        page = Math.Clamp(page, 1, totalPages);

        var artworks = await _context.Artworks
            .AsNoTracking()
            .Include(a => a.Artist)
            .OrderBy(a => a.Title)
            .ThenBy(a => a.Id)
            .Skip((page - 1) * CatalogPageSize)
            .Take(CatalogPageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;
        ViewBag.PageSize = CatalogPageSize;

        return View(artworks);
    }


    // -------------------------------------------------
    // DETTAGLI OPERA
    // -------------------------------------------------

    // GET: Artworks/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var viewModel =
            await BuildArtworkDetailsViewModelAsync(id.Value);

        if (viewModel == null)
        {
            return NotFound();
        }
        await TrackArtworkViewAsync(
        id.Value);
        return View(viewModel);
    }


    // -------------------------------------------------
    // PREFERITI
    // -------------------------------------------------

    // GET: Artworks/Favorites
    [Authorize]
    public async Task<IActionResult> Favorites()
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var favorites = await _context.ArtworkFavorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Include(f => f.Artwork)
                .ThenInclude(a => a!.Artist)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return View(favorites);
    }


    // POST: Artworks/ToggleFavorite/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> ToggleFavorite(
        int id,
        bool fromFavorites = false)
    {
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var artworkExists = await _context.Artworks
            .AnyAsync(a => a.Id == id);

        if (!artworkExists)
        {
            return NotFound();
        }

        var favorite = await _context.ArtworkFavorites
            .FirstOrDefaultAsync(f =>
                f.ArtworkId == id &&
                f.UserId == userId);

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

        if (fromFavorites)
        {
            return RedirectToAction(nameof(Favorites));
        }

        return RedirectToAction(
            nameof(Details),
            new
            {
                id
            });
    }


    // -------------------------------------------------
    // RICHIESTE INFORMAZIONI
    // -------------------------------------------------

    // POST: Artworks/RequestInformation
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestInformation(
        ArtworkInquiryViewModel model)
    {
        // Titolo e artista vengono copiati nella richiesta, così la
        // conversazione resta leggibile anche se l'opera sarà eliminata.
        var artworkInfo = await _context.Artworks
            .AsNoTracking()
            .Where(a => a.Id == model.ArtworkId)
            .Select(a => new
            {
                a.Title,
                ArtistName = a.Artist != null
                    ? a.Artist.FirstName + " " + a.Artist.LastName
                    : null
            })
            .FirstOrDefaultAsync();

        if (artworkInfo == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var viewModel =
                await BuildArtworkDetailsViewModelAsync(
                    model.ArtworkId,
                    model);

            if (viewModel == null)
            {
                return NotFound();
            }

            return View(
                "Details",
                viewModel);
        }

        string? userId = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            userId = _userManager.GetUserId(User);
        }

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

        TempData["InquirySuccess"] =
            "La tua richiesta è stata inviata correttamente. La galleria ti risponderà all'indirizzo email indicato.";

        return RedirectToAction(
            nameof(Details),
            new
            {
                id = model.ArtworkId
            });
    }


    // -------------------------------------------------
    // CREAZIONE OPERA
    // -------------------------------------------------

    // GET: Artworks/Create
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> Create()
    {
        await LoadArtistsAsync();

        return View();
    }


    // POST: Artworks/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> Create(
    [Bind("Id,Title,Year,Technique,Description,IsOnDisplay,WidthCm,HeightCm,AvailabilityStatus,ArtistId")]
    Artwork artwork,
    IFormFile? imageFile)
    {
        if (imageFile == null ||
            imageFile.Length == 0)
        {
            ModelState.AddModelError(
                "imageFile",
                "Seleziona un'immagine dell'opera.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                artwork.ImageUrl =
                    await SaveImageAsync(imageFile!);

                _context.Add(artwork);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    "imageFile",
                    ex.Message);
            }
        }

        await LoadArtistsAsync(artwork.ArtistId);

        return View(artwork);
    }


    // -------------------------------------------------
    // MODIFICA OPERA
    // -------------------------------------------------

    // GET: Artworks/Edit/5
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var artwork = await _context.Artworks
            .FindAsync(id);

        if (artwork == null)
        {
            return NotFound();
        }

        await LoadArtistsAsync(artwork.ArtistId);

        return View(artwork);
    }


    // POST: Artworks/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> Edit(
    int? id,
    [Bind("Id,Title,Year,Technique,Description,IsOnDisplay,WidthCm,HeightCm,AvailabilityStatus,ArtistId")]
    Artwork artwork,
    IFormFile? imageFile)
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

        var existingArtwork = await _context.Artworks
            .FindAsync(id);

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

        string? newImageUrl = null;
        string? oldImageUrl = existingArtwork.ImageUrl;

        try
        {
            if (imageFile != null &&
                imageFile.Length > 0)
            {
                newImageUrl = await SaveImageAsync(imageFile);

                existingArtwork.ImageUrl = newImageUrl;
            }

            await _context.SaveChangesAsync();

            if (newImageUrl != null)
            {
                DeleteImage(oldImageUrl);
            }

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            if (newImageUrl != null)
            {
                DeleteImage(newImageUrl);
            }

            ModelState.AddModelError(
                "imageFile",
                ex.Message);

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


    // -------------------------------------------------
    // ELIMINAZIONE OPERA
    // -------------------------------------------------

    public const string OpenInquiryDeleteError =
        "Non si può eliminare l'opera: una richiesta di informazioni " +
        "è ancora in corso, chiudere prima la conversazione.";


    // GET: Artworks/Delete/5
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var artwork = await _context.Artworks
            .AsNoTracking()
            .Include(a => a.Artist)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (artwork == null)
        {
            return NotFound();
        }

        // Le richieste ancora aperte bloccano l'eliminazione;
        // quelle chiuse vengono eliminate insieme all'opera.
        ViewBag.OpenInquiries = await _context.ArtworkInquiries
            .AsNoTracking()
            .CountAsync(i =>
                i.ArtworkId == artwork.Id &&
                i.Status != ArtworkInquiryStatus.Closed);

        ViewBag.ClosedInquiries = await _context.ArtworkInquiries
            .AsNoTracking()
            .CountAsync(i =>
                i.ArtworkId == artwork.Id &&
                i.Status == ArtworkInquiryStatus.Closed);

        return View(artwork);
    }


    // POST: Artworks/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var artwork = await _context.Artworks
            .FindAsync(id);

        if (artwork == null)
        {
            return NotFound();
        }

        var inquiries = await _context.ArtworkInquiries
            .Where(i => i.ArtworkId == artwork.Id)
            .ToListAsync();

        // Una conversazione ancora in corso non può andare persa:
        // lo staff deve prima chiuderla.
        if (inquiries.Any(i => i.Status != ArtworkInquiryStatus.Closed))
        {
            TempData["ErrorMessage"] = OpenInquiryDeleteError;

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = artwork.Id
                });
        }

        string? imageUrl = artwork.ImageUrl;

        // Le richieste chiuse restano consultabili da visitatori e staff:
        // conservano titolo e artista e perdono solo il collegamento
        // all'opera (ArtworkId diventa null), segno che l'opera non è
        // più disponibile. Tutto avviene in un'unica transazione.
        var artistName = await _context.Artists
            .Where(a => a.Id == artwork.ArtistId)
            .Select(a => a.FirstName + " " + a.LastName)
            .FirstOrDefaultAsync();

        foreach (var inquiry in inquiries)
        {
            inquiry.ArtworkTitle = artwork.Title;
            inquiry.ArtistName = artistName;
            inquiry.ArtworkId = null;
        }

        _context.Artworks.Remove(artwork);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"L'opera \"{artwork.Title}\" è stata eliminata.";

        DeleteImage(imageUrl);

        return RedirectToAction(nameof(Index));
    }


    // -------------------------------------------------
    // METODI PRIVATI
    // -------------------------------------------------

    private async Task<ArtworkDetailsViewModel?>
        BuildArtworkDetailsViewModelAsync(
            int artworkId,
            ArtworkInquiryViewModel? inquiryForm = null)
    {
        var artwork = await _context.Artworks
            .AsNoTracking()
            .Include(a => a.Artist)
            .Include(a => a.AudioGuides)
            .FirstOrDefaultAsync(a => a.Id == artworkId);

        if (artwork == null)
        {
            return null;
        }


        // -------------------------------------------------
        // PREFERITO DELL'UTENTE CORRENTE
        // -------------------------------------------------

        var isFavorite = false;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                isFavorite = await _context.ArtworkFavorites
                    .AsNoTracking()
                    .AnyAsync(f =>
                        f.ArtworkId == artworkId &&
                        f.UserId == userId);
            }
        }


        // -------------------------------------------------
        // ALTRE OPERE DELLO STESSO ARTISTA
        // -------------------------------------------------

        var relatedArtworks = await _context.Artworks
            .AsNoTracking()
            .Where(a =>
                a.ArtistId == artwork.ArtistId &&
                a.Id != artwork.Id)
            .OrderByDescending(a => a.IsOnDisplay)
            .ThenByDescending(a => a.Year)
            .Take(3)
            .ToListAsync();


        // -------------------------------------------------
        // FORM RICHIESTA INFORMAZIONI
        // -------------------------------------------------

        if (inquiryForm == null)
        {
            inquiryForm = new ArtworkInquiryViewModel
            {
                ArtworkId = artwork.Id
            };

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


    private bool ArtworkExists(int? id)
    {
        return _context.Artworks
            .Any(a => a.Id == id);
    }


    private async Task LoadArtistsAsync(
        int? selectedArtistId = null)
    {
        var artists = await _context.Artists
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .Select(a => new
            {
                a.Id,
                FullName = a.FirstName + " " + a.LastName
            })
            .ToListAsync();

        ViewBag.ArtistId = new SelectList(
            artists,
            "Id",
            "FullName",
            selectedArtistId);
    }


    private async Task<string> SaveImageAsync(
        IFormFile imageFile)
    {
        if (imageFile.Length > MaxImageSize)
        {
            throw new InvalidOperationException(
                "L'immagine non può superare i 5 MB.");
        }

        var extension = Path
            .GetExtension(imageFile.FileName)
            .ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                "Formato non supportato. Usa JPG, JPEG, PNG o WEBP.");
        }

        string folderPath = Path.Combine(
            _environment.WebRootPath,
            "images",
            "artworks");

        Directory.CreateDirectory(folderPath);

        string fileName =
            $"{Guid.NewGuid():N}{extension}";

        string filePath = Path.Combine(
            folderPath,
            fileName);

        await using var stream =
            new FileStream(filePath, FileMode.Create);

        await imageFile.CopyToAsync(stream);

        return $"/images/artworks/{fileName}";
    }


    private void DeleteImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        string relativePath = imageUrl
            .TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        string fullPath = Path.Combine(
            _environment.WebRootPath,
            relativePath);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }

    private async Task TrackArtworkViewAsync(
    int artworkId)
    {
        // Le visualizzazioni effettuate da Staff/Admin
        // non devono falsare le statistiche.

        if (User.IsInRole(RoleNames.Staff) ||
            User.IsInRole(RoleNames.Admin))
        {
            return;
        }


        // Forziamo l'utilizzo della sessione.

        HttpContext.Session.SetString(
            "BeArtGalleryAnalytics",
            "active");


        var sessionId =
            HttpContext.Session.Id;


        if (string.IsNullOrWhiteSpace(
            sessionId))
        {
            return;
        }


        var thirtyMinutesAgo =
            DateTime.UtcNow
                .AddMinutes(-30);


        // Evitiamo di registrare refresh ripetuti
        // della stessa opera nella stessa sessione.

        var alreadyViewed =
            await _context.ArtworkViews
                .AsNoTracking()
                .AnyAsync(v =>
                    v.ArtworkId ==
                        artworkId &&
                    v.SessionId ==
                        sessionId &&
                    v.ViewedAt >=
                        thirtyMinutesAgo);


        if (alreadyViewed)
        {
            return;
        }


        string? userId =
            null;


        if (User.Identity?.IsAuthenticated ==
            true)
        {
            userId =
                _userManager.GetUserId(User);
        }


        var artworkView =
            new ArtworkView
            {
                ArtworkId =
                    artworkId,

                UserId =
                    userId,

                SessionId =
                    sessionId,

                ViewedAt =
                    DateTime.UtcNow
            };


        _context.ArtworkViews.Add(
            artworkView);


        await _context.SaveChangesAsync();
    }
}
