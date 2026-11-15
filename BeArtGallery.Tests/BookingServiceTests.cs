using BeArtGallery.Models.Enums;
using BeArtGallery.Services;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Tests
{
    /// <summary>
    /// Test delle regole delle prenotazioni:
    /// posti disponibili, doppie prenotazioni, annullamento e check-in
    /// </summary>
    public sealed class BookingServiceTests : IDisposable
    {
        // Data e ora "attuali" per tutti i test: 1 marzo 2026, ore 10:00
        private static readonly DateTime Now = new(2026, 3, 1, 10, 0, 0);

        private readonly TestDatabase _db = new();

        private readonly FixedTimeProvider _clock = new(Now);


        private BookingService CreateService(
            Data.ApplicationDbContext context)
        {
            return new BookingService(context, _clock);
        }


        // Alla fine di ogni test si cancella il database
        public void Dispose()
        {
            _db.Dispose();
        }


        // Creazione

        // Prenotazione con posti liberi: confermata e con codice QR
        [Fact]
        public async Task Create_WithAvailablePlaces_CreatesConfirmedBookingWithQr()
        {
            var user = _db.AddUser("visitatore");
            var activityId = _db.AddActivity(Now.AddDays(2), maxParticipants: 10);

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CreateAsync(user, activityId, 3);

            Assert.True(result.Success);

            await using var check = _db.CreateContext();
            var saved = await check.Bookings.SingleAsync();

            Assert.Equal(BookingStatus.Confirmed, saved.Status);
            Assert.Equal(3, saved.NumberOfParticipants);
            Assert.False(string.IsNullOrWhiteSpace(saved.QrToken));
        }


        // Più persone dei posti liberi: rifiutata
        [Fact]
        public async Task Create_MoreParticipantsThanAvailable_IsRejected()
        {
            var first = _db.AddUser("primo");
            var second = _db.AddUser("secondo");
            var activityId = _db.AddActivity(Now.AddDays(2), maxParticipants: 5);

            _db.AddBooking(activityId, first, participants: 4);

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CreateAsync(second, activityId, 2);

            Assert.False(result.Success);
            Assert.Equal(BookingErrorCode.NotEnoughPlaces, result.ErrorCode);
            Assert.Contains("1 posti", result.Message);
        }


        // Esattamente i posti rimasti: accettata
        [Fact]
        public async Task Create_ExactlyRemainingPlaces_IsAccepted()
        {
            var first = _db.AddUser("primo");
            var second = _db.AddUser("secondo");
            var activityId = _db.AddActivity(Now.AddDays(2), maxParticipants: 5);

            _db.AddBooking(activityId, first, participants: 3);

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CreateAsync(second, activityId, 2);

            Assert.True(result.Success);
        }


        // Le prenotazioni annullate non occupano posti
        [Fact]
        public async Task Create_CancelledBookingsDoNotOccupyPlaces()
        {
            var first = _db.AddUser("primo");
            var second = _db.AddUser("secondo");
            var activityId = _db.AddActivity(Now.AddDays(2), maxParticipants: 4);

            _db.AddBooking(activityId, first, participants: 4, status: BookingStatus.Cancelled);

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CreateAsync(second, activityId, 4);

            Assert.True(result.Success);
        }


        // Lo stesso utente non può prenotare due volte
        [Fact]
        public async Task Create_SameUserTwice_IsRejected()
        {
            var user = _db.AddUser("visitatore");
            var activityId = _db.AddActivity(Now.AddDays(2));

            _db.AddBooking(activityId, user, participants: 1);

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CreateAsync(user, activityId, 1);

            Assert.False(result.Success);
            Assert.Equal(BookingErrorCode.AlreadyBooked, result.ErrorCode);
        }


        // Attività già iniziata: rifiutata
        [Fact]
        public async Task Create_ActivityAlreadyStarted_IsRejected()
        {
            var user = _db.AddUser("visitatore");
            var activityId = _db.AddActivity(Now.AddMinutes(-5));

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CreateAsync(user, activityId, 1);

            Assert.False(result.Success);
            Assert.Equal(BookingErrorCode.ActivityAlreadyStarted, result.ErrorCode);
        }


        // Attività annullata: rifiutata
        [Fact]
        public async Task Create_CancelledActivity_IsRejected()
        {
            var user = _db.AddUser("visitatore");
            var activityId = _db.AddActivity(
                Now.AddDays(2),
                status: GalleryActivityStatus.Cancelled);

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CreateAsync(user, activityId, 1);

            Assert.False(result.Success);
            Assert.Equal(BookingErrorCode.ActivityCancelled, result.ErrorCode);
        }


        // Attività che non esiste: errore "non trovata"
        [Fact]
        public async Task Create_UnknownActivity_ReturnsNotFound()
        {
            var user = _db.AddUser("visitatore");

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CreateAsync(user, 999, 1);

            Assert.False(result.Success);
            Assert.Equal(BookingErrorCode.ActivityNotFound, result.ErrorCode);
        }


        // Numero di partecipanti fuori dai limiti (0 e 11): rifiutato
        [Theory]
        [InlineData(0)]
        [InlineData(11)]
        public async Task Create_InvalidNumberOfParticipants_IsRejected(int participants)
        {
            var user = _db.AddUser("visitatore");
            var activityId = _db.AddActivity(Now.AddDays(2), maxParticipants: 50);

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CreateAsync(user, activityId, participants);

            Assert.False(result.Success);
            Assert.Equal(BookingErrorCode.InvalidParticipants, result.ErrorCode);

            await using var check = _db.CreateContext();
            Assert.Equal(0, await check.Bookings.CountAsync());
        }


        // Annullamento

        // Il titolare annulla prima dell'inizio: prenotazione annullata
        [Fact]
        public async Task Cancel_ByOwnerBeforeStart_MarksBookingAsCancelled()
        {
            var user = _db.AddUser("visitatore");
            var activityId = _db.AddActivity(Now.AddDays(1));
            var booking = _db.AddBooking(activityId, user, participants: 2);

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CancelByUserAsync(user, booking.Id);

            Assert.True(result.Success);

            await using var check = _db.CreateContext();
            var saved = await check.Bookings.SingleAsync();

            Assert.Equal(BookingStatus.Cancelled, saved.Status);
            Assert.Equal(BookingCancellationType.User, saved.CancellationType);
            Assert.NotNull(saved.CancelledAt);
        }


        // Non si può annullare la prenotazione di un altro utente
        [Fact]
        public async Task Cancel_BookingOfAnotherUser_ReturnsNotFound()
        {
            var owner = _db.AddUser("proprietario");
            var other = _db.AddUser("altro");
            var activityId = _db.AddActivity(Now.AddDays(1));
            var booking = _db.AddBooking(activityId, owner, participants: 1);

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CancelByUserAsync(other, booking.Id);

            Assert.False(result.Success);
            Assert.Equal(BookingErrorCode.BookingNotFound, result.ErrorCode);
        }


        // Dopo l'inizio dell'attività non si può annullare
        [Fact]
        public async Task Cancel_AfterActivityStarted_IsRejected()
        {
            var user = _db.AddUser("visitatore");
            var activityId = _db.AddActivity(Now.AddHours(-1));
            var booking = _db.AddBooking(activityId, user, participants: 1);

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CancelByUserAsync(user, booking.Id);

            Assert.False(result.Success);
            Assert.Equal(BookingErrorCode.ActivityAlreadyStarted, result.ErrorCode);
        }


        // Check-in

        // QR valido: registra l'ingresso e l'operatore
        [Fact]
        public async Task CheckIn_ValidQr_RegistersEntryAndStaffMember()
        {
            var user = _db.AddUser("visitatore");
            var staff = _db.AddUser("staff");
            var activityId = _db.AddActivity(Now.AddMinutes(30));
            var booking = _db.AddBooking(activityId, user, participants: 2);

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CheckInAsync(booking.QrToken!, staff);

            Assert.True(result.Success);

            await using var check = _db.CreateContext();
            var saved = await check.Bookings.SingleAsync();

            Assert.Equal(Now, saved.CheckedInAt!.Value);
            Assert.Equal(staff, saved.CheckedInByUserId);
        }


        // Lo stesso QR non si può usare due volte
        [Fact]
        public async Task CheckIn_SameQrTwice_IsRejected()
        {
            var user = _db.AddUser("visitatore");
            var staff = _db.AddUser("staff");
            var activityId = _db.AddActivity(Now.AddMinutes(30));
            var booking = _db.AddBooking(
                activityId,
                user,
                participants: 1,
                checkedInAt: Now.AddMinutes(-10));

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CheckInAsync(booking.QrToken!, staff);

            Assert.False(result.Success);
            Assert.Equal(BookingErrorCode.AlreadyCheckedIn, result.ErrorCode);
        }


        // Prenotazione annullata: check-in rifiutato
        [Fact]
        public async Task CheckIn_CancelledBooking_IsRejected()
        {
            var user = _db.AddUser("visitatore");
            var staff = _db.AddUser("staff");
            var activityId = _db.AddActivity(Now.AddMinutes(30));
            var booking = _db.AddBooking(
                activityId,
                user,
                participants: 1,
                status: BookingStatus.Cancelled);

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CheckInAsync(booking.QrToken!, staff);

            Assert.False(result.Success);
            Assert.Equal(BookingErrorCode.BookingNotConfirmed, result.ErrorCode);
        }


        // QR sconosciuto: errore "non trovata"
        [Fact]
        public async Task CheckIn_UnknownQr_ReturnsNotFound()
        {
            var staff = _db.AddUser("staff");

            await using var context = _db.CreateContext();

            var result = await CreateService(context)
                .CheckInAsync("token-inesistente", staff);

            Assert.False(result.Success);
            Assert.Equal(BookingErrorCode.BookingNotFound, result.ErrorCode);
        }
    }
}
