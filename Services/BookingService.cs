using System.Data;
using BeArtGallery.Data;
using BeArtGallery.Models;
using BeArtGallery.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Services
{
    /// <summary>
    /// Possibili errori delle operazioni sulle prenotazioni.
    /// Servono all'API e alle pagine per scegliere il codice HTTP o il messaggio giusto.
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
    /// Esito di un'operazione sulle prenotazioni: riuscita o errore, con il messaggio
    /// </summary>
    public sealed class BookingOperationResult
    {
        public bool Success { get; private init; }

        public BookingErrorCode ErrorCode { get; private init; }

        public string Message { get; private init; } = string.Empty;

        public Booking? Booking { get; private init; }


        // Crea un esito positivo
        public static BookingOperationResult Ok(string message, Booking? booking = null)
        {
            return new BookingOperationResult
            {
                Success = true,
                ErrorCode = BookingErrorCode.None,
                Message = message,
                Booking = booking
            };
        }


        // Crea un esito di errore
        public static BookingOperationResult Fail(BookingErrorCode errorCode, string message)
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
    /// Regole delle prenotazioni: creazione, annullamento da parte del visitatore e check-in
    /// </summary>
    public interface IBookingService
    {
        Task<BookingOperationResult> CreateAsync(string userId, int galleryActivityId,
            int numberOfParticipants, CancellationToken cancellationToken = default);

        Task<BookingOperationResult> CancelByUserAsync(string userId, int bookingId,
            CancellationToken cancellationToken = default);

        Task<BookingOperationResult> CheckInAsync(string qrToken, string staffUserId,
            CancellationToken cancellationToken = default);
    }


    public class BookingService : IBookingService
    {
        // Limiti dei partecipanti per una singola prenotazione
        public const int MinParticipants = 1;

        public const int MaxParticipantsPerBooking = 10;

        private readonly ApplicationDbContext _context;

        private readonly TimeProvider _timeProvider;

        public BookingService(ApplicationDbContext context, TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }

        // Date: gli orari delle attività sono in ora locale della galleria,
        // gli orari degli eventi (prenotazione, annullamento, check-in) sono salvati in UTC

        private DateTime LocalNow => _timeProvider.GetLocalNow().DateTime;

        private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        // Creazione di una prenotazione

        public async Task<BookingOperationResult> CreateAsync( string userId, int galleryActivityId,
            int numberOfParticipants, CancellationToken cancellationToken = default)
        {
            // Il numero di partecipanti deve essere tra 1 e 10
            if (numberOfParticipants < MinParticipants || numberOfParticipants > MaxParticipantsPerBooking)
            {
                return BookingOperationResult.Fail( BookingErrorCode.InvalidParticipants,
                    $"Il numero di partecipanti deve essere compreso tra {MinParticipants} e {MaxParticipantsPerBooking}.");
            }

            // Controllo dei posti e salvataggio avvengono nella stessa transazione SERIALIZABLE:
            // se due persone prenotano insieme gli ultimi posti, il database ne blocca una
            // e non si va oltre la capienza (niente overbooking)

            await using var transaction = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);

            try
            {
                // Cerca l'attività da prenotare
                var activity = await _context.GalleryActivities.FirstOrDefaultAsync(
                            a => a.Id == galleryActivityId, cancellationToken);

                if (activity == null)
                {
                    return BookingOperationResult.Fail( BookingErrorCode.ActivityNotFound, "L'attività selezionata non esiste.");
                }

                // L'attività deve essere programmata, attiva e non ancora iniziata
                if (activity.Status == GalleryActivityStatus.Cancelled)
                {
                    return BookingOperationResult.Fail( BookingErrorCode.ActivityCancelled, "L'attività è stata annullata.");
                }

                if (!activity.IsActive)
                {
                    return BookingOperationResult.Fail( BookingErrorCode.ActivityNotActive, "L'attività non è disponibile.");
                }

                if (activity.StartDateTime <= LocalNow)
                {
                    return BookingOperationResult.Fail( BookingErrorCode.ActivityAlreadyStarted,
                        "Non è possibile prenotare un'attività già iniziata.");
                }

                // Prenotazioni confermate per questa attività
                var confirmedBookings = _context.Bookings.Where(b => b.GalleryActivityId == activity.Id &&
                            b.Status == BookingStatus.Confirmed);

                // Lo stesso utente non può prenotare due volte la stessa attività
                bool alreadyBooked = await confirmedBookings.AnyAsync( b => b.UserId == userId, cancellationToken);

                if (alreadyBooked)
                {
                    return BookingOperationResult.Fail( BookingErrorCode.AlreadyBooked,
                        "Hai già una prenotazione attiva per questa attività.");
                }

                // Calcola i posti ancora liberi
                int bookedParticipants = await confirmedBookings.SumAsync( b => b.NumberOfParticipants, cancellationToken);

                int availablePlaces = Math.Max( 0, activity.MaxParticipants - bookedParticipants);

                if (numberOfParticipants > availablePlaces)
                {
                    return BookingOperationResult.Fail( BookingErrorCode.NotEnoughPlaces,
                        $"Sono disponibili soltanto {availablePlaces} posti.");
                }

                // Crea la prenotazione con un codice casuale per il QR
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

                // Conferma la transazione
                await transaction.CommitAsync(cancellationToken);

                return BookingOperationResult.Ok("Prenotazione effettuata con successo.", booking);
            }
            catch (Exception ex) when ( ex is DbUpdateException || ex is System.Data.Common.DbException)
            {
                // Errore se un'altra prenotazione è arrivata nello stesso momento
                return BookingOperationResult.Fail( BookingErrorCode.ConcurrencyConflict,
                    "Un'altra prenotazione è stata registrata nello stesso momento. Riprova.");
            }
        }

        // Annullamento da parte del visitatore
        public async Task<BookingOperationResult> CancelByUserAsync( string userId,
            int bookingId, CancellationToken cancellationToken = default)
        {
            // La prenotazione deve appartenere all'utente
            var booking =  await _context.Bookings.Include(b => b.GalleryActivity)
                    .FirstOrDefaultAsync( b => b.Id == bookingId && b.UserId == userId, cancellationToken);

            if (booking == null)
            {
                return BookingOperationResult.Fail( BookingErrorCode.BookingNotFound, "Prenotazione non trovata.");
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return BookingOperationResult.Fail( BookingErrorCode.BookingNotConfirmed, "La prenotazione è già stata annullata.");
            }

            if (booking.GalleryActivity == null)
            {
                return BookingOperationResult.Fail( BookingErrorCode.ActivityNotFound, "Attività associata non disponibile.");
            }

            // Non si può annullare se l'attività è già iniziata
            if (booking.GalleryActivity.StartDateTime <= LocalNow)
            {
                return BookingOperationResult.Fail( BookingErrorCode.ActivityAlreadyStarted,
                    "Non è possibile annullare una prenotazione per un'attività già iniziata o conclusa.");
            }

            // Segna la prenotazione come annullata dall'utente
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = UtcNow;
            booking.CancelledByUserId = userId;
            booking.CancellationType = BookingCancellationType.User;
            booking.CancellationReason = null;

            await _context.SaveChangesAsync(cancellationToken);

            return BookingOperationResult.Ok( "Prenotazione annullata correttamente.", booking);
        }


        // Check-in con il QR code
        public async Task<BookingOperationResult> CheckInAsync( string qrToken, string staffUserId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(qrToken))
            {
                return BookingOperationResult.Fail( BookingErrorCode.BookingNotFound,
                    "Il QR code non contiene una prenotazione valida.");
            }

            // Cerca la prenotazione con il codice del QR
            var booking = await _context.Bookings.Include(b => b.GalleryActivity)
                    .FirstOrDefaultAsync( b => b.QrToken == qrToken, cancellationToken);

            if (booking == null)
            {
                return BookingOperationResult.Fail( BookingErrorCode.BookingNotFound, "Prenotazione non trovata.");
            }

            // Il check-in vale solo per prenotazioni confermate e si fa una volta sola
            if (booking.Status != BookingStatus.Confirmed)
            {
                return BookingOperationResult.Fail( BookingErrorCode.BookingNotConfirmed,
                    "Non è possibile effettuare il check-in: la prenotazione non è confermata.");
            }

            if (booking.CheckedInAt.HasValue)
            {
                return BookingOperationResult.Fail( BookingErrorCode.AlreadyCheckedIn,
                    "Il check-in per questa prenotazione è già stato effettuato.");
            }

            // Registra ora dell'ingresso e operatore
            booking.CheckedInAt = UtcNow;
            booking.CheckedInByUserId = staffUserId;

            await _context.SaveChangesAsync(cancellationToken);

            return BookingOperationResult.Ok( "Ingresso registrato correttamente.", booking);
        }
    }
}