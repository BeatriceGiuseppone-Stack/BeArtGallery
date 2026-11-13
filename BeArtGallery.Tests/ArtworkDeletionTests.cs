using BeArtGallery.Models;
using BeArtGallery.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Tests
{
    /// <summary>
    /// Eliminando un'opera, le richieste di informazioni collegate
    /// restano nel database: perdono solo il collegamento all'opera
    /// e conservano titolo e artista.
    /// </summary>
    public sealed class ArtworkDeletionTests : IDisposable
    {
        private readonly TestDatabase _db = new();


        public void Dispose()
        {
            _db.Dispose();
        }


        [Fact]
        public async Task DeletingArtwork_KeepsInquiriesWithoutLink()
        {
            int artworkId;
            int inquiryId;

            await using (var setup = _db.CreateContext())
            {
                var artist = new Artist { FirstName = "Elena", LastName = "Marchetti" };
                var artwork = new Artwork { Title = "Soglie", Artist = artist };

                setup.Artworks.Add(artwork);
                await setup.SaveChangesAsync();

                var inquiry = new ArtworkInquiry
                {
                    ArtworkId = artwork.Id,
                    ArtworkTitle = artwork.Title,
                    ArtistName = "Elena Marchetti",
                    Name = "Anna",
                    Email = "anna@test.local",
                    Message = "Vorrei informazioni sul prezzo.",
                    Status = ArtworkInquiryStatus.Closed
                };

                setup.ArtworkInquiries.Add(inquiry);
                await setup.SaveChangesAsync();

                artworkId = artwork.Id;
                inquiryId = inquiry.Id;
            }

            // Eliminazione dell'opera senza caricare le richieste:
            // è il database (ON DELETE SET NULL) a staccarle.
            await using (var delete = _db.CreateContext())
            {
                var artwork = await delete.Artworks.SingleAsync(a => a.Id == artworkId);
                delete.Artworks.Remove(artwork);
                await delete.SaveChangesAsync();
            }

            await using var check = _db.CreateContext();

            Assert.False(await check.Artworks.AnyAsync(a => a.Id == artworkId));

            var kept = await check.ArtworkInquiries.SingleAsync(i => i.Id == inquiryId);

            Assert.Null(kept.ArtworkId);
            Assert.Equal("Soglie", kept.ArtworkTitle);
            Assert.Equal("Elena Marchetti", kept.ArtistName);
            Assert.Equal("Vorrei informazioni sul prezzo.", kept.Message);
        }
    }
}
