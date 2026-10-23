using BeArtGallery.Data;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    public class ArtworkQrScan
    {
        public int Id { get; set; }


        [Required]
        public int ArtworkId { get; set; }

        public Artwork? Artwork { get; set; }


        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }


        [MaxLength(100)]
        public string? SessionId { get; set; }


        public DateTime ScannedAt { get; set; }
            = DateTime.UtcNow;
    }
}