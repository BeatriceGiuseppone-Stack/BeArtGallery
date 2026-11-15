using BeArtGallery.Data;
using BeArtGallery.Models;
using BeArtGallery.Models.Enums;
using BeArtGallery.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers
{
    // "Le mie richieste": le richieste di informazioni del visitatore collegato
    [Authorize]
    public class MyInquiriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public MyInquiriesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: MyInquiries
        // Elenco delle richieste dell'utente, dalla più recente
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // Per ogni richiesta: opera (o titolo salvato se l'opera è stata eliminata),
            // stato, risposte non lette e data dell'ultimo messaggio
            var inquiries = await _context.ArtworkInquiries.AsNoTracking().Where(i => i.UserId == userId)
                    .Select(i =>
                        new MyArtworkInquiryListItemViewModel
                        {
                            Id = i.Id,
                            ArtworkDeleted = i.ArtworkId == null,
                            ArtworkTitle = i.Artwork != null ? i.Artwork.Title : i.ArtworkTitle,
                            ArtworkImageUrl = i.Artwork != null ? i.Artwork.ImageUrl : null,
                            CreatedAt = i.CreatedAt,
                            Status = i.Status,
                            UnreadReplies = i.Messages.Count(m => m.SenderType ==
                                    ArtworkInquiryMessageSenderType.Operator && m.ReadAt == null),
                            LastActivity = i.Messages.Select(m => (DateTime?)m.CreatedAt).Max() ?? i.CreatedAt
                        }).OrderByDescending(i => i.LastActivity).ToListAsync();

            return View(inquiries);
        }


        // GET: MyInquiries/Details/5
        // Conversazione con la galleria su una richiesta
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // La richiesta deve appartenere all'utente collegato
            var inquiry = await _context.ArtworkInquiries.AsNoTracking()
                    .Where(i => i.Id == id && i.UserId == userId)
                    .Select(i =>
                        new MyArtworkInquiryDetailsViewModel
                        {
                            Id = i.Id,
                            ArtworkId = i.ArtworkId,
                            ArtworkTitle = i.Artwork != null ? i.Artwork.Title : i.ArtworkTitle,
                            ArtworkImageUrl = i.Artwork != null ? i.Artwork.ImageUrl : null,
                            Message = i.Message,
                            CreatedAt = i.CreatedAt,
                            Status = i.Status
                        }) .FirstOrDefaultAsync();

            if (inquiry == null)
            {
                return NotFound();
            }

            // Le risposte dello staff vengono segnate come lette
            await _context.ArtworkInquiryMessages.Where(m => m.ArtworkInquiryId == id 
            && m.SenderType == ArtworkInquiryMessageSenderType.Operator && m.ReadAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty( m => m.ReadAt, DateTime.UtcNow));

            // Tutti i messaggi della conversazione in ordine di data
            inquiry.Messages = await _context.ArtworkInquiryMessages.AsNoTracking()
                    .Where(m =>  m.ArtworkInquiryId == id).OrderBy(m => m.CreatedAt)
                    .Select(m =>
                        new ArtworkInquiryMessageViewModel
                        {
                            Id = m.Id,
                            Message = m.Message,
                            CreatedAt = m.CreatedAt,
                            ReadAt = m.ReadAt,
                            SenderType = m.SenderType
                        }).ToListAsync();


            // Form vuoto per la risposta
            inquiry.Reply = new ArtworkInquiryReplyViewModel
                {
                    InquiryId = id
                };

            return View(inquiry);
        }


        // POST: MyInquiries/Reply
        // Il visitatore risponde nella conversazione
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(ArtworkInquiryReplyViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Inserisci un messaggio valido.";

                return RedirectToAction(nameof(Details),
                    new
                    {
                        id = model.InquiryId
                    });
            }


            // Cerca la richiesta solo tra quelle dell'utente
            var inquiry = await _context.ArtworkInquiries.FirstOrDefaultAsync(i => i.Id == model.InquiryId && i.UserId == userId);

            if (inquiry == null)
            {
                return NotFound();
            }

            // Su una richiesta chiusa non si può più scrivere
            if (inquiry.Status == ArtworkInquiryStatus.Closed)
            {
                TempData["ErrorMessage"] = "La richiesta è stata chiusa e non è possibile inviare altri messaggi.";

                return RedirectToAction(nameof(Details),
                    new
                    {
                        id = inquiry.Id
                    });
            }

            // Salva il nuovo messaggio del visitatore
            var message =
                new ArtworkInquiryMessage
                {
                    ArtworkInquiryId = inquiry.Id,
                    SenderUserId = userId,
                    SenderType = ArtworkInquiryMessageSenderType.User,
                    Message = model.Message.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

            _context.ArtworkInquiryMessages.Add(message);

            await _context.SaveChangesAsync();

            return RedirectToAction( nameof(Details),
                new {id = inquiry.Id});
        }
    }
}