using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
{
    // Dati mostrati allo staff quando scansiona il QR di una prenotazione
    public class BookingCheckInViewModel
    {
        // Falso se il QR non è valido; in quel caso c'è il messaggio di errore
        public bool IsValid { get; set; }

        public string? ErrorMessage { get; set; }

        public string QrToken { get; set; }  = string.Empty;

        public int BookingId { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        public string ActivityTitle { get; set; } = string.Empty;

        public ActivityType ActivityType { get; set; }

        public DateTime StartDateTime { get; set; }

        public string? Location { get; set; }

        public int NumberOfParticipants { get; set; }

        public BookingStatus Status { get; set; }

        public DateTime? CheckedInAt { get; set; }

        public string? CheckedInByEmail { get; set; }

        // Il check-in si può fare solo su una prenotazione confermata e non ancora usata
        public bool CanCheckIn => IsValid && Status == BookingStatus.Confirmed && !CheckedInAt.HasValue;
    }
}