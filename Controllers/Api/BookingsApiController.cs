using BeArtGallery.Data;
using BeArtGallery.Dtos;
using BeArtGallery.Models.Enums;
using BeArtGallery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers.Api
{
    /// <summary>
    /// API REST per le prenotazioni del visitatore autenticato.
    /// </summary>
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    [Produces("application/json")]
    [Tags("Prenotazioni")]
    public class BookingsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBookingService _bookingService;
        private readonly TimeProvider _timeProvider;

        public BookingsApiController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IBookingService bookingService,
            TimeProvider timeProvider)
        {
            _context = context;
            _userManager = userManager;
            _bookingService = bookingService;
            _timeProvider = timeProvider;
        }


        /// <summary>
        /// Restituisce le prenotazioni dell'utente autenticato,
        /// dalla più recente alla meno recente.
        /// </summary>
        /// <response code="200">Elenco delle prenotazioni.</response>
        /// <response code="401">Utente non autenticato.</response>
        [HttpGet("mine")]
        [ProducesResponseType<IEnumerable<MyBookingDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMine(
            CancellationToken cancellationToken)
        {
            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var now = _timeProvider.GetLocalNow().DateTime;

            var bookings =
                await _context.Bookings
                    .AsNoTracking()
                    .Where(b =>
                        b.UserId == userId &&
                        b.GalleryActivity != null)
                    .OrderByDescending(b =>
                        b.GalleryActivity!.StartDateTime)
                    .Select(b => new MyBookingDto(
                        b.Id,
                        b.GalleryActivityId,
                        b.GalleryActivity!.Title,
                        b.GalleryActivity.StartDateTime,
                        b.GalleryActivity.Location,
                        b.NumberOfParticipants,
                        b.BookingDate,
                        b.Status,
                        b.CheckedInAt,
                        b.Status == BookingStatus.Confirmed &&
                        b.GalleryActivity.StartDateTime > now))
                    .ToListAsync(cancellationToken);

            return Ok(bookings);
        }


        /// <summary>
        /// Crea una prenotazione per un'attività.
        /// Richiede l'header RequestVerificationToken (protezione CSRF).
        /// </summary>
        /// <response code="200">Prenotazione creata.</response>
        /// <response code="400">Dati non validi o attività non prenotabile.</response>
        /// <response code="404">Attività inesistente.</response>
        /// <response code="409">Posti esauriti, prenotazione già presente o conflitto concorrente.</response>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ProducesResponseType<BookingCreatedResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiMessageResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiMessageResponse>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiMessageResponse>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateBookingRequest request,
            CancellationToken cancellationToken)
        {
            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var result =
                await _bookingService.CreateAsync(
                    userId,
                    request.GalleryActivityId,
                    request.NumberOfParticipants,
                    cancellationToken);

            if (!result.Success)
            {
                return ToErrorResult(result);
            }

            return Ok(new BookingCreatedResponse(
                true,
                result.Message,
                result.Booking!.Id));
        }


        /// <summary>
        /// Annulla una prenotazione dell'utente autenticato
        /// (solo se l'attività non è ancora iniziata).
        /// </summary>
        /// <param name="id">Identificativo della prenotazione.</param>
        /// <param name="cancellationToken">Token di annullamento della richiesta.</param>
        /// <response code="200">Prenotazione annullata.</response>
        /// <response code="400">La prenotazione non può essere annullata.</response>
        /// <response code="404">Prenotazione inesistente o di un altro utente.</response>
        [HttpPost("{id:int}/cancel")]
        [ValidateAntiForgeryToken]
        [ProducesResponseType<ApiMessageResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiMessageResponse>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiMessageResponse>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelBooking(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiMessageResponse(
                    false,
                    "Utente non autenticato."));
            }

            var result =
                await _bookingService.CancelByUserAsync(
                    userId,
                    id,
                    cancellationToken);

            if (!result.Success)
            {
                return ToErrorResult(result);
            }

            return Ok(new ApiMessageResponse(
                true,
                result.Message));
        }


        // Traduce l'esito del servizio nel codice HTTP appropriato.
        private IActionResult ToErrorResult(
            BookingOperationResult result)
        {
            var body = new ApiMessageResponse(
                false,
                result.Message);

            return result.ErrorCode switch
            {
                BookingErrorCode.ActivityNotFound or
                BookingErrorCode.BookingNotFound
                    => NotFound(body),

                BookingErrorCode.AlreadyBooked or
                BookingErrorCode.NotEnoughPlaces or
                BookingErrorCode.ConcurrencyConflict
                    => Conflict(body),

                _ => BadRequest(body)
            };
        }
    }
}
