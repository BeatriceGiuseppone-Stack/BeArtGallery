using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    // Artista presente nel catalogo della galleria
    public class Artist
    {
        // Chiave primaria
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        // Nome e cognome obbligatori
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        // Nazionalità (facoltativa)
        public string? Nationality { get; set; }

        // Date di nascita e di morte, facoltative
        public DateOnly? BirthDate { get; set; }

        public DateOnly? DeathDate { get; set; }

        // Breve biografia
        public string? Biography { get; set; }

        // Opere dell'artista (relazione uno a molti)
        public ICollection<Artwork> Artworks { get; set; } = new List<Artwork>();
    }
}