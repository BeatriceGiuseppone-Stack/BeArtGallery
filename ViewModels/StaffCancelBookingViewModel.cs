using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.ViewModels
{
    public class StaffCancelBookingViewModel
    {
        public int BookingId { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        public string ActivityTitle { get; set; } = string.Empty;

        public DateTime StartDateTime { get; set; }

        [Required(ErrorMessage = "Inserisci il motivo dell'annullamento.")]
        [MaxLength(
            500,
            ErrorMessage = "La motivazione non può superare 500 caratteri.")]
        [Display(Name = "Motivazione")]
        public string CancellationReason { get; set; } = string.Empty;
    }
}