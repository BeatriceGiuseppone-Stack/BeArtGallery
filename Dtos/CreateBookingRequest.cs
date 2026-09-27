using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Dtos
{
    public class CreateBookingRequest
    {
        [Required]
        public int GalleryActivityId { get; set; }

        [Range(1, 10)]
        public int NumberOfParticipants { get; set; }
    }
}