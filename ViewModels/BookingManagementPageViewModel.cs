using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
{
    public class BookingManagementPageViewModel
    {
        public List<BookingManagementViewModel> Bookings { get; set; }
            = new();

        public List<BookingActivityFilterViewModel> Activities { get; set; }
            = new();

        public int? ActivityId { get; set; }

        public BookingStatus? Status { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? UserEmail { get; set; }
    }


    public class BookingActivityFilterViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
    }
}