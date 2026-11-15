using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using BeArtGallery.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Services
{
    /// <summary>
    /// Calcola gli indicatori della dashboard e dell'API /api/dashboard/stats.
    /// Conteggi, somme e medie li fa il database: al server arrivano solo i risultati.
    /// </summary>
    public interface IDashboardService
    {
        Task<DashboardViewModel> BuildAsync( CancellationToken cancellationToken = default);
    }

    // Implementazione del servizio della dashboard
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        private readonly TimeProvider _timeProvider;

        public DashboardService( ApplicationDbContext context, TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }

        // Prepara tutti i dati della dashboard
        public async Task<DashboardViewModel> BuildAsync(CancellationToken cancellationToken = default)
        {
            var ct = cancellationToken;

            // Ora attuale della galleria (gli orari delle attività sono in ora locale)
            var now = _timeProvider.GetLocalNow().DateTime;

            var model = new DashboardViewModel();

            // Check-in e presenze

            // Prenotazioni confermate
            var confirmedBookings = _context.Bookings.AsNoTracking()
                    .Where(b => b.Status == BookingStatus.Confirmed && b.GalleryActivity != null);

            // Visitatori attesi: persone di tutte le prenotazioni confermate
            model.ExpectedVisitors =  await confirmedBookings.SumAsync(b => b.NumberOfParticipants, ct);

            // Persone entrate davvero (check-in fatto)
            model.CheckedInVisitors = await confirmedBookings.Where(b => b.CheckedInAt != null).SumAsync(b => b.NumberOfParticipants, ct);

            // Persone ancora da arrivare: senza check-in e attività non ancora finita
            model.VisitorsToArrive = await confirmedBookings.Where(b => b.CheckedInAt == null &&
                        b.GalleryActivity!.StartDateTime.AddMinutes(b.GalleryActivity.DurationMinutes) > now)
                    .SumAsync(b => b.NumberOfParticipants, ct);

            // No-show: senza check-in e attività già finita
            model.NoShowVisitors = await confirmedBookings.Where(b => b.CheckedInAt == null && b.GalleryActivity!.StartDateTime
                            .AddMinutes(b.GalleryActivity.DurationMinutes) <= now).SumAsync(b => b.NumberOfParticipants, ct);

            // Tasso di presenza: si calcola solo sulle attività già finite,
            // altrimenti gli eventi futuri abbasserebbero la percentuale
            var concludedBookings = confirmedBookings.Where(b =>  b.GalleryActivity!.StartDateTime
                            .AddMinutes(b.GalleryActivity.DurationMinutes) <= now);

            var expectedInConcluded = await concludedBookings.SumAsync(b => b.NumberOfParticipants, ct);

            var checkedInInConcluded = await concludedBookings.Where(b => b.CheckedInAt != null)
                    .SumAsync(b => b.NumberOfParticipants, ct);

            // Percentuale con un decimale; 0 se non ci sono attività finite
            model.AttendanceRate = expectedInConcluded > 0 ? Math.Round( (double)checkedInInConcluded / expectedInConcluded * 100, 1) : 0;

            // Numeri principali
            model.TotalUsers = await _context.Users.CountAsync(ct);

            model.TotalArtworks = await _context.Artworks.CountAsync(ct);

            model.AvailableArtworks = await _context.Artworks.CountAsync(a => a.AvailabilityStatus == ArtworkAvailabilityStatus.Available, ct);

            model.TotalFavorites = await _context.ArtworkFavorites.CountAsync(ct);

            model.TotalInquiries = await _context.ArtworkInquiries.CountAsync(ct);

            // Richieste aperte: nuove o in lavorazione
            model.OpenInquiries = await _context.ArtworkInquiries.CountAsync(i => i.Status == ArtworkInquiryStatus.New ||
                        i.Status == ArtworkInquiryStatus.InProgress, ct);

            model.ConfirmedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Confirmed, ct);

            // Attività future in programma
            model.UpcomingActivities = await _context.GalleryActivities.CountAsync(a => a.Status == GalleryActivityStatus.Scheduled &&
                        a.StartDateTime >= now, ct);

            // Interesse per le opere
            model.TotalArtworkViews = await _context.ArtworkViews.CountAsync(ct);

            // Percentuale di visualizzazioni che diventano preferiti
            model.ViewToFavoriteRate = model.TotalArtworkViews > 0 ? Math.Round((decimal)model.TotalFavorites / model.TotalArtworkViews * 100,
                        1) : 0;

            // Percentuale di preferiti che diventano richieste
            model.FavoriteToInquiryRate = model.TotalFavorites > 0 ? Math.Round((decimal)model.TotalInquiries / model.TotalFavorites * 100,
                        1) : 0;

            // Le 5 opere più viste
            model.MostViewedArtworks = await _context.Artworks.AsNoTracking().Where(a => a.Views.Any()).OrderByDescending(a => a.Views.Count)
                    .Take(5)
                    .Select(a => new DashboardArtworkStatisticViewModel
                    {
                        ArtworkId = a.Id,
                        Title = a.Title,
                        ArtistName = a.Artist != null ? a.Artist.FirstName + " " + a.Artist.LastName : string.Empty,
                        ImageUrl = a.ImageUrl,
                        Count = a.Views.Count
                    }).ToListAsync(ct);

            // Occupazione delle attività future: posti prenotati su posti totali

            var futureActivities = _context.GalleryActivities.AsNoTracking()
                    .Where(a => a.Status == GalleryActivityStatus.Scheduled && a.StartDateTime >= now);

            var totalCapacity = await futureActivities.SumAsync(a => a.MaxParticipants, ct);

            // Partecipanti delle prenotazioni confermate
            var totalBookedParticipants =  await futureActivities.SelectMany(a => a.Bookings)
                    .Where(b => b.Status == BookingStatus.Confirmed).SumAsync(b => b.NumberOfParticipants, ct);

            model.OccupancyRate = totalCapacity > 0 ? Math.Round((decimal)totalBookedParticipants / totalCapacity * 100,
                        1) : 0;

            // Richieste per stato
            model.NewInquiries = await _context.ArtworkInquiries.CountAsync(i => i.Status == ArtworkInquiryStatus.New, ct);

            model.InProgressInquiries = await _context.ArtworkInquiries.CountAsync(i => i.Status == ArtworkInquiryStatus.InProgress, ct);

            model.ClosedInquiries = await _context.ArtworkInquiries.CountAsync(i => i.Status == ArtworkInquiryStatus.Closed, ct);

            // Prenotazioni degli ultimi 6 mesi

            // I mesi sono quelli dell'ora locale, ma le date sono salvate in UTC:
            // si converte l'inizio del periodo in UTC per il filtro
            // e ogni data in ora locale per contarla nel mese giusto

            var localZone = _timeProvider.LocalTimeZone;

            var firstMonthLocal = new DateTime(now.Year, now.Month, 1).AddMonths(-5);

            var firstMonthUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(firstMonthLocal, DateTimeKind.Unspecified),
                    localZone);

            var bookingDatesUtc = await _context.Bookings.AsNoTracking().Where(b => b.BookingDate >= firstMonthUtc)
                    .Select(b => b.BookingDate).ToListAsync(ct);

            var bookingMonthsLocal = bookingDatesUtc
                    .Select(d => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(d, DateTimeKind.Utc), localZone)).ToList();

            // Un valore per ognuno dei 6 mesi, anche se è zero
            for (var i = 0; i < 6; i++)
            {
                var month = firstMonthLocal.AddMonths(i);

                model.BookingsByMonth.Add(
                    new DashboardMonthlyDataViewModel
                    {
                        Label = month.ToString("MMM yyyy"),
                        Value = bookingMonthsLocal.Count(d => d.Year == month.Year && d.Month == month.Month)
                    });
            }

            // Le 5 opere più aggiunte ai preferiti
            model.MostFavoritedArtworks = await _context.Artworks.AsNoTracking().Where(a => a.Favorites.Any())
                .OrderByDescending(a => a.Favorites.Count) .Take(5)
                    .Select(a => new DashboardArtworkStatisticViewModel
                    {
                        ArtworkId = a.Id,
                        Title = a.Title,
                        ArtistName = a.Artist != null ? a.Artist.FirstName + " " + a.Artist.LastName : string.Empty,
                        ImageUrl = a.ImageUrl,
                        Count = a.Favorites.Count
                    }).ToListAsync(ct);

            // Le 5 opere con più richieste di informazioni

            model.MostRequestedArtworks = await _context.Artworks.AsNoTracking().Where(a => a.Inquiries.Any())
                    .OrderByDescending(a => a.Inquiries.Count).Take(5)
                    .Select(a => new DashboardArtworkStatisticViewModel
                    {
                        ArtworkId = a.Id,
                        Title = a.Title,
                        ArtistName = a.Artist != null ? a.Artist.FirstName + " " + a.Artist.LastName : string.Empty,
                        ImageUrl = a.ImageUrl,
                        Count = a.Inquiries.Count
                    }).ToListAsync(ct);

            // Le prossime 5 attività con i posti prenotati
            model.UpcomingActivitiesList = await futureActivities.OrderBy(a => a.StartDateTime).Take(5)
                    .Select(a => new DashboardActivityViewModel
                    {
                        Id = a.Id,
                        Title = a.Title,
                        StartDateTime = a.StartDateTime,
                        MaxParticipants = a.MaxParticipants,
                        BookedParticipants = a.Bookings.Where(b => b.Status == BookingStatus.Confirmed)
                                .Sum(b => (int?)b.NumberOfParticipants) ?? 0
                    }).ToListAsync(ct);

            // Feedback dei visitatori: numero, media e ultimi commenti
            model.TotalFeedbacks = await _context.VisitFeedbacks.CountAsync(ct);

            model.AverageRating = model.TotalFeedbacks > 0 ? Math.Round(await _context.VisitFeedbacks
                            .AverageAsync(f => (double)f.Rating, ct), 1) : 0;

            model.LatestFeedbacks = await _context.VisitFeedbacks.AsNoTracking()
                    .OrderByDescending(f => f.CreatedAt).Take(5)
                    .Select(f => new DashboardFeedbackViewModel
                    {
                        ActivityTitle = f.Booking != null && f.Booking.GalleryActivity != null ? f.Booking.GalleryActivity.Title 
                        : string.Empty,
                        Rating = f.Rating,
                        Comment = f.Comment,
                        CreatedAt = f.CreatedAt
                    }).ToListAsync(ct);

            // Avvisi automatici: semplici regole con delle soglie, non intelligenza artificiale

            // Visualizzazioni, preferiti e richieste di ogni opera
            var artworkPerformance = await _context.Artworks.AsNoTracking()
                    .Select(a => new
                    {
                        a.Title,
                        Views = a.Views.Count,
                        Favorites = a.Favorites.Count,
                        Inquiries = a.Inquiries.Count
                    }).ToListAsync(ct);

            foreach (var artwork in artworkPerformance)
            {
                // Opera con molti preferiti ma nessuna richiesta
                if (artwork.Favorites >= 3 && artwork.Inquiries == 0)
                {
                    model.Insights.Add(new DashboardInsightViewModel
                    {
                        Type = "opportunity",
                        Title = "Interesse non convertito",
                        Description = $"{artwork.Title} ha {artwork.Favorites} preferiti " +
                            "ma nessuna richiesta di informazioni. " + "Potrebbe essere utile valorizzarla maggiormente " +
                            "o verificare eventuali barriere alla richiesta."
                    });
                }

                // Opera molto vista ma mai aggiunta ai preferiti
                if (artwork.Views >= 5 && artwork.Favorites == 0)
                {
                    model.Insights.Add(new DashboardInsightViewModel
                    {
                        Type = "attention",
                        Title = "Molte visualizzazioni, poco coinvolgimento",
                        Description = $"{artwork.Title} è stata visualizzata {artwork.Views} volte " +
                            "ma non è ancora stata aggiunta ai preferiti. " + "Potrebbe essere utile rivedere la presentazione dell'opera."
                    });
                }
            }

            // Ci sono richieste da gestire
            if (model.OpenInquiries > 0)
            {
                model.Insights.Add(new DashboardInsightViewModel
                {
                    Type = "info",
                    Title = "Richieste da gestire",
                    Description = $"Sono presenti {model.OpenInquiries} " + "richieste informazioni ancora aperte."
                });
            }

            // Media dei feedback sotto 3 (con almeno 3 feedback)
            if (model.TotalFeedbacks >= 3 && model.AverageRating < 3)
            {
                model.Insights.Add(new DashboardInsightViewModel
                {
                    Type = "attention",
                    Title = "Soddisfazione da migliorare",
                    Description = $"La valutazione media delle visite è {model.AverageRating}/5. " + 
                    "Conviene leggere i commenti dei visitatori."
                });
            }

            // Le prossime attività sono quasi piene
            if (model.OccupancyRate >= 80)
            {
                model.Insights.Add(new DashboardInsightViewModel
                {
                    Type = "success",
                    Title = "Elevata partecipazione",
                    Description = "Le prossime attività hanno raggiunto un'occupazione media del " +
                        $"{model.OccupancyRate}%. " + "La domanda per gli eventi è elevata."
                });
            }

            // Al massimo 5 avvisi
            model.Insights = model.Insights.Take(5).ToList();

            return model;
        }
    }
}