using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    // Messaggio di una conversazione su una richiesta di informazioni
    public class ArtworkInquiryMessage
    {
        public int Id { get; set; }

        // Richiesta a cui appartiene
        [Required]
        public int ArtworkInquiryId { get; set; }

        public ArtworkInquiry? ArtworkInquiry { get; set; }

        // Chi ha scritto il messaggio
        public string? SenderUserId { get; set; }

        public ApplicationUser? SenderUser { get; set; }

        // Visitatore o operatore della galleria
        [Required]
        public ArtworkInquiryMessageSenderType SenderType { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Quando il destinatario l'ha letto (null se non ancora letto)
        public DateTime? ReadAt { get; set; }
    }
}