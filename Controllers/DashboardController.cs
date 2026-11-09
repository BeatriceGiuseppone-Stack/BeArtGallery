using BeArtGallery.Authorization;
using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using BeArtGallery.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers
{
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;


        public DashboardController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var now =
                DateTime.Now;


            var sixMonthsAgo =
                new DateTime(
                    now.Year,
                    now.Month,
                    1)
                .AddMonths(-5);

            // =================================================
            // CHECK-IN E PRESENZE
            // =================================================

            // Prenotazioni confermate
            var confirmedBookingsForAttendance =
                await _context.Bookings
                    .AsNoTracking()
                    .Include(b => b.GalleryActivity)
                    .Where(b =>
                        b.Status == BookingStatus.Confirmed &&
                        b.GalleryActivity != null)
                    .ToListAsync();


            // =================================================
            // VISITATORI ATTESI
            // =================================================

            // Tutte le persone associate
            // a prenotazioni confermate.

            var expectedVisitors =
                confirmedBookingsForAttendance
                    .Sum(b => b.NumberOfParticipants);


            // =================================================
            // CHECK-IN EFFETTUATI
            // =================================================

            // Numero di persone effettivamente entrate.

            var checkedInVisitors =
                confirmedBookingsForAttendance
                    .Where(b =>
                        b.CheckedInAt.HasValue)
                    .Sum(b =>
                        b.NumberOfParticipants);


            // =================================================
            // DA ARRIVARE
            // =================================================

            // Consideriamo solamente attività
            // che non sono ancora terminate.

            var visitorsToArrive =
                confirmedBookingsForAttendance
                    .Where(b =>
                        !b.CheckedInAt.HasValue &&
                        b.GalleryActivity != null &&
                        b.GalleryActivity.StartDateTime
                            .AddMinutes(
                                b.GalleryActivity.DurationMinutes)
                            > now)
                    .Sum(b =>
                        b.NumberOfParticipants);


            // =================================================
            // NO-SHOW
            // =================================================

            // Una persona viene considerata no-show
            // solamente dopo la conclusione dell'attività,
            // non semplicemente dopo l'orario di inizio.

            var noShowVisitors =
                confirmedBookingsForAttendance
                    .Where(b =>
                        !b.CheckedInAt.HasValue &&
                        b.GalleryActivity != null &&
                        b.GalleryActivity.StartDateTime
                            .AddMinutes(
                                b.GalleryActivity.DurationMinutes)
                            <= now)
                    .Sum(b =>
                        b.NumberOfParticipants);


            // =================================================
            // TASSO DI PRESENZA
            // =================================================

            // Per il tasso di presenza consideriamo
            // solamente attività già concluse.
            // In questo modo gli eventi futuri non abbassano
            // artificialmente la percentuale.

            // =================================================
            // TASSO DI PRESENZA
            // =================================================

            // Rapporto in tempo reale tra
            // visitatori attesi e visitatori
            // che hanno effettuato il check-in.

            var attendanceRate =
                expectedVisitors > 0
                    ? Math.Round(
                        (double)checkedInVisitors /
                        expectedVisitors * 100,
                        1)
                    : 0;

            var model =
                new DashboardViewModel();

            // =================================================
            // CHECK-IN E PRESENZE
            // =================================================

            model.ExpectedVisitors = expectedVisitors;

            model.CheckedInVisitors = checkedInVisitors;

            model.VisitorsToArrive = visitorsToArrive;

            model.NoShowVisitors = noShowVisitors;

            model.AttendanceRate = attendanceRate;
             
            // =================================================
            // KPI PRINCIPALI
            // =================================================

            model.TotalUsers =
                await _context.Users
                    .AsNoTracking()
                    .CountAsync();


            model.TotalArtworks =
                await _context.Artworks
                    .AsNoTracking()
                    .CountAsync();


            model.AvailableArtworks =
                await _context.Artworks
                    .AsNoTracking()
                    .CountAsync(a =>
                        a.AvailabilityStatus ==
                        ArtworkAvailabilityStatus.Available);


            model.TotalFavorites =
                await _context.ArtworkFavorites
                    .AsNoTracking()
                    .CountAsync();


            model.TotalInquiries =
                await _context.ArtworkInquiries
                    .AsNoTracking()
                    .CountAsync();


            model.OpenInquiries =
                await _context.ArtworkInquiries
                    .AsNoTracking()
                    .CountAsync(i =>
                        i.Status ==
                            ArtworkInquiryStatus.New ||
                        i.Status ==
                            ArtworkInquiryStatus.InProgress);


            model.ConfirmedBookings =
                await _context.Bookings
                    .AsNoTracking()
                    .CountAsync(b =>
                        b.Status ==
                        BookingStatus.Confirmed);


            model.UpcomingActivities =
                await _context.GalleryActivities
                    .AsNoTracking()
                    .CountAsync(a =>
                        a.Status ==
                            GalleryActivityStatus.Scheduled &&
                        a.StartDateTime >= now);

            // =================================================
            // ANALISI INTERESSE OPERE
            // =================================================

            model.TotalArtworkViews =
                await _context.ArtworkViews
                    .AsNoTracking()
                    .CountAsync();


            model.ViewToFavoriteRate =
                model.TotalArtworkViews > 0
                    ? Math.Round(
                        (decimal)model.TotalFavorites /
                        model.TotalArtworkViews *
                        100,
                        1)
                    : 0;


            model.FavoriteToInquiryRate =
                model.TotalFavorites > 0
                    ? Math.Round(
                        (decimal)model.TotalInquiries /
                        model.TotalFavorites *
                        100,
                        1)
                    : 0;

            // =================================================
            // OPERE PIÙ VISUALIZZATE
            // =================================================

            model.MostViewedArtworks =
                await _context.Artworks
                    .AsNoTracking()
                    .Where(a =>
                        a.Views.Any())
                    .OrderByDescending(a =>
                        a.Views.Count)
                    .Take(5)
                    .Select(a =>
                        new DashboardArtworkStatisticViewModel
                        {
                            ArtworkId =
                                a.Id,

                            Title =
                                a.Title,

                            ArtistName =
                                a.Artist != null
                                    ? a.Artist.FirstName +
                                      " " +
                                      a.Artist.LastName
                                    : string.Empty,

                            ImageUrl =
                                a.ImageUrl,

                            Count =
                                a.Views.Count
                        })
                    .ToListAsync();

            // =================================================
            // TASSO OCCUPAZIONE ATTIVITÀ FUTURE
            // =================================================

            var futureActivities =
                await _context.GalleryActivities
                    .AsNoTracking()
                    .Where(a =>
                        a.Status ==
                            GalleryActivityStatus.Scheduled &&
                        a.StartDateTime >= now)
                    .Select(a => new
                    {
                        a.MaxParticipants,

                        BookedParticipants =
                            a.Bookings
                                .Where(b =>
                                    b.Status ==
                                    BookingStatus.Confirmed)
                                .Sum(b =>
                                    (int?)b.NumberOfParticipants)
                            ?? 0
                    })
                    .ToListAsync();


            var totalCapacity =
                futureActivities.Sum(a =>
                    a.MaxParticipants);


            var totalBookedParticipants =
                futureActivities.Sum(a =>
                    a.BookedParticipants);


            model.OccupancyRate =
                totalCapacity > 0
                    ? Math.Round(
                        (decimal)totalBookedParticipants /
                        totalCapacity *
                        100,
                        1)
                    : 0;


            // =================================================
            // STATO RICHIESTE
            // =================================================

            model.NewInquiries =
                await _context.ArtworkInquiries
                    .AsNoTracking()
                    .CountAsync(i =>
                        i.Status ==
                        ArtworkInquiryStatus.New);


            model.InProgressInquiries =
                await _context.ArtworkInquiries
                    .AsNoTracking()
                    .CountAsync(i =>
                        i.Status ==
                        ArtworkInquiryStatus.InProgress);


            model.ClosedInquiries =
                await _context.ArtworkInquiries
                    .AsNoTracking()
                    .CountAsync(i =>
                        i.Status ==
                        ArtworkInquiryStatus.Closed);


            // =================================================
            // PRENOTAZIONI ULTIMI 6 MESI
            // =================================================

            var bookings =
                await _context.Bookings
                    .AsNoTracking()
                    .Where(b =>
                        b.BookingDate >= sixMonthsAgo)
                    .Select(b => new
                    {
                        b.BookingDate
                    })
                    .ToListAsync();


            for (var i = 0; i < 6; i++)
            {
                var month =
                    sixMonthsAgo.AddMonths(i);


                var count =
                    bookings.Count(b =>
                        b.BookingDate.Year ==
                            month.Year &&
                        b.BookingDate.Month ==
                            month.Month);


                model.BookingsByMonth.Add(
                    new DashboardMonthlyDataViewModel
                    {
                        Label =
                            month.ToString("MMM yyyy"),

                        Value =
                            count
                    });
            }


            // =================================================
            // OPERE PIÙ AGGIUNTE AI PREFERITI
            // =================================================

            model.MostFavoritedArtworks =
                await _context.Artworks
                    .AsNoTracking()
                    .Where(a =>
                        a.Favorites.Any())
                    .OrderByDescending(a =>
                        a.Favorites.Count)
                    .Take(5)
                    .Select(a =>
                        new DashboardArtworkStatisticViewModel
                        {
                            ArtworkId =
                                a.Id,

                            Title =
                                a.Title,

                            ArtistName =
                                a.Artist != null
                                    ? a.Artist.FirstName +
                                      " " +
                                      a.Artist.LastName
                                    : string.Empty,

                            ImageUrl =
                                a.ImageUrl,

                            Count =
                                a.Favorites.Count
                        })
                    .ToListAsync();


            // =================================================
            // OPERE CON PIÙ RICHIESTE INFORMAZIONI
            // =================================================

            model.MostRequestedArtworks =
                await _context.Artworks
                    .AsNoTracking()
                    .Where(a =>
                        a.Inquiries.Any())
                    .OrderByDescending(a =>
                        a.Inquiries.Count)
                    .Take(5)
                    .Select(a =>
                        new DashboardArtworkStatisticViewModel
                        {
                            ArtworkId =
                                a.Id,

                            Title =
                                a.Title,

                            ArtistName =
                                a.Artist != null
                                    ? a.Artist.FirstName +
                                      " " +
                                      a.Artist.LastName
                                    : string.Empty,

                            ImageUrl =
                                a.ImageUrl,

                            Count =
                                a.Inquiries.Count
                        })
                    .ToListAsync();


            // =================================================
            // PROSSIME ATTIVITÀ
            // =================================================

            model.UpcomingActivitiesList =
                await _context.GalleryActivities
                    .AsNoTracking()
                    .Where(a =>
                        a.Status ==
                            GalleryActivityStatus.Scheduled &&
                        a.StartDateTime >= now)
                    .OrderBy(a =>
                        a.StartDateTime)
                    .Take(5)
                    .Select(a =>
                        new DashboardActivityViewModel
                        {
                            Id =
                                a.Id,

                            Title =
                                a.Title,

                            StartDateTime =
                                a.StartDateTime,

                            MaxParticipants =
                                a.MaxParticipants,

                            BookedParticipants =
                                a.Bookings
                                    .Where(b =>
                                        b.Status ==
                                        BookingStatus.Confirmed)
                                    .Sum(b =>
                                        (int?)b.NumberOfParticipants)
                                ?? 0
                        })
                    .ToListAsync();

            // =================================================
            // INSIGHT AUTOMATICI
            // =================================================

            var artworkPerformance =
                await _context.Artworks
                    .AsNoTracking()
                    .Select(a => new
                    {
                        a.Id,
                        a.Title,

                        Views =
                            a.Views.Count,

                        Favorites =
                            a.Favorites.Count,

                        Inquiries =
                            a.Inquiries.Count
                    })
                    .ToListAsync();


            foreach (var artwork in artworkPerformance)
            {
                // Molti preferiti, nessuna richiesta

                if (artwork.Favorites >= 3 &&
                    artwork.Inquiries == 0)
                {
                    model.Insights.Add(
                        new DashboardInsightViewModel
                        {
                            Type =
                                "opportunity",

                            Title =
                                "Interesse non convertito",

                            Description =
                                $"{artwork.Title} ha " +
                                $"{artwork.Favorites} preferiti " +
                                "ma nessuna richiesta di informazioni. " +
                                "Potrebbe essere utile valorizzarla " +
                                "maggiormente o verificare eventuali " +
                                "barriere alla richiesta."
                        });
                }


                // Molte visite ma nessun preferito

                if (artwork.Views >= 5 &&
                    artwork.Favorites == 0)
                {
                    model.Insights.Add(
                        new DashboardInsightViewModel
                        {
                            Type =
                                "attention",

                            Title =
                                "Molte visualizzazioni, poco coinvolgimento",

                            Description =
                                $"{artwork.Title} è stata visualizzata " +
                                $"{artwork.Views} volte ma non è ancora " +
                                "stata aggiunta ai preferiti. " +
                                "Potrebbe essere utile rivedere la " +
                                "presentazione dell'opera."
                        });
                }
            }


            // Richieste da gestire

            if (model.OpenInquiries > 0)
            {
                model.Insights.Add(
                    new DashboardInsightViewModel
                    {
                        Type =
                            "info",

                        Title =
                            "Richieste da gestire",

                        Description =
                            $"Sono presenti {model.OpenInquiries} " +
                            "richieste informazioni ancora aperte."
                    });
            }


            // Alta occupazione eventi

            if (model.OccupancyRate >= 80)
            {
                model.Insights.Add(
                    new DashboardInsightViewModel
                    {
                        Type =
                            "success",

                        Title =
                            "Elevata partecipazione",

                        Description =
                            $"Le prossime attività hanno raggiunto " +
                            $"un'occupazione media del " +
                            $"{model.OccupancyRate}%. " +
                            "La domanda per visite ed eventi è elevata."
                    });
            }


            model.Insights =
                model.Insights
                    .Take(5)
                    .ToList();

            return View(model);
        }
    }
}