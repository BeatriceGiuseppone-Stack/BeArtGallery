using BeArtGallery.Authorization;
using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using BeArtGallery.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BeArtGallery.Models;

namespace BeArtGallery.Controllers
{
    [Authorize(Roles = RoleNames.StaffOrAdmin)]
    public class ArtworkInquiriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public ArtworkInquiriesController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // -------------------------------------------------
        // ELENCO RICHIESTE
        // -------------------------------------------------

        // GET: ArtworkInquiries
        public async Task<IActionResult> Index(
            ArtworkInquiryStatus? status = null)
        {
            var baseQuery =
                _context.ArtworkInquiries
                    .AsNoTracking();


            // Recuperiamo i contatori con una sola query raggruppata.
            var statusCounts =
                await baseQuery
                    .GroupBy(i => i.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(
                        x => x.Status,
                        x => x.Count);


            statusCounts.TryGetValue(
                ArtworkInquiryStatus.New,
                out var newCount);

            statusCounts.TryGetValue(
                ArtworkInquiryStatus.InProgress,
                out var inProgressCount);

            statusCounts.TryGetValue(
                ArtworkInquiryStatus.Closed,
                out var closedCount);


            var query = baseQuery;


            if (status.HasValue)
            {
                query =
                    query.Where(i =>
                        i.Status == status.Value);
            }


            var inquiries =
                await query
                    .OrderBy(i =>
                        i.Status == ArtworkInquiryStatus.New
                            ? 0
                            : i.Status == ArtworkInquiryStatus.InProgress
                                ? 1
                                : 2)
                    .ThenByDescending(i => i.CreatedAt)
                    .Select(i =>
                        new ArtworkInquiryListItemViewModel
                        {
                            Id = i.Id,

                            ArtworkId = i.ArtworkId,

                            ArtworkTitle =
                                i.Artwork != null
                                    ? i.Artwork.Title
                                    : string.Empty,

                            ArtistName =
                                i.Artwork != null &&
                                i.Artwork.Artist != null
                                    ? i.Artwork.Artist.FirstName
                                      + " "
                                      + i.Artwork.Artist.LastName
                                    : string.Empty,

                            Name = i.Name,

                            Email = i.Email,

                            CreatedAt = i.CreatedAt,

                            Status = i.Status,

                            IsRegisteredUser =
                                i.UserId != null
                        })
                    .ToListAsync();


            var viewModel =
                new ArtworkInquiryIndexViewModel
                {
                    StatusFilter = status,

                    TotalCount =
                        newCount +
                        inProgressCount +
                        closedCount,

                    NewCount = newCount,

                    InProgressCount =
                        inProgressCount,

                    ClosedCount = closedCount,

                    Inquiries = inquiries
                };


            return View(viewModel);
        }


        // -------------------------------------------------
        // DETTAGLIO RICHIESTA
        // -------------------------------------------------

        // GET: ArtworkInquiries/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var inquiry =
                await _context.ArtworkInquiries
                    .AsNoTracking()
                    .Where(i => i.Id == id)
                    .Select(i =>
                        new ArtworkInquiryDetailsViewModel
                        {
                            Id = i.Id,

                            ArtworkId = i.ArtworkId,

                            ArtworkTitle =
                                i.Artwork != null
                                    ? i.Artwork.Title
                                    : string.Empty,

                            ArtworkImageUrl =
                                i.Artwork != null
                                    ? i.Artwork.ImageUrl
                                    : null,

                            ArtistName =
                                i.Artwork != null &&
                                i.Artwork.Artist != null
                                    ? i.Artwork.Artist.FirstName
                                      + " "
                                      + i.Artwork.Artist.LastName
                                    : string.Empty,

                            Name = i.Name,

                            Email = i.Email,

                            Message = i.Message,

                            CreatedAt = i.CreatedAt,

                            Status = i.Status,

                            IsRegisteredUser =
                                i.UserId != null
                        })
                    .FirstOrDefaultAsync();


            if (inquiry == null)
            {
                return NotFound();
            }

            inquiry.Messages =
    await _context.ArtworkInquiryMessages
        .AsNoTracking()
        .Where(m =>
            m.ArtworkInquiryId == id)
        .OrderBy(m => m.CreatedAt)
        .Select(m =>
            new ArtworkInquiryMessageViewModel
            {
                Id = m.Id,

                Message = m.Message,

                CreatedAt = m.CreatedAt,

                ReadAt = m.ReadAt,

                SenderType = m.SenderType,

                SenderEmail =
                    m.SenderUser != null
                        ? m.SenderUser.Email
                        : null
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
                        i.Id == model.InquiryId);


            if (inquiry == null)
            {
                return NotFound();
            }


            var userId =
                _userManager.GetUserId(User);


            var message =
                new ArtworkInquiryMessage
                {
                    ArtworkInquiryId =
                        inquiry.Id,

                    SenderUserId =
                        userId,

                    SenderType =
                        ArtworkInquiryMessageSenderType.Operator,

                    Message =
                        model.Message.Trim(),

                    CreatedAt =
                        DateTime.UtcNow
                };


            _context.ArtworkInquiryMessages.Add(
                message);


            if (inquiry.Status ==
                ArtworkInquiryStatus.New)
            {
                inquiry.Status =
                    ArtworkInquiryStatus.InProgress;
            }


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Risposta inviata correttamente.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = inquiry.Id
                });
        }

        // -------------------------------------------------
        // CAMBIO STATO
        // -------------------------------------------------

        // POST: ArtworkInquiries/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            ArtworkInquiryStatus status)
        {
            if (!Enum.IsDefined(
                typeof(ArtworkInquiryStatus),
                status))
            {
                return BadRequest();
            }


            var inquiry =
                await _context.ArtworkInquiries
                    .FirstOrDefaultAsync(i =>
                        i.Id == id);


            if (inquiry == null)
            {
                return NotFound();
            }


            inquiry.Status = status;


            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] =
                "Lo stato della richiesta è stato aggiornato correttamente.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id
                });
        }
    }
}