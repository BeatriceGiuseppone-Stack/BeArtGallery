using System.ComponentModel.DataAnnotations;
using BeArtGallery.Models.Enums;

namespace BeArtGallery.Models
{
    public class GalleryActivity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public ActivityType ActivityType { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        [Range(15, 480)]
        public int DurationMinutes { get; set; }

        [Range(1, 500)]
        public int MaxParticipants { get; set; }

        [MaxLength(150)]
        public string? Location { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Booking> Bookings { get; set; }
            = new List<Booking>();
    }
}