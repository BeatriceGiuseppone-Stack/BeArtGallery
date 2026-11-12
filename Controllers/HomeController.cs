using BeArtGallery.Data;
using BeArtGallery.Models;
using BeArtGallery.Models.Enums;
using BeArtGallery.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BeArtGallery.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // La home mostra contenuti reali presi dal database:
        // le ultime opere esposte, i prossimi appuntamenti e gli artisti.
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            var model = new HomeViewModel
            {
                FeaturedArtworks = await _context.Artworks
                    .AsNoTracking()
                    .Where(a => a.IsOnDisplay
                             && a.ImageUrl != null
                             && a.ImageUrl != "")
                    .OrderByDescending(a => a.Id)
                    .Take(5)
                    .Select(a => new HomeArtworkItem
                    {
                        Id = a.Id,
                        Title = a.Title,
                        ArtistName = a.Artist != null
                            ? a.Artist.FirstName + " " + a.Artist.LastName
                            : string.Empty,
                        Year = a.Year,
                        Technique = a.Technique,
                        ImageUrl = a.ImageUrl!
                    })
                    .ToListAsync(),

                UpcomingActivities = await _context.GalleryActivities
                    .AsNoTracking()
                    .Where(a => a.IsActive
                             && a.Status == GalleryActivityStatus.Scheduled
                             && a.StartDateTime > now)
                    .OrderBy(a => a.StartDateTime)
                    .Take(3)
                    .Select(a => new HomeActivityItem
                    {
                        Id = a.Id,
                        Title = a.Title,
                        ActivityType = a.ActivityType,
                        StartDateTime = a.StartDateTime,
                        Location = a.Location,
                        MaxParticipants = a.MaxParticipants,
                        BookedParticipants = a.Bookings
                            .Where(b => b.Status == BookingStatus.Confirmed)
                            .Sum(b => (int?)b.NumberOfParticipants) ?? 0
                    })
                    .ToListAsync(),

                Artists = await _context.Artists
                    .AsNoTracking()
                    .OrderBy(a => a.LastName)
                    .ThenBy(a => a.FirstName)
                    .Take(8)
                    .Select(a => new HomeArtistItem
                    {
                        Id = a.Id,
                        FullName = a.FirstName + " " + a.LastName,
                        Nationality = a.Nationality,
                        ArtworksCount = a.Artworks.Count
                    })
                    .ToListAsync(),

                ArtworksOnDisplayCount = await _context.Artworks
                    .CountAsync(a => a.IsOnDisplay),

                ArtistsCount = await _context.Artists.CountAsync()
            };

            return View(model);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
