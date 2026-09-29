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

            var activity = await _context.GalleryActivities
                .Include(a => a.Bookings)
                .FirstOrDefaultAsync(a =>
                    a.Id == request.GalleryActivityId);

            if (activity == null)
            {
                return NotFound(new
                {
                    message = "L'attività selezionata non esiste."
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

            bool alreadyBooked = activity.Bookings.Any(b =>
                b.UserId == userId &&
                b.Status == BookingStatus.Confirmed);

            if (alreadyBooked)
            {
                return Conflict(new
                {
                    message = "Hai già una prenotazione attiva per questa attività."
                });
            }

            int bookedParticipants = activity.Bookings
                .Where(b =>
                    b.Status == BookingStatus.Confirmed)
                .Sum(b => b.NumberOfParticipants);

            int availablePlaces =
                activity.MaxParticipants - bookedParticipants;

            if (request.NumberOfParticipants > availablePlaces)
            {
                return Conflict(new
                {
                    message =
                        $"Sono disponibili soltanto {availablePlaces} posti."
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
    }
}