using BeArtGallery.Authorization;
using BeArtGallery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeArtGallery.Controllers
{
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;


        public DashboardController(
            IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }


        // GET: /Dashboard
        // Il calcolo degli indicatori è nel DashboardService,
        // condiviso con l'API /api/dashboard/stats.
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var model =
                await _dashboardService.BuildAsync(
                    cancellationToken);

            return View(model);
        }
    }
}
