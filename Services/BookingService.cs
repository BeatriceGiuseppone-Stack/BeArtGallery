using System.Data;
using BeArtGallery.Data;
using BeArtGallery.Models;
using BeArtGallery.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Services
{
    /// <summary>
    /// Codici di esito delle operazioni sulle prenotazioni.
    /// Permettono al livello di presentazione (API o MVC)
    /// di tradurre l'esito nel codice HTTP o nel messaggio corretto.
    /// </summary>
    public enum BookingErrorCode
    {
        None = 0,
        InvalidParticipants,
        ActivityNotFound,
        ActivityCancelled,
        ActivityNotActive,
        ActivityAlreadyStarted,
        AlreadyBooked,
        NotEnoughPlaces,
        BookingNotFound,
        BookingNotConfirmed,
        AlreadyCheckedIn,
        ConcurrencyConflict
    }


    /// <summary>
    /// Risultato di un'operazione sulle prenotazioni.
    /// </summary>
    public sealed class BookingOperationResult
    {
        public bool Success { get; private init; }

        public BookingErrorCode ErrorCode { get; private init; }

        public string Message { get; private init; } = string.Empty;

        public Booking? Booking { get; private init; }


        public static BookingOperationResult Ok(
            string message,
            Booking? booking = null)
        {
            return new BookingOperationResult
            {
                Success = true,
                ErrorCode = BookingErrorCode.None,
                Message = message,
                Booking = booking
            };
        }


        public static BookingOperationResult Fail(
            BookingErrorCode errorCode,
            string message)
        {
            return new BookingOperationResult
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message
            };
        }
    }


    /// <summary>
    /// Logica di dominio delle prenotazioni: creazione,
    /// annullamento da parte del visitatore e check-in.
    /// </summary>
    public interface IBookingService
    {
        Task<BookingOperationResult> CreateAsync(
            string userId,
            int galleryActivityId,
            int numberOfParticipants,
            CancellationToken cancellationToken = default);

        Task<BookingOperationResult> CancelByUserAsync(
            string userId,
            int bookingId,
            CancellationToken cancellationToken = default);

        Task<BookingOperationResult> CheckInAsync(
            string qrToken,
            string staffUserId,
            CancellationToken cancellationToken = default);
    }


    public class BookingService : IBookingService
    {
        public const int MinParticipants = 1;

        public const int MaxParticipantsPerBooking = 10;


        private readonly ApplicationDbContext _context;

        private readonly TimeProvider _timeProvider;


        public BookingService(
            ApplicationDbContext context,
            TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }


        // Convenzione sulle date del progetto:
        // - gli orari delle attività (StartDateTime) sono orari
        //   locali della galleria, inseriti dallo staff;
        // - i timestamp degli eventi (prenotazione, annullamento,
        //   check-in...) sono salvati in UTC e convertiti in ora
        //   locale solo in visualizzazione.

        private DateTime LocalNow =>
            _timeProvider.GetLocalNow().DateTime;

        private DateTime UtcNow =>
            _timeProvider.GetUtcNow().UtcDateTime;


        // =================================================
        // CREAZIONE PRENOTAZIONE
        // =================================================

        public async Task<BookingOperationResult> CreateAsync(
            string userId,
            int galleryActivityId,
            int numberOfParticipants,
            CancellationToken cancellationToken = default)
        {
            if (numberOfParticipants < MinParticipants ||
                numberOfParticipants > MaxParticipantsPerBooking)
            {
                return BookingOperationResult.Fail(
                    BookingErrorCode.InvalidParticipants,
                    $"Il numero di partecipanti deve essere compreso tra {MinParticipants} e {MaxParticipantsPerBooking}.");
            }


            // La verifica dei posti e l'inserimento avvengono nella
            // stessa transazione SERIALIZABLE: se due visitatori
            // prenotano contemporaneamente gli ultimi posti, il
            // database fa fallire una delle due transazioni invece
            // di permettere l'overbooking.

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            try
            {
                var activity =
                    await _context.GalleryActivities
                        .FirstOrDefaultAsync(
                            a => a.Id == galleryActivityId,
                            cancellationToken);

                if (activity == null)
                {
                    return BookingOperationResult.Fail(
                        BookingErrorCode.ActivityNotFound,
                        "L'attività selezionata non esiste.");
                }

                if (activity.Status == GalleryActivityStatus.Cancelled)
                {
                    return BookingOperationResult.Fail(
                        BookingErrorCode.ActivityCancelled,
                        "L'attività è stata annullata.");
                }

                if (!activity.IsActive)
                {
                    return BookingOperationResult.Fail(
                        BookingErrorCode.ActivityNotActive,
                        "L'attività non è disponibile.");
                }

                if (activity.StartDateTime <= LocalNow)
                {
                    return BookingOperationResult.Fail(
                        BookingErrorCode.ActivityAlreadyStarted,
                        "Non è possibile prenotare un'attività già iniziata.");
                }


                var confirmedBookings =
                    _context.Bookings
                        .Where(b =>
                            b.GalleryActivityId == activity.Id &&
                            b.Status == BookingStatus.Confirmed);


                bool alreadyBooked =
                    await confirmedBookings.AnyAsync(
                        b => b.UserId == userId,
                        cancellationToken);

                if (alreadyBooked)
                {
                    return BookingOperationResult.Fail(
                        BookingErrorCode.AlreadyBooked,
                        "Hai già una prenotazione attiva per questa attività.");
                }


                int bookedParticipants =
                    await confirmedBookings.SumAsync(
                        b => b.NumberOfParticipants,
                        cancellationToken);

                int availablePlaces =
                    Math.Max(
                        0,
                        activity.MaxParticipants - bookedParticipants);

                if (numberOfParticipants > availablePlaces)
                {
                    return BookingOperationResult.Fail(
                        BookingErrorCode.NotEnoughPlaces,
                        $"Sono disponibili soltanto {availablePlaces} posti.");
                }


                var booking = new Booking
                {
                    GalleryActivityId = activity.Id,
                    UserId = userId,
                    NumberOfParticipants = numberOfParticipants,
                    BookingDate = UtcNow,
                    Status = BookingStatus.Confirmed,
                    QrToken = Guid.NewGuid().ToString("N")
                };

                _context.Bookings.Add(booking);

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return BookingOperationResult.Ok(
                    "Prenotazione effettuata con successo.",
                    booking);
            }
            catch (Exception ex) when (
                ex is DbUpdateException ||
                ex is System.Data.Common.DbException)
            {
                // Tipicamente un deadlock/conflitto di serializzazione
                // causato da una prenotazione concorrente.
                return BookingOperationResult.Fail(
                    BookingErrorCode.ConcurrencyConflict,
                    "Un'altra prenotazione è stata registrata nello stesso momento. Riprova.");
            }
        }


        // =================================================
        // ANNULLAMENTO DA PARTE DEL VISITATORE
        // =================================================

        public async Task<BookingOperationResult> CancelByUserAsync(
            string userId,
            int bookingId,
            CancellationToken cancellationToken = default)
        {
            var booking =
                await _context.Bookings
                    .Include(b => b.GalleryActivity)
                    .FirstOrDefaultAsync(
                        b => b.Id == bookingId && b.UserId == userId,
                        cancellationToken);

            if (booking == null)
            {
                return BookingOperationResult.Fail(
                    BookingErrorCode.BookingNotFound,
                    "Prenotazione non trovata.");
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return BookingOperationResult.Fail(
                    BookingErrorCode.BookingNotConfirmed,
                    "La prenotazione è già stata annullata.");
            }

            if (booking.GalleryActivity == null)
            {
                return BookingOperationResult.Fail(
                    BookingErrorCode.ActivityNotFound,
                    "Attività associata non disponibile.");
            }

            if (booking.GalleryActivity.StartDateTime <= LocalNow)
            {
                return BookingOperationResult.Fail(
                    BookingErrorCode.ActivityAlreadyStarted,
                    "Non è possibile annullare una prenotazione per un'attività già iniziata o conclusa.");
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = UtcNow;
            booking.CancelledByUserId = userId;
            booking.CancellationType = BookingCancellationType.User;
            booking.CancellationReason = null;

            await _context.SaveChangesAsync(cancellationToken);

            return BookingOperationResult.Ok(
                "Prenotazione annullata correttamente.",
                booking);
        }


        // =================================================
        // CHECK-IN TRAMITE QR
        // =================================================

        public async Task<BookingOperationResult> CheckInAsync(
            string qrToken,
            string staffUserId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(qrToken))
            {
                return BookingOperationResult.Fail(
                    BookingErrorCode.BookingNotFound,
                    "Il QR code non contiene una prenotazione valida.");
            }

            var booking =
                await _context.Bookings
                    .Include(b => b.GalleryActivity)
                    .FirstOrDefaultAsync(
                        b => b.QrToken == qrToken,
                        cancellationToken);

            if (booking == null)
            {
                return BookingOperationResult.Fail(
                    BookingErrorCode.BookingNotFound,
                    "Prenotazione non trovata.");
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                return BookingOperationResult.Fail(
                    BookingErrorCode.BookingNotConfirmed,
                    "Non è possibile effettuare il check-in: la prenotazione non è confermata.");
            }

            if (booking.CheckedInAt.HasValue)
            {
                return BookingOperationResult.Fail(
                    BookingErrorCode.AlreadyCheckedIn,
                    "Il check-in per questa prenotazione è già stato effettuato.");
            }

            booking.CheckedInAt = UtcNow;
            booking.CheckedInByUserId = staffUserId;

            await _context.SaveChangesAsync(cancellationToken);

            return BookingOperationResult.Ok(
                "Ingresso registrato correttamente.",
                booking);
        }
    }
}
