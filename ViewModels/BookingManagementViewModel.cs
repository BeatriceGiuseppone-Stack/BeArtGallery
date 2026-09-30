using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
{
    public class BookingManagementViewModel
    {
        public int BookingId { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        public string ActivityTitle { get; set; } = string.Empty;

        public ActivityType ActivityType { get; set; }

        public DateTime StartDateTime { get; set; }

        public string? Location { get; set; }

        public int NumberOfParticipants { get; set; }

        public DateTime BookingDate { get; set; }

        public BookingStatus Status { get; set; }
    }
}