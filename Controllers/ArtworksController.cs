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
    public async Task<IActionResult> Index()
    {
        var artworks = await _context.Artworks
            .AsNoTracking()
            .Include(a => a.Artist)
            .OrderBy(a => a.Title)
            .ToListAsync();

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
        var artworkExists = await _context.Artworks
            .AnyAsync(a => a.Id == model.ArtworkId);

        if (!artworkExists)
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
        [Bind("Id,Title,Year,Technique,Description,IsOnDisplay,AvailabilityStatus,ArtistId")]
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
       [Bind("Id,Title,Year,Technique,Description,IsOnDisplay,AvailabilityStatus,ArtistId")]
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

        var hasInquiries = await _context.ArtworkInquiries
            .AsNoTracking()
            .AnyAsync(i => i.ArtworkId == artwork.Id);

        ViewBag.HasInquiries = hasInquiries;

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

        var hasInquiries = await _context.ArtworkInquiries
            .AnyAsync(i => i.ArtworkId == artwork.Id);

        if (hasInquiries)
        {
            TempData["ErrorMessage"] =
                "Non è possibile eliminare questa opera perché sono presenti richieste di informazioni associate.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = artwork.Id
                });
        }

        string? imageUrl = artwork.ImageUrl;

        _context.Artworks.Remove(artwork);

        await _context.SaveChangesAsync();

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
}
