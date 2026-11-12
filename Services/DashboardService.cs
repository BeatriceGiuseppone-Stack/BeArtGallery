using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using BeArtGallery.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Services
{
    /// <summary>
    /// Calcola gli indicatori (KPI) mostrati nella dashboard
    /// e restituiti dall'API /api/dashboard/stats.
    /// Le aggregazioni (COUNT, SUM, AVG) sono eseguite dal
    /// database: in memoria arrivano solo i risultati.
    /// </summary>
    public interface IDashboardService
    {
        Task<DashboardViewModel> BuildAsync(
            CancellationToken cancellationToken = default);
    }


    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        private readonly TimeProvider _timeProvider;


        public DashboardService(
            ApplicationDbContext context,
            TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }


        public async Task<DashboardViewModel> BuildAsync(
            CancellationToken cancellationToken = default)
        {
            var ct = cancellationToken;

            // Orari delle attività = ora locale della galleria
            var now = _timeProvider.GetLocalNow().DateTime;

            var model = new DashboardViewModel();


            // =================================================
            // CHECK-IN E PRESENZE
            // =================================================

            var confirmedBookings =
                _context.Bookings
                    .AsNoTracking()
                    .Where(b =>
                        b.Status == BookingStatus.Confirmed &&
                        b.GalleryActivity != null);


            // Visitatori attesi: tutte le persone associate
            // a prenotazioni confermate.
            model.ExpectedVisitors =
                await confirmedBookings
                    .SumAsync(b => b.NumberOfParticipants, ct);


            // Check-in effettuati: persone effettivamente entrate.
            model.CheckedInVisitors =
                await confirmedBookings
                    .Where(b => b.CheckedInAt != null)
                    .SumAsync(b => b.NumberOfParticipants, ct);


            // Da arrivare: senza check-in, attività non ancora terminata.
            model.VisitorsToArrive =
                await confirmedBookings
                    .Where(b =>
                        b.CheckedInAt == null &&
                        b.GalleryActivity!.StartDateTime
                            .AddMinutes(b.GalleryActivity.DurationMinutes) > now)
                    .SumAsync(b => b.NumberOfParticipants, ct);


            // No-show: senza check-in e attività già conclusa
            // (non semplicemente iniziata).
            model.NoShowVisitors =
                await confirmedBookings
                    .Where(b =>
                        b.CheckedInAt == null &&
                        b.GalleryActivity!.StartDateTime
                            .AddMinutes(b.GalleryActivity.DurationMinutes) <= now)
                    .SumAsync(b => b.NumberOfParticipants, ct);


            // Tasso di presenza: calcolato solo sulle attività
            // già concluse, così gli eventi futuri (per i quali
            // nessuno può ancora aver fatto check-in) non abbassano
            // artificialmente la percentuale.
            var concludedBookings =
                confirmedBookings
                    .Where(b =>
                        b.GalleryActivity!.StartDateTime
                            .AddMinutes(b.GalleryActivity.DurationMinutes) <= now);

            var expectedInConcluded =
                await concludedBookings
                    .SumAsync(b => b.NumberOfParticipants, ct);

            var checkedInInConcluded =
                await concludedBookings
                    .Where(b => b.CheckedInAt != null)
                    .SumAsync(b => b.NumberOfParticipants, ct);

            model.AttendanceRate =
                expectedInConcluded > 0
                    ? Math.Round(
                        (double)checkedInInConcluded / expectedInConcluded * 100,
                        1)
                    : 0;


            // =================================================
            // KPI PRINCIPALI
            // =================================================

            model.TotalUsers =
                await _context.Users.CountAsync(ct);

            model.TotalArtworks =
                await _context.Artworks.CountAsync(ct);

            model.AvailableArtworks =
                await _context.Artworks
                    .CountAsync(a =>
                        a.AvailabilityStatus == ArtworkAvailabilityStatus.Available,
                        ct);

            model.TotalFavorites =
                await _context.ArtworkFavorites.CountAsync(ct);

            model.TotalInquiries =
                await _context.ArtworkInquiries.CountAsync(ct);

            model.OpenInquiries =
                await _context.ArtworkInquiries
                    .CountAsync(i =>
                        i.Status == ArtworkInquiryStatus.New ||
                        i.Status == ArtworkInquiryStatus.InProgress,
                        ct);

            model.ConfirmedBookings =
                await _context.Bookings
                    .CountAsync(b => b.Status == BookingStatus.Confirmed, ct);

            model.UpcomingActivities =
                await _context.GalleryActivities
                    .CountAsync(a =>
                        a.Status == GalleryActivityStatus.Scheduled &&
                        a.StartDateTime >= now,
                        ct);


            // =================================================
            // ANALISI INTERESSE OPERE
            // =================================================

            model.TotalArtworkViews =
                await _context.ArtworkViews.CountAsync(ct);

            model.ViewToFavoriteRate =
                model.TotalArtworkViews > 0
                    ? Math.Round(
                        (decimal)model.TotalFavorites / model.TotalArtworkViews * 100,
                        1)
                    : 0;

            model.FavoriteToInquiryRate =
                model.TotalFavorites > 0
                    ? Math.Round(
                        (decimal)model.TotalInquiries / model.TotalFavorites * 100,
                        1)
                    : 0;


            // =================================================
            // OPERE PIÙ VISUALIZZATE
            // =================================================

            model.MostViewedArtworks =
                await _context.Artworks
                    .AsNoTracking()
                    .Where(a => a.Views.Any())
                    .OrderByDescending(a => a.Views.Count)
                    .Take(5)
                    .Select(a => new DashboardArtworkStatisticViewModel
                    {
                        ArtworkId = a.Id,
                        Title = a.Title,
                        ArtistName = a.Artist != null
                            ? a.Artist.FirstName + " " + a.Artist.LastName
                            : string.Empty,
                        ImageUrl = a.ImageUrl,
                        Count = a.Views.Count
                    })
                    .ToListAsync(ct);


            // =================================================
            // TASSO OCCUPAZIONE ATTIVITÀ FUTURE
            // =================================================

            var futureActivities =
                _context.GalleryActivities
                    .AsNoTracking()
                    .Where(a =>
                        a.Status == GalleryActivityStatus.Scheduled &&
                        a.StartDateTime >= now);

            var totalCapacity =
                await futureActivities
                    .SumAsync(a => a.MaxParticipants, ct);

            var totalBookedParticipants =
                await futureActivities
                    .SelectMany(a => a.Bookings)
                    .Where(b => b.Status == BookingStatus.Confirmed)
                    .SumAsync(b => b.NumberOfParticipants, ct);

            model.OccupancyRate =
                totalCapacity > 0
                    ? Math.Round(
                        (decimal)totalBookedParticipants / totalCapacity * 100,
                        1)
                    : 0;


            // =================================================
            // STATO RICHIESTE
            // =================================================

            model.NewInquiries =
                await _context.ArtworkInquiries
                    .CountAsync(i => i.Status == ArtworkInquiryStatus.New, ct);

            model.InProgressInquiries =
                await _context.ArtworkInquiries
                    .CountAsync(i => i.Status == ArtworkInquiryStatus.InProgress, ct);

            model.ClosedInquiries =
                await _context.ArtworkInquiries
                    .CountAsync(i => i.Status == ArtworkInquiryStatus.Closed, ct);


            // =================================================
            // PRENOTAZIONI ULTIMI 6 MESI
            // =================================================

            // I mesi sono quelli del calendario locale; BookingDate
            // è salvata in UTC, quindi convertiamo i confini del
            // periodo in UTC per il filtro e ogni data in ora locale
            // per il raggruppamento.

            var localZone = _timeProvider.LocalTimeZone;

            var firstMonthLocal =
                new DateTime(now.Year, now.Month, 1)
                    .AddMonths(-5);

            var firstMonthUtc =
                TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(firstMonthLocal, DateTimeKind.Unspecified),
                    localZone);

            var bookingDatesUtc =
                await _context.Bookings
                    .AsNoTracking()
                    .Where(b => b.BookingDate >= firstMonthUtc)
                    .Select(b => b.BookingDate)
                    .ToListAsync(ct);

            var bookingMonthsLocal =
                bookingDatesUtc
                    .Select(d =>
                        TimeZoneInfo.ConvertTimeFromUtc(
                            DateTime.SpecifyKind(d, DateTimeKind.Utc),
                            localZone))
                    .ToList();

            for (var i = 0; i < 6; i++)
            {
                var month = firstMonthLocal.AddMonths(i);

                model.BookingsByMonth.Add(
                    new DashboardMonthlyDataViewModel
                    {
                        Label = month.ToString("MMM yyyy"),
                        Value = bookingMonthsLocal.Count(d =>
                            d.Year == month.Year &&
                            d.Month == month.Month)
                    });
            }


            // =================================================
            // OPERE PIÙ AGGIUNTE AI PREFERITI
            // =================================================

            model.MostFavoritedArtworks =
                await _context.Artworks
                    .AsNoTracking()
                    .Where(a => a.Favorites.Any())
                    .OrderByDescending(a => a.Favorites.Count)
                    .Take(5)
                    .Select(a => new DashboardArtworkStatisticViewModel
                    {
                        ArtworkId = a.Id,
                        Title = a.Title,
                        ArtistName = a.Artist != null
                            ? a.Artist.FirstName + " " + a.Artist.LastName
                            : string.Empty,
                        ImageUrl = a.ImageUrl,
                        Count = a.Favorites.Count
                    })
                    .ToListAsync(ct);


            // =================================================
            // OPERE CON PIÙ RICHIESTE INFORMAZIONI
            // =================================================

            model.MostRequestedArtworks =
                await _context.Artworks
                    .AsNoTracking()
                    .Where(a => a.Inquiries.Any())
                    .OrderByDescending(a => a.Inquiries.Count)
                    .Take(5)
                    .Select(a => new DashboardArtworkStatisticViewModel
                    {
                        ArtworkId = a.Id,
                        Title = a.Title,
                        ArtistName = a.Artist != null
                            ? a.Artist.FirstName + " " + a.Artist.LastName
                            : string.Empty,
                        ImageUrl = a.ImageUrl,
                        Count = a.Inquiries.Count
                    })
                    .ToListAsync(ct);


            // =================================================
            // PROSSIME ATTIVITÀ
            // =================================================

            model.UpcomingActivitiesList =
                await futureActivities
                    .OrderBy(a => a.StartDateTime)
                    .Take(5)
                    .Select(a => new DashboardActivityViewModel
                    {
                        Id = a.Id,
                        Title = a.Title,
                        StartDateTime = a.StartDateTime,
                        MaxParticipants = a.MaxParticipants,
                        BookedParticipants =
                            a.Bookings
                                .Where(b => b.Status == BookingStatus.Confirmed)
                                .Sum(b => (int?)b.NumberOfParticipants)
                            ?? 0
                    })
                    .ToListAsync(ct);


            // =================================================
            // FEEDBACK DEI VISITATORI
            // =================================================

            model.TotalFeedbacks =
                await _context.VisitFeedbacks.CountAsync(ct);

            model.AverageRating =
                model.TotalFeedbacks > 0
                    ? Math.Round(
                        await _context.VisitFeedbacks
                            .AverageAsync(f => (double)f.Rating, ct),
                        1)
                    : 0;

            model.LatestFeedbacks =
                await _context.VisitFeedbacks
                    .AsNoTracking()
                    .OrderByDescending(f => f.CreatedAt)
                    .Take(5)
                    .Select(f => new DashboardFeedbackViewModel
                    {
                        ActivityTitle =
                            f.Booking != null && f.Booking.GalleryActivity != null
                                ? f.Booking.GalleryActivity.Title
                                : string.Empty,
                        Rating = f.Rating,
                        Comment = f.Comment,
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync(ct);


            // =================================================
            // INSIGHT AUTOMATICI
            // =================================================

            var artworkPerformance =
                await _context.Artworks
                    .AsNoTracking()
                    .Select(a => new
                    {
                        a.Title,
                        Views = a.Views.Count,
                        Favorites = a.Favorites.Count,
                        Inquiries = a.Inquiries.Count
                    })
                    .ToListAsync(ct);

            foreach (var artwork in artworkPerformance)
            {
                // Molti preferiti, nessuna richiesta
                if (artwork.Favorites >= 3 &&
                    artwork.Inquiries == 0)
                {
                    model.Insights.Add(new DashboardInsightViewModel
                    {
                        Type = "opportunity",
                        Title = "Interesse non convertito",
                        Description =
                            $"{artwork.Title} ha {artwork.Favorites} preferiti " +
                            "ma nessuna richiesta di informazioni. " +
                            "Potrebbe essere utile valorizzarla maggiormente " +
                            "o verificare eventuali barriere alla richiesta."
                    });
                }

                // Molte visite ma nessun preferito
                if (artwork.Views >= 5 &&
                    artwork.Favorites == 0)
                {
                    model.Insights.Add(new DashboardInsightViewModel
                    {
                        Type = "attention",
                        Title = "Molte visualizzazioni, poco coinvolgimento",
                        Description =
                            $"{artwork.Title} è stata visualizzata {artwork.Views} volte " +
                            "ma non è ancora stata aggiunta ai preferiti. " +
                            "Potrebbe essere utile rivedere la presentazione dell'opera."
                    });
                }
            }

            // Richieste da gestire
            if (model.OpenInquiries > 0)
            {
                model.Insights.Add(new DashboardInsightViewModel
                {
                    Type = "info",
                    Title = "Richieste da gestire",
                    Description =
                        $"Sono presenti {model.OpenInquiries} " +
                        "richieste informazioni ancora aperte."
                });
            }

            // Soddisfazione dei visitatori bassa
            if (model.TotalFeedbacks >= 3 &&
                model.AverageRating < 3)
            {
                model.Insights.Add(new DashboardInsightViewModel
                {
                    Type = "attention",
                    Title = "Soddisfazione da migliorare",
                    Description =
                        $"La valutazione media delle visite è {model.AverageRating}/5. " +
                        "Conviene leggere i commenti dei visitatori."
                });
            }

            // Alta occupazione eventi
            if (model.OccupancyRate >= 80)
            {
                model.Insights.Add(new DashboardInsightViewModel
                {
                    Type = "success",
                    Title = "Elevata partecipazione",
                    Description =
                        "Le prossime attività hanno raggiunto un'occupazione media del " +
                        $"{model.OccupancyRate}%. " +
                        "La domanda per gli eventi è elevata."
                });
            }

            model.Insights =
                model.Insights
                    .Take(5)
                    .ToList();

            return model;
        }
    }
}
