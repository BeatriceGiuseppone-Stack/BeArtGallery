using BeArtGallery.Data;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    /// <summary>
    /// Feedback lasciato da un visitatore dopo aver partecipato
    /// a una visita o a un evento (check-in effettuato).
    /// Ogni prenotazione può ricevere al massimo un feedback.
    /// </summary>
    public class VisitFeedback
    {
        public int Id { get; set; }


        [Required]
        public int BookingId { get; set; }

        public Booking? Booking { get; set; }


        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }


        // Valutazione da 1 a 5 stelle.
        [Range(1, 5)]
        public int Rating { get; set; }


        [MaxLength(1000)]
        public string? Comment { get; set; }


        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}
