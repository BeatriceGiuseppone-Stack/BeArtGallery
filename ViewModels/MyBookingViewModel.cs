using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
{
    // Una prenotazione nella pagina "Le mie prenotazioni"
    public class MyBookingViewModel
    {
        public int BookingId { get; set; }

        public string ActivityTitle { get; set; } = string.Empty;

        public ActivityType ActivityType { get; set; }

        public DateTime StartDateTime { get; set; }

        public string? Location { get; set; }

        public int NumberOfParticipants { get; set; }

        public DateTime BookingDate { get; set; }

        public BookingStatus Status { get; set; }

        public DateTime? CancelledAt { get; set; }

        public BookingCancellationType? CancellationType { get; set; }

        public string? CancellationReason { get; set; }

        // Si può annullare se è confermata e l'attività non è ancora iniziata
        public bool CanCancel => Status == BookingStatus.Confirmed && StartDateTime > DateTime.Now;

        public string? QrToken { get; set; }

        public DateTime? CheckedInAt { get; set; }

        // Feedback già lasciato e relativo voto
        public bool HasFeedback { get; set; }

        public int? FeedbackRating { get; set; }

        // Il feedback si può lasciare dopo il check-in, una sola volta
        public bool CanLeaveFeedback => Status == BookingStatus.Confirmed && CheckedInAt.HasValue && !HasFeedback;
    }
}