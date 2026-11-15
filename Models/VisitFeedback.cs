using BeArtGallery.Data;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    /// <summary>
    /// Feedback di un visitatore dopo aver partecipato a una visita o a un evento.
    /// Ogni prenotazione può avere al massimo un feedback.
    /// </summary>
    public class VisitFeedback
    {
        public int Id { get; set; }

        // Prenotazione a cui si riferisce
        [Required]
        public int BookingId { get; set; }

        public Booking? Booking { get; set; }

        // Utente che ha lasciato il feedback
        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        // Voto da 1 a 5 stelle
        [Range(1, 5)]
        public int Rating { get; set; }

        // Commento facoltativo
        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}