using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    public class ArtworkAudioGuide
    {
        public int Id { get; set; }


        [Required]
        public int ArtworkId { get; set; }

        public Artwork? Artwork { get; set; }


        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = "it-IT";


        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;


        [Required]
        [MaxLength(500)]
        public string AudioUrl { get; set; } = string.Empty;


        [MaxLength(5000)]
        public string? Transcript { get; set; }


        public bool IsActive { get; set; } = true;


        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;


        public DateTime? UpdatedAt { get; set; }
    }
}