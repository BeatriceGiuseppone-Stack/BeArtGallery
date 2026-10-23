using BeArtGallery.Models.Enums;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

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


        // Dimensioni reali dell'opera in centimetri

        [Precision(8, 2)]
        [Range(
            0.1,
            10000,
            ErrorMessage = "La larghezza deve essere maggiore di 0.")]
        public decimal? WidthCm { get; set; }


        [Precision(8, 2)]
        [Range(
            0.1,
            10000,
            ErrorMessage = "L'altezza deve essere maggiore di 0.")]
        public decimal? HeightCm { get; set; }


        public ArtworkAvailabilityStatus AvailabilityStatus { get; set; }
            = ArtworkAvailabilityStatus.Available;


        [Required]
        public int ArtistId { get; set; }


        public Artist? Artist { get; set; }

        public ICollection<ArtworkFavorite> Favorites { get; set; } = new List<ArtworkFavorite>();

        public ICollection<ArtworkInquiry> Inquiries { get; set; } = new List<ArtworkInquiry>();

        public ICollection<ArtworkView> Views { get; set; } = new List<ArtworkView>();
    }
}