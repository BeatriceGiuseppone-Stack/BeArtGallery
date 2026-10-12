using BeArtGallery.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    public class Artwork
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public int? Year { get; set; }

        [MaxLength(150)]
        public string? Technique { get; set; }

        public string? Description { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsOnDisplay { get; set; } = true;

        [Required]
        public int ArtistId { get; set; }

        public Artist? Artist { get; set; }

        public ICollection<ArtworkFavorite> Favorites { get; set; }
    = new List<ArtworkFavorite>();

        public ICollection<ArtworkInquiry> Inquiries { get; set; }
            = new List<ArtworkInquiry>();

        public ArtworkAvailabilityStatus AvailabilityStatus { get; set; }
    = ArtworkAvailabilityStatus.Available;
    }
}