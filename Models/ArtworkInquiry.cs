using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Models
{
    // Richiesta di informazioni su un'opera inviata da un visitatore
    public class ArtworkInquiry
    {
        public int Id { get; set; }

        // Opera della richiesta: diventa null se l'opera viene eliminata
        public int? ArtworkId { get; set; }

        public Artwork? Artwork { get; set; }

        // Titolo e artista salvati al momento della richiesta,
        // così restano leggibili anche se l'opera viene eliminata
        [MaxLength(200)]
        public string ArtworkTitle { get; set; } = string.Empty;

        [MaxLength(201)]
        public string? ArtistName { get; set; }

        // Utente collegato (null se la richiesta arriva da un visitatore senza account)
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        // Dati di contatto e messaggio del visitatore
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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Stato: nuova, in lavorazione o chiusa
        public ArtworkInquiryStatus Status { get; set; } = ArtworkInquiryStatus.New;

        // Messaggi della conversazione con lo staff
        public ICollection<ArtworkInquiryMessage> Messages { get; set; } = new List<ArtworkInquiryMessage>();
    }
}