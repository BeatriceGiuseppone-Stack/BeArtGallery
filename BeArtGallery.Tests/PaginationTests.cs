using BeArtGallery.Controllers;
using BeArtGallery.Models;
using Microsoft.AspNetCore.Mvc;

namespace BeArtGallery.Tests
{
    /// <summary>
    /// Test della paginazione degli artisti (9 per pagina): il database restituisce
    /// solo la pagina richiesta e le pagine non valide vengono corrette
    /// </summary>
    public sealed class PaginationTests : IDisposable
    {
        private readonly TestDatabase _db = new();


        public void Dispose()
        {
            _db.Dispose();
        }


        // Aggiunge il numero di artisti richiesto
        private void AddArtists(int count)
        {
            using var context = _db.CreateContext();

            for (var i = 1; i <= count; i++)
            {
                context.Artists.Add(new Artist
                {
                    FirstName = "Nome",
                    // Cognomi con due cifre (Artista01, Artista02, ...):
                    // così l'ordine alfabetico è uguale a quello numerico
                    LastName = $"Artista{i:D2}"
                });
            }

            context.SaveChanges();
        }


        // Apre una pagina dell'elenco artisti e restituisce artisti e vista
        private async Task<(List<Artist> Artists, ViewResult View)> OpenPage(int page)
        {
            await using var context = _db.CreateContext();
            var controller = new ArtistsController(context);

            var view = Assert.IsType<ViewResult>(await controller.Index(page));
            var artists = Assert.IsAssignableFrom<IEnumerable<Artist>>(view.Model).ToList();

            return (artists, view);
        }


        // Prima pagina: 9 artisti in ordine alfabetico
        [Fact]
        public async Task FirstPage_ShowsNineArtistsInAlphabeticalOrder()
        {
            AddArtists(20);

            var (artists, view) = await OpenPage(1);

            Assert.Equal(ArtistsController.ArtistsPageSize, artists.Count);
            Assert.Equal("Artista01", artists.First().LastName);
            Assert.Equal("Artista09", artists.Last().LastName);
            Assert.Equal(3, view.ViewData["TotalPages"]);
            Assert.Equal(20, view.ViewData["TotalCount"]);
        }


        // Ultima pagina: solo gli artisti rimasti
        [Fact]
        public async Task LastPage_ShowsOnlyRemainingArtists()
        {
            AddArtists(20);

            var (artists, view) = await OpenPage(3);

            Assert.Equal(2, artists.Count);
            Assert.Equal("Artista19", artists[0].LastName);
            Assert.Equal(3, view.ViewData["Page"]);
        }


        // Pagine fuori dai limiti: si va alla prima o all'ultima
        [Theory]
        [InlineData(0, 1)]
        [InlineData(-4, 1)]
        [InlineData(99, 3)]
        public async Task OutOfRangePage_IsClampedToValidPage(int requested, int expected)
        {
            AddArtists(20);

            var (_, view) = await OpenPage(requested);

            Assert.Equal(expected, view.ViewData["Page"]);
        }


        // Nessun artista: una sola pagina vuota
        [Fact]
        public async Task EmptyCatalog_HasOnePageAndNoArtists()
        {
            var (artists, view) = await OpenPage(1);

            Assert.Empty(artists);
            Assert.Equal(1, view.ViewData["TotalPages"]);
        }
    }
}
