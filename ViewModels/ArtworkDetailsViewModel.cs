using BeArtGallery.Models;

namespace BeArtGallery.ViewModels
{
    // Dati della scheda di un'opera
    public class ArtworkDetailsViewModel
    {
        // Opera da mostrare
        public Artwork Artwork { get; set; } = null!;

        // Vero se l'opera è tra i preferiti dell'utente
        public bool IsFavorite { get; set; }

        // Form per la richiesta di informazioni
        public ArtworkInquiryViewModel Inquiry { get; set; } = new();

        // Altre opere dello stesso artista
        public List<Artwork> RelatedArtworks { get; set; } = new();
    }
}