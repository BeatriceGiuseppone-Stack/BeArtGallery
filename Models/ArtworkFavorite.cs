using BeArtGallery.Data;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    public class ArtworkFavorite
    {
        public int Id { get; set; }

        [Required]
        public int ArtworkId { get; set; }

        public Artwork? Artwork { get; set; }


        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }


        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}