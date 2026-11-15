using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.ViewComponents
{
    // Numero di risposte non lette, mostrato vicino a "Le mie richieste" nel menu
    public class InquiryNotificationCountViewComponent
        : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public InquiryNotificationCountViewComponent(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Se l'utente non ha fatto l'accesso non si mostra niente
            if (UserClaimsPrincipal.Identity?.IsAuthenticated != true)
            {
                return Content(string.Empty);
            }

            var userId = _userManager.GetUserId(UserClaimsPrincipal);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Content(string.Empty);
            }

            // Conta le risposte dello staff non ancora lette sulle richieste dell'utente
            var count = await _context.ArtworkInquiryMessages.AsNoTracking().CountAsync(m => 
                        m.SenderType == ArtworkInquiryMessageSenderType.Operator && m.ReadAt == null && m.ArtworkInquiry != null &&
                        m.ArtworkInquiry.UserId == userId);

            return View(count);
        }
    }
}