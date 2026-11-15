using BeArtGallery.Data;
using BeArtGallery.Dtos;
using BeArtGallery.Models;
using BeArtGallery.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers.Api
{
    /// <summary>
    /// API pubblica per leggere il catalogo delle opere
    /// </summary>
    [ApiController]
    [Route("api/artworks")]
    [AllowAnonymous]
    [Produces("application/json")]
    [Tags("Catalogo opere")]
    public class ArtworksApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ArtworksApiController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        /// <summary>
        /// Elenca le opere del catalogo, con filtri facoltativi
        /// </summary>
        /// <param name="search">Testo da cercare nel titolo o nel nome dell'artista.</param>
        /// <param name="artistId">Filtra per artista.</param>
        /// <param name="availability">Filtra per stato di disponibilità.</param>
        /// <param name="cancellationToken">Token di annullamento della richiesta.</param>
        /// <response code="200">Elenco ordinato per titolo.</response>
        [HttpGet]
        [ProducesResponseType<IEnumerable<ArtworkSummaryDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int? artistId,
            [FromQuery] ArtworkAvailabilityStatus? availability,
            CancellationToken cancellationToken)
        {
            IQueryable<Artwork> query =
                _context.Artworks.AsNoTracking();

            // Ricerca nel titolo e nel nome dell'artista
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(a =>
                    a.Title.Contains(term) ||
                    (a.Artist != null &&
                     (a.Artist.FirstName.Contains(term) ||
                      a.Artist.LastName.Contains(term))));
            }

            // Filtro per artista
            if (artistId.HasValue)
            {
                query = query.Where(a => a.ArtistId == artistId.Value);
            }

            // Filtro per disponibilità
            if (availability.HasValue)
            {
                query = query.Where(a => a.AvailabilityStatus == availability.Value);
            }

            // Opere in ordine di titolo, nel formato dell'API
            var artworks =
                await query
                    .OrderBy(a => a.Title)
                    .Select(a => new ArtworkSummaryDto(
                        a.Id,
                        a.Title,
                        a.Year,
                        a.Technique,
                        a.ImageUrl,
                        a.ArtistId,
                        a.Artist != null
                            ? a.Artist.FirstName + " " + a.Artist.LastName
                            : string.Empty,
                        a.AvailabilityStatus,
                        a.IsOnDisplay))
                    .ToListAsync(cancellationToken);

            return Ok(artworks);
        }


        /// <summary>
        /// Restituisce un'opera con le sue audioguide attive
        /// </summary>
        /// <param name="id">Identificativo dell'opera.</param>
        /// <param name="cancellationToken">Token di annullamento della richiesta.</param>
        /// <response code="200">Opera trovata.</response>
        /// <response code="404">Opera inesistente.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ArtworkDetailDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiMessageResponse>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var artwork =
                await _context.Artworks
                    .AsNoTracking()
                    .Include(a => a.Artist)
                    .Include(a => a.AudioGuides)
                    .FirstOrDefaultAsync(
                        a => a.Id == id,
                        cancellationToken);

            if (artwork == null)
            {
                return NotFound(new ApiMessageResponse(
                    false,
                    "Opera non trovata."));
            }

            // Solo le audioguide attive
            var audioGuides =
                artwork.AudioGuides
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.LanguageCode)
                    .Select(g => new AudioGuideDto(
                        g.Id,
                        g.LanguageCode,
                        g.Title,
                        g.AudioUrl))
                    .ToList();

            return Ok(new ArtworkDetailDto(
                artwork.Id,
                artwork.Title,
                artwork.Year,
                artwork.Technique,
                artwork.Description,
                artwork.ImageUrl,
                artwork.WidthCm,
                artwork.HeightCm,
                artwork.ArtistId,
                artwork.Artist != null
                    ? $"{artwork.Artist.FirstName} {artwork.Artist.LastName}"
                    : string.Empty,
                artwork.Artist?.Nationality,
                artwork.AvailabilityStatus,
                artwork.IsOnDisplay,
                audioGuides));
        }
    }
}