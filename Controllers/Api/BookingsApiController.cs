using BeArtGallery.Data;
using BeArtGallery.Dtos;
using BeArtGallery.Models;
using BeArtGallery.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers.Api
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public class BookingsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingsApiController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [FromBody] CreateBookingRequest request)
        {
            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var activity = await _context.GalleryActivities.Include(a => a.Bookings).FirstOrDefaultAsync(a => a.Id == request.GalleryActivityId);

            if (activity == null)
            {
                return NotFound(new
                {
                    message = "L'attività selezionata non esiste."
                });
            }

            if (activity.Status == GalleryActivityStatus.Cancelled)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "L'attività è stata annullata."
                });
            }

            if (!activity.IsActive)
            {
                return BadRequest(new
                {
                    message = "L'attività non è disponibile."
                });
            }

            if (activity.StartDateTime <= DateTime.Now)
            {
                return BadRequest(new
                {
                    message = "Non è possibile prenotare un'attività già iniziata."
                });
            }

            bool alreadyBooked = activity.Bookings.Any(b => b.UserId == userId && b.Status == BookingStatus.Confirmed);

            if (alreadyBooked)
            {
                return Conflict(new
                {
                    message = "Hai già una prenotazione attiva per questa attività."
                });
            }

            int bookedParticipants = activity.Bookings.Where(b => b.Status == BookingStatus.Confirmed).Sum(b => b.NumberOfParticipants);

            int availablePlaces = activity.MaxParticipants - bookedParticipants;

            if (request.NumberOfParticipants > availablePlaces)
            {
                return Conflict(new
                {
                    message = $"Sono disponibili soltanto {availablePlaces} posti."
                });
            }

            var booking = new Booking
            {
                GalleryActivityId = activity.Id,
                UserId = userId,
                NumberOfParticipants = request.NumberOfParticipants,
                BookingDate = DateTime.UtcNow,
                Status = BookingStatus.Confirmed
            };

            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                bookingId = booking.Id,
                message = "Prenotazione effettuata con successo."
            });
        }

        // POST: /api/bookings/{id}/cancel
        [HttpPost("{id:int}/cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Utente non autenticato."
                });
            }

            var booking = await _context.Bookings.Include(b => b.GalleryActivity).FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Prenotazione non trovata."
                });
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "La prenotazione è già stata annullata."
                });
            }

            if (booking.GalleryActivity == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Attività associata non disponibile."
                });
            }

            if (booking.GalleryActivity.StartDateTime <= DateTime.Now)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Non è possibile annullare una prenotazione per un'attività già iniziata o conclusa."
                });
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancelledByUserId = userId;
            booking.CancellationType = BookingCancellationType.User;
            booking.CancellationReason = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Prenotazione annullata correttamente."
            });
        }
    }
}