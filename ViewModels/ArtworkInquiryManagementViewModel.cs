using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
{
    // Pagina dello staff con l'elenco delle richieste e i contatori per stato
    public class ArtworkInquiryIndexViewModel
    {
        public ArtworkInquiryStatus? StatusFilter { get; set; }

        public int TotalCount { get; set; }

        public int NewCount { get; set; }

        public int InProgressCount { get; set; }

        public int ClosedCount { get; set; }

        public List<ArtworkInquiryListItemViewModel> Inquiries { get; set; } = new();
    }


    // Riga dell'elenco delle richieste
    public class ArtworkInquiryListItemViewModel
    {
        public int Id { get; set; }

        // Opera della richiesta (null se è stata eliminata)
        public int? ArtworkId { get; set; }

        public bool ArtworkDeleted => ArtworkId == null;

        public string ArtworkTitle { get; set; } = string.Empty;

        public string ArtistName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public ArtworkInquiryStatus Status { get; set; }

        // Vero se la richiesta arriva da un utente registrato
        public bool IsRegisteredUser { get; set; }
    }


    // Dettaglio di una richiesta visto dallo staff
    public class ArtworkInquiryDetailsViewModel
    {
        public int Id { get; set; }

        // Opera della richiesta (null se è stata eliminata)
        public int? ArtworkId { get; set; }

        public bool ArtworkDeleted => ArtworkId == null;

        public string ArtworkTitle { get; set; } = string.Empty;

        public string? ArtworkImageUrl { get; set; }

        public string ArtistName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public ArtworkInquiryStatus Status { get; set; }

        public bool IsRegisteredUser { get; set; }

        public List<ArtworkInquiryMessageViewModel> Messages { get; set; } = new();

        public ArtworkInquiryReplyViewModel Reply { get; set; } = new();
    }
}