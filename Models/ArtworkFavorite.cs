using BeArtGallery.Data;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    // Opera salvata tra i preferiti da un utente
    public class ArtworkFavorite
    {
        public int Id { get; set; }

        // Opera e utente collegati
        [Required]
        public int ArtworkId { get; set; }

        public Artwork? Artwork { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        // Quando è stata aggiunta
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}