using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
{
    // Dati mostrati nella home: le opere esposte, i prossimi
    // appuntamenti e gli artisti della galleria.
    public class HomeViewModel
    {
        public List<HomeArtworkItem> FeaturedArtworks { get; set; } = new();

        public List<HomeActivityItem> UpcomingActivities { get; set; } = new();

        public List<HomeArtistItem> Artists { get; set; } = new();

        public int ArtworksOnDisplayCount { get; set; }

        public int ArtistsCount { get; set; }
    }

    public class HomeArtworkItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ArtistName { get; set; } = string.Empty;

        public int? Year { get; set; }

        public string? Technique { get; set; }

        public string ImageUrl { get; set; } = string.Empty;
    }

    public class HomeActivityItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public ActivityType ActivityType { get; set; }

        public DateTime StartDateTime { get; set; }

        public string? Location { get; set; }

        public int MaxParticipants { get; set; }

        public int BookedParticipants { get; set; }

        public int AvailablePlaces => Math.Max(0, MaxParticipants - BookedParticipants);
    }

    public class HomeArtistItem
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Nationality { get; set; }

        public int ArtworksCount { get; set; }
    }
}
