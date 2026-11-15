using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.ViewModels
{
    // Dati della pagina per annullare un'attività
    public class CancelActivityViewModel
    {
        public int ActivityId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime StartDateTime { get; set; }

        // Prenotazioni e persone che verranno avvisate dell'annullamento
        public int ConfirmedBookings { get; set; }

        public int ConfirmedParticipants { get; set; }

        [Required(ErrorMessage = "Inserisci il motivo dell'annullamento.")]
        [MaxLength(500)]
        [Display(Name = "Motivazione")]
        public string CancellationReason { get; set; } = string.Empty;
    }
}