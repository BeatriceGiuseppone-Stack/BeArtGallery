using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
{
    // Pagina dello staff con le prenotazioni e i filtri
    public class BookingManagementPageViewModel
    {
        public List<BookingManagementViewModel> Bookings { get; set; } = new();

        public List<BookingActivityFilterViewModel> Activities { get; set; } = new();

        // Valori dei filtri scelti
        public int? ActivityId { get; set; }

        public BookingStatus? Status { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? UserEmail { get; set; }
    }


    // Attività da mostrare nel menu del filtro
    public class BookingActivityFilterViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
    }
}