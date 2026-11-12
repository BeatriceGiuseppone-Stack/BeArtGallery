using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.ViewModels
{
    /// <summary>
    /// Modulo con cui il visitatore valuta una visita o un evento.
    /// </summary>
    public class VisitFeedbackFormViewModel
    {
        [Required]
        public int BookingId { get; set; }

        // Dati dell'attività, solo per la visualizzazione
        public string ActivityTitle { get; set; } = string.Empty;

        public DateTime StartDateTime { get; set; }


        [Required(ErrorMessage = "Seleziona una valutazione.")]
        [Range(1, 5, ErrorMessage = "La valutazione deve essere compresa tra 1 e 5.")]
        [Display(Name = "Valutazione")]
        public int? Rating { get; set; }


        [MaxLength(1000, ErrorMessage = "Il commento può contenere al massimo 1000 caratteri.")]
        [Display(Name = "Commento (facoltativo)")]
        public string? Comment { get; set; }
    }


    /// <summary>
    /// Riga dell'elenco feedback consultato dallo staff.
    /// </summary>
    public class VisitFeedbackListItemViewModel
    {
        public int Id { get; set; }

        public string ActivityTitle { get; set; } = string.Empty;

        public DateTime ActivityStartDateTime { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }
    }


    /// <summary>
    /// Pagina di riepilogo dei feedback per lo staff.
    /// </summary>
    public class VisitFeedbackManagementViewModel
    {
        public int TotalFeedbacks { get; set; }

        public double AverageRating { get; set; }

        // Numero di feedback per ciascun voto: indice 0 = 1 stella ... indice 4 = 5 stelle
        public int[] RatingDistribution { get; set; } = new int[5];

        public int? RatingFilter { get; set; }

        public List<VisitFeedbackListItemViewModel> Feedbacks { get; set; } = new();
    }
}
