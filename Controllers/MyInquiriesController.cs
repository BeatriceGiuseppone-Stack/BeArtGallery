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


        public async Task<IActionResult> Index()
        {
            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }


            var inquiries =
                await _context.ArtworkInquiries
                    .AsNoTracking()
                    .Where(i =>
                        i.UserId == userId)
                    .Select(i =>
                        new MyArtworkInquiryListItemViewModel
                        {
                            Id = i.Id,

                            ArtworkTitle =
                                i.Artwork != null
                                    ? i.Artwork.Title
                                    : string.Empty,

                            ArtworkImageUrl =
                                i.Artwork != null
                                    ? i.Artwork.ImageUrl
                                    : null,

                            CreatedAt =
                                i.CreatedAt,

                            Status =
                                i.Status,

                            UnreadReplies =
                                i.Messages.Count(m =>
                                    m.SenderType ==
                                    ArtworkInquiryMessageSenderType.Operator &&
                                    m.ReadAt == null),

                            LastActivity =
                                i.Messages
                                    .Select(m =>
                                        (DateTime?)m.CreatedAt)
                                    .Max()
                                ?? i.CreatedAt
                        })
                    .OrderByDescending(i =>
                        i.LastActivity)
                    .ToListAsync();


            return View(inquiries);
        }


        public async Task<IActionResult> Details(
            int id)
        {
            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }


            var inquiry =
                await _context.ArtworkInquiries
                    .AsNoTracking()
                    .Where(i =>
                        i.Id == id &&
                        i.UserId == userId)
                    .Select(i =>
                        new MyArtworkInquiryDetailsViewModel
                        {
                            Id = i.Id,

                            ArtworkId =
                                i.ArtworkId,

                            ArtworkTitle =
                                i.Artwork != null
                                    ? i.Artwork.Title
                                    : string.Empty,

                            ArtworkImageUrl =
                                i.Artwork != null
                                    ? i.Artwork.ImageUrl
                                    : null,

                            Message =
                                i.Message,

                            CreatedAt =
                                i.CreatedAt,

                            Status =
                                i.Status
                        })
                    .FirstOrDefaultAsync();


            if (inquiry == null)
            {
                return NotFound();
            }


            // Le risposte dello staff diventano lette.
            await _context.ArtworkInquiryMessages
                .Where(m =>
                    m.ArtworkInquiryId == id &&
                    m.SenderType ==
                    ArtworkInquiryMessageSenderType.Operator &&
                    m.ReadAt == null)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        m => m.ReadAt,
                        DateTime.UtcNow));


            inquiry.Messages =
                await _context.ArtworkInquiryMessages
                    .AsNoTracking()
                    .Where(m =>
                        m.ArtworkInquiryId == id)
                    .OrderBy(m =>
                        m.CreatedAt)
                    .Select(m =>
                        new ArtworkInquiryMessageViewModel
                        {
                            Id =
                                m.Id,

                            Message =
                                m.Message,

                            CreatedAt =
                                m.CreatedAt,

                            ReadAt =
                                m.ReadAt,

                            SenderType =
                                m.SenderType
                        })
                    .ToListAsync();


            inquiry.Reply =
                new ArtworkInquiryReplyViewModel
                {
                    InquiryId = id
                };


            return View(inquiry);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(
            ArtworkInquiryReplyViewModel model)
        {
            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }


            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Inserisci un messaggio valido.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = model.InquiryId
                    });
            }


            var inquiry =
                await _context.ArtworkInquiries
                    .FirstOrDefaultAsync(i =>
                        i.Id == model.InquiryId &&
                        i.UserId == userId);


            if (inquiry == null)
            {
                return NotFound();
            }


            if (inquiry.Status ==
                ArtworkInquiryStatus.Closed)
            {
                TempData["ErrorMessage"] =
                    "La richiesta è stata chiusa e non è possibile inviare altri messaggi.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = inquiry.Id
                    });
            }


            var message =
                new ArtworkInquiryMessage
                {
                    ArtworkInquiryId =
                        inquiry.Id,

                    SenderUserId =
                        userId,

                    SenderType =
                        ArtworkInquiryMessageSenderType.User,

                    Message =
                        model.Message.Trim(),

                    CreatedAt =
                        DateTime.UtcNow
                };


            _context.ArtworkInquiryMessages.Add(
                message);


            await _context.SaveChangesAsync();


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = inquiry.Id
                });
        }
    }
}