using BeArtGallery.Models;

namespace BeArtGallery.ViewModels
{
    public class ArtworkDetailsViewModel
    {
        public Artwork Artwork { get; set; } = null!;

        public bool IsFavorite { get; set; }

        public ArtworkInquiryViewModel Inquiry { get; set; }
            = new();

        public List<Artwork> RelatedArtworks { get; set; }
            = new();
    }
}