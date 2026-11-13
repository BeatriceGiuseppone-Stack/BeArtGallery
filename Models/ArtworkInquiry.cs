using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    public class ArtworkInquiry
    {
        public int Id { get; set; }


        // Null quando l'opera è stata eliminata dal catalogo:
        // la richiesta resta consultabile grazie ai dati copiati sotto.
        public int? ArtworkId { get; set; }

        public Artwork? Artwork { get; set; }


        // Titolo e artista copiati al momento della richiesta,
        // così la conversazione resta leggibile anche se l'opera
        // viene eliminata.
        [MaxLength(200)]
        public string ArtworkTitle { get; set; } = string.Empty;

        [MaxLength(201)]
        public string? ArtistName { get; set; }


        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }


        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;


        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;


        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;


        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;


        public ArtworkInquiryStatus Status { get; set; }
            = ArtworkInquiryStatus.New;

        public ICollection<ArtworkInquiryMessage> Messages { get; set; }
    = new List<ArtworkInquiryMessage>();
    }
}