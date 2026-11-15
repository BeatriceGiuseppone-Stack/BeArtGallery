using BeArtGallery.Authorization;
using BeArtGallery.Dtos;
using BeArtGallery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeArtGallery.Controllers.Api
{
    /// <summary>
    /// API con gli indicatori della galleria, solo per Staff e Admin.
    /// Può essere usata da strumenti esterni di analisi.
    /// </summary>
    [ApiController]
    [Route("api/dashboard")]
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    [Produces("application/json")]
    [Tags("Dashboard")]
    public class DashboardApiController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardApiController(
            IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }


        /// <summary>
        /// Restituisce i principali indicatori (KPI) della galleria
        /// </summary>
        /// <response code="200">Indicatori calcolati al momento della richiesta.</response>
        /// <response code="401">Utente non autenticato.</response>
        /// <response code="403">Utente senza ruolo Staff o Admin.</response>
        [HttpGet("stats")]
        [ProducesResponseType<DashboardStatsDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetStats(
            CancellationToken cancellationToken)
        {
            // Stessi calcoli della pagina Dashboard
            var m = await _dashboardService.BuildAsync(cancellationToken);

            return Ok(new DashboardStatsDto(
                m.TotalUsers,
                m.TotalArtworks,
                m.AvailableArtworks,
                m.ConfirmedBookings,
                m.UpcomingActivities,
                m.OccupancyRate,
                m.ExpectedVisitors,
                m.CheckedInVisitors,
                m.NoShowVisitors,
                m.AttendanceRate,
                m.TotalArtworkViews,
                m.TotalFavorites,
                m.OpenInquiries,
                m.TotalFeedbacks,
                m.AverageRating,
                m.BookingsByMonth
                    .Select(x => new MonthlyValueDto(x.Label, x.Value))
                    .ToList()));
        }
    }
}