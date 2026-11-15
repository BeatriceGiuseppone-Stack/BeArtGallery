using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    // Audioguida di un'opera, in una lingua
    public class ArtworkAudioGuide
    {
        public int Id { get; set; }

        // Opera a cui appartiene
        [Required]
        public int ArtworkId { get; set; }

        public Artwork? Artwork { get; set; }

        // Lingua dell'audioguida (es. it-IT, en-GB)
        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = "it-IT";

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // Percorso del file audio in wwwroot/audio/artworks
        [Required]
        [MaxLength(500)]
        public string AudioUrl { get; set; } = string.Empty;

        // Testo dell'audioguida, utile anche per chi non può ascoltare
        [MaxLength(5000)]
        public string? Transcript { get; set; }

        // Solo le audioguide attive sono visibili ai visitatori
        public bool IsActive { get; set; } = true;

        // Data di creazione e di ultima modifica
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}