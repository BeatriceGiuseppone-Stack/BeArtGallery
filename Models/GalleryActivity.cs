using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    // Evento organizzato dalla galleria
    public class GalleryActivity
    {
        public int Id { get; set; }

        // Titolo e descrizione
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        // Tipo di attività (nel sito si usano solo eventi)
        [Required]
        public ActivityType ActivityType { get; set; }

        // Data e ora di inizio (ora locale della galleria)
        [Required]
        public DateTime StartDateTime { get; set; }

        // Durata in minuti
        [Range(15, 480)]
        public int DurationMinutes { get; set; }

        // Numero massimo di partecipanti
        [Range(1, 500)]
        public int MaxParticipants { get; set; }

        // Luogo dell'attività
        [MaxLength(150)]
        public string? Location { get; set; }

        // Attività attiva o disattivata
        public bool IsActive { get; set; } = true;

        // Prenotazioni dell'attività
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // Stato: programmata o annullata
        public GalleryActivityStatus Status { get; set; } = GalleryActivityStatus.Scheduled;

        // Dati dell'annullamento: quando, chi e motivo
        public DateTime? CancelledAt { get; set; }

        public string? CancelledByUserId { get; set; }

        public ApplicationUser? CancelledByUser { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }
    }
}