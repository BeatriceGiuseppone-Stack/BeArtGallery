using BeArtGallery.Authorization;
using BeArtGallery.Data;
using BeArtGallery.Models;
using BeArtGallery.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers
{
    // Audioguide delle opere: aggiunta, modifica ed eliminazione (solo Staff e Admin)
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public class ArtworkAudioGuidesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // Dimensione massima del file audio: 25 MB
        private const long MaxAudioSize = 25 * 1024 * 1024;

        // Formati audio accettati
        private static readonly string[] AllowedExtensions =
        {
            ".mp3",
            ".m4a",
            ".wav"
        };

        public ArtworkAudioGuidesController( ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // GET: ArtworkAudioGuides/Create?artworkId=5
        // Form per una nuova audioguida, già compilato con lingua e titolo

        [HttpGet]
        public async Task<IActionResult> Create(int artworkId)
        {
            var artwork = await _context.Artworks.AsNoTracking().FirstOrDefaultAsync(a => a.Id == artworkId);

            if (artwork == null)
            {
                return NotFound();
            }

            ViewBag.ArtworkTitle = artwork.Title;

            return View(
                new ArtworkAudioGuideViewModel
                {
                    ArtworkId = artwork.Id,
                    LanguageCode = "it-IT",
                    Title = $"{artwork.Title} - Audioguida",
                    IsActive = true
                });
        }

        // POST: ArtworkAudioGuides/Create
        // Salva l'audioguida con il file audio

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ArtworkAudioGuideViewModel model)
        {
            var artwork = await _context.Artworks.AsNoTracking().FirstOrDefaultAsync(a => a.Id == model.ArtworkId);

            if (artwork == null)
            {
                return NotFound();
            }

            ViewBag.ArtworkTitle = artwork.Title;

            // Il file audio è obbligatorio
            if (model.AudioFile == null)
            {
                ModelState.AddModelError(nameof(model.AudioFile), "Seleziona un file audio.");
            }
            else
            {
                ValidateAudioFile(model.AudioFile, nameof(model.AudioFile));
            }

            // Per ogni opera c'è al massimo un'audioguida per lingua
            var duplicateLanguage =  await _context.ArtworkAudioGuides.AsNoTracking()
                    .AnyAsync(a => a.ArtworkId == model.ArtworkId && a.LanguageCode == model.LanguageCode);

            if (duplicateLanguage)
            {
                ModelState.AddModelError(nameof(model.LanguageCode), "Per questa opera esiste già un'audioguida nella lingua selezionata.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? audioUrl = null;

            // Salva il file e poi l'audioguida nel database
            try
            {
                audioUrl = await SaveAudioAsync(model.AudioFile!);

                var audioGuide =
                    new ArtworkAudioGuide
                    {
                        ArtworkId = model.ArtworkId,
                        LanguageCode = model.LanguageCode,
                        Title = model.Title.Trim(),
                        AudioUrl = audioUrl,
                        Transcript = string.IsNullOrWhiteSpace(model.Transcript) ? null
                                : model.Transcript.Trim(),
                        IsActive = model.IsActive,
                        CreatedAt = DateTime.UtcNow
                    };

                _context.ArtworkAudioGuides.Add(audioGuide);

                await _context.SaveChangesAsync();

                TempData["AudioGuideSuccess"] = "Audioguida aggiunta correttamente.";

                return RedirectToAction( "Details", "Artworks",
                    new{ id = model.ArtworkId });

            }
            // Se qualcosa va storto si cancella il file appena salvato
            catch
            {
                if (!string.IsNullOrWhiteSpace(audioUrl))
                {
                    DeleteAudioFile(audioUrl);
                }

                throw;
            }
        }


        // GET: ArtworkAudioGuides/Edit/5
        // Form di modifica dell'audioguida

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var audioGuide = await _context.ArtworkAudioGuides.AsNoTracking().Include(a => a.Artwork)
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (audioGuide == null)
            {
                return NotFound();
            }

            ViewBag.ArtworkTitle = audioGuide.Artwork?.Title;

            var model =
                new ArtworkAudioGuideViewModel
                {
                    ArtworkId = audioGuide.ArtworkId,
                    LanguageCode = audioGuide.LanguageCode,
                    Title = audioGuide.Title,
                    Transcript = audioGuide.Transcript,
                    IsActive = audioGuide.IsActive
                };

            return View(model);
        }


        // POST: ArtworkAudioGuides/Edit/5
        // Salva le modifiche all'audioguida

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ArtworkAudioGuideViewModel model)
        {
            var audioGuide = await _context.ArtworkAudioGuides.Include(a => a.Artwork)
                    .FirstOrDefaultAsync(a =>  a.Id == id);

            if (audioGuide == null)
            {
                return NotFound();
            }

            ViewBag.ArtworkTitle = audioGuide.Artwork?.Title;

            // La lingua non deve essere già usata da un'altra audioguida della stessa opera
            var duplicateLanguage = await _context.ArtworkAudioGuides.AsNoTracking()
                    .AnyAsync(a => a.Id != id && a.ArtworkId == audioGuide.ArtworkId &&
                        a.LanguageCode == model.LanguageCode);

            if (duplicateLanguage)
            {
                ModelState.AddModelError(nameof(model.LanguageCode), "Per questa opera esiste già un'audioguida nella lingua selezionata.");
            }

            // Il nuovo file audio è facoltativo
            if (model.AudioFile != null)
            {
                ValidateAudioFile( model.AudioFile, nameof(model.AudioFile));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? newAudioUrl = null;

            var oldAudioUrl = audioGuide.AudioUrl;

            try
            {
                // Se arriva un nuovo file sostituisce quello vecchio
                if (model.AudioFile != null)
                {
                    newAudioUrl = await SaveAudioAsync(model.AudioFile);

                    audioGuide.AudioUrl = newAudioUrl;
                }

                // Aggiorna gli altri campi
                audioGuide.LanguageCode = model.LanguageCode;

                audioGuide.Title = model.Title.Trim();

                audioGuide.Transcript = string.IsNullOrWhiteSpace(model.Transcript) ? null : model.Transcript.Trim();

                audioGuide.IsActive = model.IsActive;

                audioGuide.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Il vecchio file si cancella solo dopo il salvataggio riuscito
                if (newAudioUrl != null && oldAudioUrl != newAudioUrl)
                {
                    DeleteAudioFile (oldAudioUrl);
                }

                TempData["AudioGuideSuccess"] = "Audioguida aggiornata correttamente.";

                return RedirectToAction(
                    "Details",
                    "Artworks",
                    new { id = audioGuide.ArtworkId });
            }
            catch
            {
                if (newAudioUrl != null)
                {
                    DeleteAudioFile(newAudioUrl);
                }

                throw;
            }
        }


        // GET: ArtworkAudioGuides/Delete/5
        // Pagina di conferma dell'eliminazione

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var audioGuide = await _context.ArtworkAudioGuides.AsNoTracking().Include(a => a.Artwork)
                    .FirstOrDefaultAsync(a => a.Id == id);

            if (audioGuide == null)
            {
                return NotFound();
            }

            return View(audioGuide);
        }


        // POST: ArtworkAudioGuides/Delete/5
        // Elimina l'audioguida e il suo file

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var audioGuide =  await _context.ArtworkAudioGuides.FirstOrDefaultAsync(a => a.Id == id);

            if (audioGuide == null)
            {
                return NotFound();
            }

            var artworkId = audioGuide.ArtworkId;

            var audioUrl = audioGuide.AudioUrl;

            _context.ArtworkAudioGuides.Remove(audioGuide);

            await _context.SaveChangesAsync();

            DeleteAudioFile(audioUrl);

            TempData["AudioGuideSuccess"] = "Audioguida eliminata correttamente.";

            return RedirectToAction("Details", "Artworks", new { id = artworkId });
        }


        // Controlla che il file audio non sia vuoto, troppo grande o di un formato non accettato

        private void ValidateAudioFile(IFormFile file, string propertyName)
        {
            if (file.Length <= 0)
            {
                ModelState.AddModelError(propertyName, "Il file audio è vuoto.");

                return;
            }

            if (file.Length > MaxAudioSize)
            {
                ModelState.AddModelError(propertyName, "Il file audio non può superare 25 MB.");
            }

            var extension = Path.GetExtension( file.FileName);

            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError( propertyName, "Sono consentiti solamente file MP3, M4A o WAV.");
            }
        }

        // Salva il file in wwwroot/audio/artworks con un nome casuale
        // e restituisce il percorso da salvare nel database

        private async Task<string> SaveAudioAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var fileName = $"{Guid.NewGuid():N}{extension}";

            var folder = Path.Combine( _environment.WebRootPath, "audio", "artworks");

            Directory.CreateDirectory(folder);

            var fullPath = Path.Combine(folder, fileName);

            await using var stream = new FileStream( fullPath, FileMode.Create);

            await file.CopyToAsync(stream);

            return $"/audio/artworks/{fileName}";
        }

        // Cancella il file audio dalla cartella del sito

        private void DeleteAudioFile(string? audioUrl)
        {
            if (string.IsNullOrWhiteSpace(audioUrl))
            {
                return;
            }

            var fileName = Path.GetFileName(audioUrl);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var fullPath = Path.Combine(_environment.WebRootPath, "audio", "artworks", fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}