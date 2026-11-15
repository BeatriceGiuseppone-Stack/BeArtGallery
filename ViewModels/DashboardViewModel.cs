using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
{
    // Tutti i dati mostrati nella dashboard
    public class DashboardViewModel
    {
        // Numeri principali

        public int TotalUsers { get; set; }

        public int TotalArtworks { get; set; }

        public int AvailableArtworks { get; set; }

        public int TotalFavorites { get; set; }

        public int TotalInquiries { get; set; }

        public int OpenInquiries { get; set; }

        public int ConfirmedBookings { get; set; }

        public int UpcomingActivities { get; set; }

        // Percentuali (con un decimale)
        public decimal OccupancyRate { get; set; }

        public int TotalArtworkViews { get; set; }

        public decimal ViewToFavoriteRate { get; set; }

        public decimal FavoriteToInquiryRate { get; set; }

        // Check-in e presenze

        public int ExpectedVisitors { get; set; }

        public int CheckedInVisitors { get; set; }

        public int VisitorsToArrive { get; set; }

        public int NoShowVisitors { get; set; }

        public double AttendanceRate { get; set; }

        // Feedback dei visitatori

        public int TotalFeedbacks { get; set; }

        public double AverageRating { get; set; }

        public List<DashboardFeedbackViewModel> LatestFeedbacks { get; set; } = new();

        // Classifica delle opere più viste
        public List<DashboardArtworkStatisticViewModel> MostViewedArtworks { get; set; } = new();

        // Avvisi automatici
        public List<DashboardInsightViewModel> Insights { get; set; } = new();

        // Dati del grafico delle prenotazioni per mese
        public List<DashboardMonthlyDataViewModel> BookingsByMonth { get; set; } = new();

        // Richieste per stato
        public int NewInquiries { get; set; }

        public int InProgressInquiries { get; set; }

        public int ClosedInquiries { get; set; }


        // Classifiche delle opere più salvate e più richieste
        public List<DashboardArtworkStatisticViewModel> MostFavoritedArtworks { get; set; } = new();

        public List<DashboardArtworkStatisticViewModel> MostRequestedArtworks { get; set; } = new();

        // Prossime attività
        public List<DashboardActivityViewModel> UpcomingActivitiesList { get; set; } = new();
    }

    // Un mese del grafico: etichetta e numero di prenotazioni
    public class DashboardMonthlyDataViewModel
    {
        public string Label { get; set; } = string.Empty;

        public int Value { get; set; }
    }


    // Un'opera di una classifica con il suo conteggio
    public class DashboardArtworkStatisticViewModel
    {
        public int ArtworkId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ArtistName { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public int Count { get; set; }
    }


    // Una delle prossime attività con i posti prenotati
    public class DashboardActivityViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime StartDateTime { get; set; }

        public int MaxParticipants { get; set; }

        public int BookedParticipants { get; set; }

        // Posti ancora liberi
        public int AvailablePlaces { get { return Math.Max( 0, MaxParticipants - BookedParticipants); } }

        // Percentuale di posti occupati
        public decimal OccupancyRate { 
            get 
            {
                if (MaxParticipants <= 0)
                {
                    return 0;
                }

                return Math.Round(  (decimal)BookedParticipants / MaxParticipants * 100, 1);
            }
        }
    }

    // Uno degli ultimi feedback
    public class DashboardFeedbackViewModel
    {
        public string ActivityTitle { get; set; } = string.Empty;

        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    // Un avviso automatico: titolo, descrizione e tipo (per il colore)
    public class DashboardInsightViewModel
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Type { get; set; } = "info";
    }
}