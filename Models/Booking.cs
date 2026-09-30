using System.ComponentModel.DataAnnotations;
using BeArtGallery.Models.Enums;
using Microsoft.AspNetCore.Identity;
using BeArtGallery.Data;

namespace BeArtGallery.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public int GalleryActivityId { get; set; }

        public GalleryActivity? GalleryActivity { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        [Range(1, 10)]
        public int NumberOfParticipants { get; set; } = 1;

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        public BookingStatus Status { get; set; }
            = BookingStatus.Confirmed;

        public DateTime? CancelledAt { get; set; }

        public string? CancelledByUserId { get; set; }

        public ApplicationUser? CancelledByUser { get; set; }

        public BookingCancellationType? CancellationType { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }
    }
}