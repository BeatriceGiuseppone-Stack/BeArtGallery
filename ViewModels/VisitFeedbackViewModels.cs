using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.ViewModels
{
    /// <summary>
    /// Form con cui il visitatore valuta una visita o un evento
    /// </summary>
    public class VisitFeedbackFormViewModel
    {
        [Required]
        public int BookingId { get; set; }

        // Dati dell'attività, solo da mostrare
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
    /// Riga dell'elenco dei feedback per lo staff
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
    /// Pagina dei feedback per lo staff
    /// </summary>
    public class VisitFeedbackManagementViewModel
    {
        public int TotalFeedbacks { get; set; }

        public double AverageRating { get; set; }

        // Quanti feedback per ogni voto: posizione 0 = 1 stella, posizione 4 = 5 stelle
        public int[] RatingDistribution { get; set; } = new int[5];

        // Voto scelto nel filtro
        public int? RatingFilter { get; set; }

        public List<VisitFeedbackListItemViewModel> Feedbacks { get; set; } = new();
    }
}