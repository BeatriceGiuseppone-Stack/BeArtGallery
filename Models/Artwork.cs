using BeArtGallery.Models.Enums;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Models
{
    // Opera d'arte del catalogo
    public class Artwork
    {
        public int Id { get; set; }


        // Titolo obbligatorio
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // Anno, tecnica e descrizione
        public int? Year { get; set; }

        [MaxLength(150)]
        public string? Technique { get; set; }

        public string? Description { get; set; }

        // Percorso dell'immagine salvata in wwwroot/images/artworks
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        // Indica se l'opera è esposta in galleria
        public bool IsOnDisplay { get; set; } = true;

        // Dimensioni reali dell'opera in centimetri (servono per l'anteprima nella stanza)

        [Precision(8, 2)]
        [Range( 0.1, 10000, ErrorMessage = "La larghezza deve essere maggiore di 0.")]
        public decimal? WidthCm { get; set; }


        [Precision(8, 2)]
        [Range( 0.1, 10000, ErrorMessage = "L'altezza deve essere maggiore di 0.")]
        public decimal? HeightCm { get; set; }


        // Disponibilità dell'opera (disponibile, ultime disponibilità, venduta, non disponibile)
        public ArtworkAvailabilityStatus AvailabilityStatus { get; set; } = ArtworkAvailabilityStatus.Available;


        // Artista che ha creato l'opera
        [Required]
        public int ArtistId { get; set; }

        public Artist? Artist { get; set; }

        // Dati collegati all'opera: preferiti, richieste, visualizzazioni, audioguide e scansioni QR
        public ICollection<ArtworkFavorite> Favorites { get; set; } = new List<ArtworkFavorite>();

        public ICollection<ArtworkInquiry> Inquiries { get; set; } = new List<ArtworkInquiry>();

        public ICollection<ArtworkView> Views { get; set; } = new List<ArtworkView>();

        public ICollection<ArtworkAudioGuide> AudioGuides { get; set; } = new List<ArtworkAudioGuide>();

        public ICollection<ArtworkQrScan> QrScans { get; set; } = new List<ArtworkQrScan>();
    }
}