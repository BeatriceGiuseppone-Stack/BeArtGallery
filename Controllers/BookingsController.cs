using BeArtGallery.Authorization;
using BeArtGallery.Data;
using BeArtGallery.Models.Enums;
using BeArtGallery.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Bookings
        // GET: /Bookings
        public async Task<IActionResult> Index()
        {
            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }


            // =================================================
            // GENERA IL TOKEN PER LE VECCHIE PRENOTAZIONI
            // =================================================

            var bookingsWithoutQr =
                await _context.Bookings
                    .Where(b =>
                        b.UserId == userId &&
                        b.QrToken == null)
                    .ToListAsync();


            if (bookingsWithoutQr.Any())
            {
                foreach (var booking in bookingsWithoutQr)
                {
                    booking.QrToken =
                        Guid.NewGuid()
                            .ToString("N");
                }


                await _context.SaveChangesAsync();
            }


            // =================================================
            // CARICAMENTO PRENOTAZIONI
            // =================================================

            var bookings =
                await _context.Bookings
                    .AsNoTracking()
                    .Include(b => b.GalleryActivity)
                    .Where(b =>
                        b.UserId == userId)
                    .OrderByDescending(b =>
                        b.GalleryActivity!.StartDateTime)
                    .Select(b =>
                        new MyBookingViewModel
                        {
                            BookingId = b.Id,
                            ActivityTitle = b.GalleryActivity != null ? b.GalleryActivity.Title : string.Empty,
                            ActivityType = b.GalleryActivity!.ActivityType,
                            StartDateTime = b.GalleryActivity.StartDateTime,
                            Location = b.GalleryActivity.Location,
                            NumberOfParticipants = b.NumberOfParticipants,
                            BookingDate = b.BookingDate,
                            Status = b.Status,
                            CancelledAt = b.CancelledAt,
                            CancellationType = b.CancellationType,
                            CancellationReason = b.CancellationReason,
                            QrToken = b.QrToken,
                            CheckedInAt = b.CheckedInAt
                        })
                    .ToListAsync();


            return View(bookings);
        }

        // GET: /Bookings/Manage
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        public async Task<IActionResult> Manage(
            int? activityId,
            BookingStatus? status,
            DateTime? fromDate,
            DateTime? toDate,
            string? userEmail)
        {
            var query = _context.Bookings.AsNoTracking().Include(b => b.GalleryActivity).Include(b => b.User).AsQueryable();

            // FILTRO ATTIVITÀ
            if (activityId.HasValue)
            {
                query = query.Where( b => b.GalleryActivityId == activityId.Value);
            }

            // FILTRO STATO
            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            // FILTRO DATA ATTIVITÀ - DA
            if (fromDate.HasValue)
            {
                query = query.Where(b => b.GalleryActivity != null && b.GalleryActivity.StartDateTime >= fromDate.Value);
            }

            // FILTRO DATA ATTIVITÀ - A
            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1);

                query = query.Where(b => b.GalleryActivity != null && b.GalleryActivity.StartDateTime < endDate);
            }

            // FILTRO EMAIL UTENTE
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                query = query.Where(b => b.User != null && b.User.Email != null && b.User.Email.Contains(userEmail));
            }

            var bookings = await query.OrderByDescending(b => b.GalleryActivity!.StartDateTime)
                .Select(b => new BookingManagementViewModel
                {
                    BookingId = b.Id,
                    UserEmail = b.User != null ? b.User.Email ?? string.Empty : string.Empty,
                    ActivityTitle = b.GalleryActivity != null ? b.GalleryActivity.Title : string.Empty,
                    ActivityType = b.GalleryActivity!.ActivityType,
                    StartDateTime = b.GalleryActivity.StartDateTime,
                    Location = b.GalleryActivity.Location,
                    NumberOfParticipants = b.NumberOfParticipants,
                    BookingDate = b.BookingDate,
                    Status = b.Status
                })
                .ToListAsync();

            var activities = await _context.GalleryActivities.AsNoTracking().OrderByDescending(a => a.StartDateTime)
                .Select(a => new BookingActivityFilterViewModel
                {
                    Id = a.Id,
                    Title = a.Title
                })
                .ToListAsync();

            var model = new BookingManagementPageViewModel
            {
                Bookings = bookings,
                Activities = activities,
                ActivityId = activityId,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                UserEmail = userEmail
            };

            return View(model);
        }

        // GET: /Bookings/CancelByStaff/5
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        [HttpGet]
        public async Task<IActionResult> CancelByStaff(int id)
        {
            var booking = await _context.Bookings.AsNoTracking().Include(b => b.User)
                .Include(b => b.GalleryActivity).FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                TempData["ErrorMessage"] = "La prenotazione risulta già annullata.";

                return RedirectToAction(nameof(Manage));
            }

            if (booking.GalleryActivity == null || booking.GalleryActivity.StartDateTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Non è possibile annullare una prenotazione relativa a un'attività già iniziata.";

                return RedirectToAction(nameof(Manage));
            }

            var model = new StaffCancelBookingViewModel
            {
                BookingId = booking.Id,
                UserEmail = booking.User?.Email ?? string.Empty,
                ActivityTitle = booking.GalleryActivity.Title,
                StartDateTime = booking.GalleryActivity.StartDateTime
            };

            return View(model);
        }

        // POST: /Bookings/CancelByStaff
        [Authorize(Roles = RoleNames.StaffOrAdmin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelByStaff(
            StaffCancelBookingViewModel model)
        {
            var booking = await _context.Bookings.Include(b => b.User).Include(b => b.GalleryActivity)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null)
            {
                return NotFound();
            }

            model.UserEmail = booking.User?.Email ?? string.Empty;

            model.ActivityTitle = booking.GalleryActivity?.Title ?? string.Empty;

            model.StartDateTime = booking.GalleryActivity?.StartDateTime ?? default;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                TempData["ErrorMessage"] = "La prenotazione risulta già annullata.";

                return RedirectToAction(nameof(Manage));
            }

            if (booking.GalleryActivity == null || booking.GalleryActivity.StartDateTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Non è possibile annullare questa prenotazione.";

                return RedirectToAction(nameof(Manage));
            }

            var currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Challenge();
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancelledByUserId = currentUserId;
            booking.CancellationType = BookingCancellationType.StaffOrAdmin;
            booking.CancellationReason = model.CancellationReason.Trim();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"La prenotazione di {model.UserEmail} è stata annullata.";

            return RedirectToAction(nameof(Manage));
        }
    }
}