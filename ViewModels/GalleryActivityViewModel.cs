using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
    {
        public class GalleryActivityViewModel
        {
            public int Id { get; set; }

            public string Title { get; set; } = string.Empty;

            public string? Description { get; set; }

            public ActivityType ActivityType { get; set; }

            public DateTime StartDateTime { get; set; }

            public int DurationMinutes { get; set; }

            public int MaxParticipants { get; set; }

            public string? Location { get; set; }

            public bool IsActive { get; set; }

            public int BookedParticipants { get; set; }

            public int AvailablePlaces =>
                Math.Max(0, MaxParticipants - BookedParticipants);
        }
    }