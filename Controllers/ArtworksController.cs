using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BeArtGallery.Models;
using BeArtGallery.Data;
using Microsoft.AspNetCore.Authorization;
using BeArtGallery.Authorization;

public class ArtworksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

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
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }


    // GET: Artworks
    public async Task<IActionResult> Index()
    {
        var artworks = await _context.Artworks
            .Include(a => a.Artist)
            .ToListAsync();

        return View(artworks);
    }


    // GET: Artworks/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var artwork = await _context.Artworks
            .Include(a => a.Artist)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (artwork == null)
        {
            return NotFound();
        }

        return View(artwork);
    }


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
        [Bind("Id,Title,Year,Technique,Description,IsOnDisplay,ArtistId")]
        Artwork artwork,
        IFormFile? imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            ModelState.AddModelError(
                "imageFile",
                "Seleziona un'immagine dell'opera.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                artwork.ImageUrl = await SaveImageAsync(imageFile!);

                _context.Add(artwork);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("imageFile", ex.Message);
            }
        }

        await LoadArtistsAsync(artwork.ArtistId);

        return View(artwork);
    }


    // GET: Artworks/Edit/5
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("Id,Title,Year,Technique,Description,IsOnDisplay,ArtistId")]
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

            ModelState.AddModelError("imageFile", ex.Message);

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


    // GET: Artworks/Delete/5
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var artwork = await _context.Artworks
            .Include(a => a.Artist)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (artwork == null)
        {
            return NotFound();
        }

        return View(artwork);
    }


    // POST: Artworks/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var artwork = await _context.Artworks.FindAsync(id);

        if (artwork != null)
        {
            string? imageUrl = artwork.ImageUrl;

            _context.Artworks.Remove(artwork);

            await _context.SaveChangesAsync();

            DeleteImage(imageUrl);
        }

        return RedirectToAction(nameof(Index));
    }


    private bool ArtworkExists(int? id)
    {
        return _context.Artworks.Any(a => a.Id == id);
    }


    private async Task LoadArtistsAsync(int? selectedArtistId = null)
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


    private async Task<string> SaveImageAsync(IFormFile imageFile)
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

        string filePath =
            Path.Combine(folderPath, fileName);

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

        string relativePath =
            imageUrl.TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

        string fullPath =
            Path.Combine(_environment.WebRootPath, relativePath);

        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }
}