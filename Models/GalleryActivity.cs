using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using System.ComponentModel.DataAnnotations;

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

        public GalleryActivityStatus Status { get; set; } = GalleryActivityStatus.Scheduled;

        public DateTime? CancelledAt { get; set; }

        public string? CancelledByUserId { get; set; }

        public ApplicationUser? CancelledByUser { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }
    }
}