using BeArtGallery.Models.Enums;

namespace BeArtGallery.ViewModels
{
    public class ArtworkInquiryIndexViewModel
    {
        public ArtworkInquiryStatus? StatusFilter { get; set; }

        public int TotalCount { get; set; }

        public int NewCount { get; set; }

        public int InProgressCount { get; set; }

        public int ClosedCount { get; set; }

        public List<ArtworkInquiryListItemViewModel> Inquiries { get; set; }
            = new();
    }


    public class ArtworkInquiryListItemViewModel
    {
        public int Id { get; set; }

        // Null se l'opera è stata eliminata dal catalogo
        public int? ArtworkId { get; set; }

        public bool ArtworkDeleted => ArtworkId == null;

        public string ArtworkTitle { get; set; } = string.Empty;

        public string ArtistName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public ArtworkInquiryStatus Status { get; set; }

        public bool IsRegisteredUser { get; set; }
    }


    public class ArtworkInquiryDetailsViewModel
    {
        public int Id { get; set; }

        // Null se l'opera è stata eliminata dal catalogo
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


        public List<ArtworkInquiryMessageViewModel> Messages { get; set; }
            = new();


        public ArtworkInquiryReplyViewModel Reply { get; set; }
            = new();
    }
}