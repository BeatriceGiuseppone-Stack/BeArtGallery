using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    public class ArtworkInquiryMessage
    {
        public int Id { get; set; }


        [Required]
        public int ArtworkInquiryId { get; set; }

        public ArtworkInquiry? ArtworkInquiry { get; set; }


        public string? SenderUserId { get; set; }

        public ApplicationUser? SenderUser { get; set; }


        [Required]
        public ArtworkInquiryMessageSenderType SenderType { get; set; }


        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;


        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;


        public DateTime? ReadAt { get; set; }
    }
}