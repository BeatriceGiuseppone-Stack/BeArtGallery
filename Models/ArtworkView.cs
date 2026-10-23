using BeArtGallery.Data;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    public class ArtworkView
    {
        public int Id { get; set; }


        [Required]
        public int ArtworkId { get; set; }

        public Artwork? Artwork { get; set; }


        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }


        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;


        public DateTime ViewedAt { get; set; }
            = DateTime.UtcNow;
    }
}