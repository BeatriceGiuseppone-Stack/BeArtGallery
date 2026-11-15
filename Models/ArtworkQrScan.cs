using BeArtGallery.Data;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    // Scansione del QR code di un'opera, usata per le statistiche
    public class ArtworkQrScan
    {
        public int Id { get; set; }

        // Opera scansionata
        [Required]
        public int ArtworkId { get; set; }

        public Artwork? Artwork { get; set; }

        // Utente (se ha fatto l'accesso) e sessione del browser
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        [MaxLength(100)]
        public string? SessionId { get; set; }

        // Momento della scansione
        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    }
}