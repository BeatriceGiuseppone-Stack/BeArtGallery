using BeArtGallery.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.ViewModels
{
    public class ArtworkInquiryMessageViewModel
    {
        public int Id { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? ReadAt { get; set; }

        public ArtworkInquiryMessageSenderType SenderType { get; set; }

        public string? SenderEmail { get; set; }
    }


    public class ArtworkInquiryReplyViewModel
    {
        [Required]
        public int InquiryId { get; set; }


        [Required(ErrorMessage = "Scrivi un messaggio.")]
        [StringLength(
            2000,
            MinimumLength = 2,
            ErrorMessage = "Il messaggio deve contenere almeno 2 caratteri.")]
        public string Message { get; set; } = string.Empty;
    }


    public class MyArtworkInquiryListItemViewModel
    {
        public int Id { get; set; }

        // Vero se l'opera è stata eliminata dal catalogo
        public bool ArtworkDeleted { get; set; }

        public string ArtworkTitle { get; set; } = string.Empty;

        public string? ArtworkImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastActivity { get; set; }

        public ArtworkInquiryStatus Status { get; set; }

        public int UnreadReplies { get; set; }
    }


    public class MyArtworkInquiryDetailsViewModel
    {
        public int Id { get; set; }

        // Null se l'opera è stata eliminata dal catalogo
        public int? ArtworkId { get; set; }

        public bool ArtworkDeleted => ArtworkId == null;

        public string ArtworkTitle { get; set; } = string.Empty;

        public string? ArtworkImageUrl { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public ArtworkInquiryStatus Status { get; set; }

        public List<ArtworkInquiryMessageViewModel> Messages { get; set; }
            = new();

        public ArtworkInquiryReplyViewModel Reply { get; set; }
            = new();

        public bool CanReply =>
            Status != ArtworkInquiryStatus.Closed;
    }
}