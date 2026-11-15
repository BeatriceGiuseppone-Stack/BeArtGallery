using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
{
    // Dati di un'attività da mostrare nell'elenco e nella scheda
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

        // Posti già prenotati e posti liberi
        public int BookedParticipants { get; set; }

        public int AvailablePlaces => Math.Max(0, MaxParticipants - BookedParticipants);

        public GalleryActivityStatus Status { get; set; }

        public string? CancellationReason { get; set; }

        public DateTime? CancelledAt { get; set; }

        // Vero se l'attività ha prenotazioni (allora non si può eliminare)
        public bool HasBookings { get; set; }
    }
}