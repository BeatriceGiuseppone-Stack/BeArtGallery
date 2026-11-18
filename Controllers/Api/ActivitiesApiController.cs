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
    /// API pubblica per leggere gli eventi della galleria
    /// </summary>
    [ApiController]
    [Route("api/activities")]
    [AllowAnonymous]
    [Produces("application/json")]
    [Tags("Attività")]
    public class ActivitiesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly TimeProvider _timeProvider;

        public ActivitiesApiController(
            ApplicationDbContext context,
            TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }


        /// <summary>
        /// Elenca le attività attive e non annullate
        /// </summary>
        /// <param name="type">Filtro facoltativo per tipo di attività.</param>
        /// <param name="includePast">Se true include anche le attività già iniziate.</param>
        /// <param name="cancellationToken">Token di annullamento della richiesta.</param>
        /// <response code="200">Elenco ordinato per data di inizio.</response>
        [HttpGet]
        [ProducesResponseType<IEnumerable<ActivityDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] ActivityType? type,
            [FromQuery] bool includePast,
            CancellationToken cancellationToken)
        {
            // Ora attuale della galleria
            var now = _timeProvider.GetLocalNow().DateTime;

            // Solo attività attive e non annullate
            IQueryable<GalleryActivity> query =
                _context.GalleryActivities
                    .AsNoTracking()
                    .Where(a =>
                        a.IsActive &&
                        a.Status == GalleryActivityStatus.Scheduled);

            // Di base solo le attività future
            if (!includePast)
            {
                query = query.Where(a => a.StartDateTime > now);
            }

            // Filtro per tipo di attività
            if (type.HasValue)
            {
                query = query.Where(a => a.ActivityType == type.Value);
            }

            var activities =
                await ProjectToDto(query.OrderBy(a => a.StartDateTime))
                    .ToListAsync(cancellationToken);

            return Ok(activities);
        }


        /// <summary>
        /// Restituisce un'attività con i posti ancora liberi
        /// </summary>
        /// <param name="id">Identificativo dell'attività.</param>
        /// <param name="cancellationToken">Token di annullamento della richiesta.</param>
        /// <response code="200">Attività trovata.</response>
        /// <response code="404">Attività inesistente.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ActivityDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiMessageResponse>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var activity =
                await ProjectToDto(
                        _context.GalleryActivities
                            .AsNoTracking()
                            .Where(a => a.Id == id))
                    .FirstOrDefaultAsync(cancellationToken);

            if (activity == null)
            {
                return NotFound(new ApiMessageResponse(
                    false,
                    "Attività non trovata."));
            }

            return Ok(activity);
        }


        // Trasforma le attività nel formato dell'API.
        // La somma dei partecipanti la fa il database (SUM), senza caricare le prenotazioni.
        private static IQueryable<ActivityDto> ProjectToDto(
            IQueryable<GalleryActivity> query)
        {
            return query.Select(a => new
            {
                Activity = a,
                Booked =
                    a.Bookings
                        .Where(b => b.Status == BookingStatus.Confirmed)
                        .Sum(b => (int?)b.NumberOfParticipants) ?? 0
            })
            .Select(x => new ActivityDto(
                x.Activity.Id,
                x.Activity.Title,
                x.Activity.Description,
                x.Activity.ActivityType,
                x.Activity.StartDateTime,
                x.Activity.DurationMinutes,
                x.Activity.Location,
                x.Activity.MaxParticipants,
                x.Booked,
                // I posti liberi non possono essere negativi
                x.Activity.MaxParticipants - x.Booked < 0 ? 0
                    : x.Activity.MaxParticipants - x.Booked,
                x.Activity.Status));
        }
    }
}