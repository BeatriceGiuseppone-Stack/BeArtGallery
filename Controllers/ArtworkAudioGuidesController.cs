using BeArtGallery.Authorization;
using BeArtGallery.Data;
using BeArtGallery.Models;
using BeArtGallery.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers
{
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public class ArtworkAudioGuidesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;


        private const long MaxAudioSize =
            25 * 1024 * 1024;


        private static readonly string[] AllowedExtensions =
        {
            ".mp3",
            ".m4a",
            ".wav"
        };


        public ArtworkAudioGuidesController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // =================================================
        // CREATE - GET
        // =================================================

        [HttpGet]
        public async Task<IActionResult> Create(
            int artworkId)
        {
            var artwork =
                await _context.Artworks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.Id == artworkId);


            if (artwork == null)
            {
                return NotFound();
            }


            ViewBag.ArtworkTitle =
                artwork.Title;


            return View(
                new ArtworkAudioGuideViewModel
                {
                    ArtworkId =
                        artwork.Id,

                    LanguageCode =
                        "it-IT",

                    Title =
                        $"{artwork.Title} - Audioguida",

                    IsActive =
                        true
                });
        }


        // =================================================
        // CREATE - POST
        // =================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ArtworkAudioGuideViewModel model)
        {
            var artwork =
                await _context.Artworks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.Id == model.ArtworkId);


            if (artwork == null)
            {
                return NotFound();
            }


            ViewBag.ArtworkTitle =
                artwork.Title;


            if (model.AudioFile == null)
            {
                ModelState.AddModelError(
                    nameof(model.AudioFile),
                    "Seleziona un file audio.");
            }
            else
            {
                ValidateAudioFile(
                    model.AudioFile,
                    nameof(model.AudioFile));
            }


            var duplicateLanguage =
                await _context.ArtworkAudioGuides
                    .AsNoTracking()
                    .AnyAsync(a =>
                        a.ArtworkId ==
                            model.ArtworkId &&
                        a.LanguageCode ==
                            model.LanguageCode);


            if (duplicateLanguage)
            {
                ModelState.AddModelError(
                    nameof(model.LanguageCode),
                    "Per questa opera esiste già un'audioguida nella lingua selezionata.");
            }


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            string? audioUrl =
                null;


            try
            {
                audioUrl =
                    await SaveAudioAsync(
                        model.AudioFile!);


                var audioGuide =
                    new ArtworkAudioGuide
                    {
                        ArtworkId =
                            model.ArtworkId,

                        LanguageCode =
                            model.LanguageCode,

                        Title =
                            model.Title.Trim(),

                        AudioUrl =
                            audioUrl,

                        Transcript =
                            string.IsNullOrWhiteSpace(
                                model.Transcript)
                                ? null
                                : model.Transcript.Trim(),

                        IsActive =
                            model.IsActive,

                        CreatedAt =
                            DateTime.UtcNow
                    };


                _context.ArtworkAudioGuides.Add(
                    audioGuide);


                await _context.SaveChangesAsync();


                TempData["AudioGuideSuccess"] =
                    "Audioguida aggiunta correttamente.";


                return RedirectToAction(
                    "Details",
                    "Artworks",
                    new
                    {
                        id = model.ArtworkId
                    });
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(
                    audioUrl))
                {
                    DeleteAudioFile(
                        audioUrl);
                }


                throw;
            }
        }


        // =================================================
        // EDIT - GET
        // =================================================

        [HttpGet]
        public async Task<IActionResult> Edit(
            int id)
        {
            var audioGuide =
                await _context.ArtworkAudioGuides
                    .AsNoTracking()
                    .Include(a => a.Artwork)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id);


            if (audioGuide == null)
            {
                return NotFound();
            }


            ViewBag.ArtworkTitle =
                audioGuide.Artwork?.Title;


            var model =
                new ArtworkAudioGuideViewModel
                {
                    ArtworkId =
                        audioGuide.ArtworkId,

                    LanguageCode =
                        audioGuide.LanguageCode,

                    Title =
                        audioGuide.Title,

                    Transcript =
                        audioGuide.Transcript,

                    IsActive =
                        audioGuide.IsActive
                };


            return View(model);
        }


        // =================================================
        // EDIT - POST
        // =================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ArtworkAudioGuideViewModel model)
        {
            var audioGuide =
                await _context.ArtworkAudioGuides
                    .Include(a => a.Artwork)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id);


            if (audioGuide == null)
            {
                return NotFound();
            }


            ViewBag.ArtworkTitle =
                audioGuide.Artwork?.Title;


            var duplicateLanguage =
                await _context.ArtworkAudioGuides
                    .AsNoTracking()
                    .AnyAsync(a =>
                        a.Id != id &&
                        a.ArtworkId ==
                            audioGuide.ArtworkId &&
                        a.LanguageCode ==
                            model.LanguageCode);


            if (duplicateLanguage)
            {
                ModelState.AddModelError(
                    nameof(model.LanguageCode),
                    "Per questa opera esiste già un'audioguida nella lingua selezionata.");
            }


            if (model.AudioFile != null)
            {
                ValidateAudioFile(
                    model.AudioFile,
                    nameof(model.AudioFile));
            }


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            string? newAudioUrl =
                null;


            var oldAudioUrl =
                audioGuide.AudioUrl;


            try
            {
                if (model.AudioFile != null)
                {
                    newAudioUrl =
                        await SaveAudioAsync(
                            model.AudioFile);

                    audioGuide.AudioUrl =
                        newAudioUrl;
                }


                audioGuide.LanguageCode =
                    model.LanguageCode;

                audioGuide.Title =
                    model.Title.Trim();

                audioGuide.Transcript =
                    string.IsNullOrWhiteSpace(
                        model.Transcript)
                        ? null
                        : model.Transcript.Trim();

                audioGuide.IsActive =
                    model.IsActive;

                audioGuide.UpdatedAt =
                    DateTime.UtcNow;


                await _context.SaveChangesAsync();


                if (newAudioUrl != null &&
                    oldAudioUrl !=
                    newAudioUrl)
                {
                    DeleteAudioFile(
                        oldAudioUrl);
                }


                TempData["AudioGuideSuccess"] =
                    "Audioguida aggiornata correttamente.";


                return RedirectToAction(
                    "Details",
                    "Artworks",
                    new
                    {
                        id =
                            audioGuide.ArtworkId
                    });
            }
            catch
            {
                if (newAudioUrl != null)
                {
                    DeleteAudioFile(
                        newAudioUrl);
                }


                throw;
            }
        }


        // =================================================
        // DELETE - GET
        // =================================================

        [HttpGet]
        public async Task<IActionResult> Delete(
            int id)
        {
            var audioGuide =
                await _context.ArtworkAudioGuides
                    .AsNoTracking()
                    .Include(a => a.Artwork)
                    .FirstOrDefaultAsync(a =>
                        a.Id == id);


            if (audioGuide == null)
            {
                return NotFound();
            }


            return View(audioGuide);
        }


        // =================================================
        // DELETE - POST
        // =================================================

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var audioGuide =
                await _context.ArtworkAudioGuides
                    .FirstOrDefaultAsync(a =>
                        a.Id == id);


            if (audioGuide == null)
            {
                return NotFound();
            }


            var artworkId =
                audioGuide.ArtworkId;


            var audioUrl =
                audioGuide.AudioUrl;


            _context.ArtworkAudioGuides.Remove(
                audioGuide);


            await _context.SaveChangesAsync();


            DeleteAudioFile(
                audioUrl);


            TempData["AudioGuideSuccess"] =
                "Audioguida eliminata correttamente.";


            return RedirectToAction(
                "Details",
                "Artworks",
                new
                {
                    id =
                        artworkId
                });
        }


        // =================================================
        // VALIDAZIONE AUDIO
        // =================================================

        private void ValidateAudioFile(
            IFormFile file,
            string propertyName)
        {
            if (file.Length <= 0)
            {
                ModelState.AddModelError(
                    propertyName,
                    "Il file audio è vuoto.");

                return;
            }


            if (file.Length > MaxAudioSize)
            {
                ModelState.AddModelError(
                    propertyName,
                    "Il file audio non può superare 25 MB.");
            }


            var extension =
                Path.GetExtension(
                    file.FileName);


            if (string.IsNullOrWhiteSpace(
                    extension) ||
                !AllowedExtensions.Contains(
                    extension,
                    StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    propertyName,
                    "Sono consentiti solamente file MP3, M4A o WAV.");
            }
        }


        // =================================================
        // SALVATAGGIO AUDIO
        // =================================================

        private async Task<string> SaveAudioAsync(
            IFormFile file)
        {
            var extension =
                Path.GetExtension(
                    file.FileName)
                .ToLowerInvariant();


            var fileName =
                $"{Guid.NewGuid():N}{extension}";


            var folder =
                Path.Combine(
                    _environment.WebRootPath,
                    "audio",
                    "artworks");


            Directory.CreateDirectory(
                folder);


            var fullPath =
                Path.Combine(
                    folder,
                    fileName);


            await using var stream =
                new FileStream(
                    fullPath,
                    FileMode.Create);


            await file.CopyToAsync(
                stream);


            return
                $"/audio/artworks/{fileName}";
        }


        // =================================================
        // ELIMINAZIONE FILE
        // =================================================

        private void DeleteAudioFile(
            string? audioUrl)
        {
            if (string.IsNullOrWhiteSpace(
                audioUrl))
            {
                return;
            }


            var fileName =
                Path.GetFileName(
                    audioUrl);


            if (string.IsNullOrWhiteSpace(
                fileName))
            {
                return;
            }


            var fullPath =
                Path.Combine(
                    _environment.WebRootPath,
                    "audio",
                    "artworks",
                    fileName);


            if (System.IO.File.Exists(
                fullPath))
            {
                System.IO.File.Delete(
                    fullPath);
            }
        }
    }
}