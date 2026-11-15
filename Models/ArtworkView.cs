using BeArtGallery.Data;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    // Visualizzazione della scheda di un'opera, usata per le statistiche
    public class ArtworkView
    {
        public int Id { get; set; }

        // Opera vista
        [Required]
        public int ArtworkId { get; set; }

        public Artwork? Artwork { get; set; }

        // Utente (se ha fatto l'accesso) e sessione del browser
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        // Momento della visualizzazione
        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    }
}