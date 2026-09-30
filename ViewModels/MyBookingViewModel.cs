using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
{
    public class MyBookingViewModel
    {
        public int BookingId { get; set; }

        public string ActivityTitle { get; set; } = string.Empty;

        public ActivityType ActivityType { get; set; }

        public DateTime StartDateTime { get; set; }

        public string? Location { get; set; }

        public int NumberOfParticipants { get; set; }

        public DateTime BookingDate { get; set; }

        public BookingStatus Status { get; set; }

        public DateTime? CancelledAt { get; set; }

        public BookingCancellationType? CancellationType { get; set; }

        public string? CancellationReason { get; set; }

        public bool CanCancel => Status == BookingStatus.Confirmed && StartDateTime > DateTime.Now;
    }
}