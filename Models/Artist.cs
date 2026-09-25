using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    public class Artist
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Nationality { get; set; }

        public DateOnly? BirthDate { get; set; }

        public DateOnly? DeathDate { get; set; }

        public string? Biography { get; set; }

        public ICollection<Artwork> Artworks { get; set; } = new List<Artwork>();
    }
}