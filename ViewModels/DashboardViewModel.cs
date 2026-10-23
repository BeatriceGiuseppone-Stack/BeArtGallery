using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
{
    public class DashboardViewModel
    {
        // KPI principali

        public int TotalUsers { get; set; }

        public int TotalArtworks { get; set; }

        public int AvailableArtworks { get; set; }

        public int TotalFavorites { get; set; }

        public int TotalInquiries { get; set; }

        public int OpenInquiries { get; set; }

        public int ConfirmedBookings { get; set; }

        public int UpcomingActivities { get; set; }

        public decimal OccupancyRate { get; set; }

        public int TotalArtworkViews { get; set; }

        public decimal ViewToFavoriteRate { get; set; }

        public decimal FavoriteToInquiryRate { get; set; }


        public List<DashboardArtworkStatisticViewModel> MostViewedArtworks
        {
            get;
            set;
        } = new();


        public List<DashboardInsightViewModel> Insights
        {
            get;
            set;
        } = new();

        // Grafico prenotazioni

        public List<DashboardMonthlyDataViewModel> BookingsByMonth
        {
            get;
            set;
        } = new();


        // Stato richieste

        public int NewInquiries { get; set; }

        public int InProgressInquiries { get; set; }

        public int ClosedInquiries { get; set; }


        // Classifiche opere

        public List<DashboardArtworkStatisticViewModel> MostFavoritedArtworks
        {
            get;
            set;
        } = new();

        public List<DashboardArtworkStatisticViewModel> MostRequestedArtworks
        {
            get;
            set;
        } = new();


        // Prossime attività

        public List<DashboardActivityViewModel> UpcomingActivitiesList
        {
            get;
            set;
        } = new();
    }


    public class DashboardMonthlyDataViewModel
    {
        public string Label { get; set; } = string.Empty;

        public int Value { get; set; }
    }


    public class DashboardArtworkStatisticViewModel
    {
        public int ArtworkId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ArtistName { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public int Count { get; set; }
    }


    public class DashboardActivityViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime StartDateTime { get; set; }

        public int MaxParticipants { get; set; }

        public int BookedParticipants { get; set; }

        public int AvailablePlaces
        {
            get
            {
                return Math.Max(
                    0,
                    MaxParticipants - BookedParticipants);
            }
        }

        public decimal OccupancyRate
        {
            get
            {
                if (MaxParticipants <= 0)
                {
                    return 0;
                }

                return Math.Round(
                    (decimal)BookedParticipants /
                    MaxParticipants *
                    100,
                    1);
            }
        }
    }

    public class DashboardInsightViewModel
    {
        public string Title { get; set; }
            = string.Empty;

        public string Description { get; set; }
            = string.Empty;

        public string Type { get; set; }
            = "info";
    }
}