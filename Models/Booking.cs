using System.ComponentModel.DataAnnotations;
using BeArtGallery.Models.Enums;
using Microsoft.AspNetCore.Identity;
using BeArtGallery.Data;

namespace BeArtGallery.Models
{
    // Prenotazione di un utente per una visita o un evento
    public class Booking
    {
        public int Id { get; set; }

        // Attività prenotata
        [Required]
        public int GalleryActivityId { get; set; }

        public GalleryActivity? GalleryActivity { get; set; }

        // Utente che ha prenotato
        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        // Numero di persone (da 1 a 10)
        [Range(1, 10)]
        public int NumberOfParticipants { get; set; } = 1;

        // Data della prenotazione (in UTC)
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        // Confermata o annullata
        public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

        // Dati dell'annullamento: quando, chi, tipo e motivo
        public DateTime? CancelledAt { get; set; }

        public string? CancelledByUserId { get; set; }

        public ApplicationUser? CancelledByUser { get; set; }

        public BookingCancellationType? CancellationType { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        // Codice casuale contenuto nel QR della prenotazione
        [MaxLength(64)]
        public string? QrToken { get; set; }

        // Dati del check-in: quando e quale operatore
        public DateTime? CheckedInAt { get; set; }

        public string? CheckedInByUserId { get; set; }

        public ApplicationUser? CheckedInByUser { get; set; }

        // Feedback del visitatore (al massimo uno per prenotazione)
        public VisitFeedback? Feedback { get; set; }
    }
}