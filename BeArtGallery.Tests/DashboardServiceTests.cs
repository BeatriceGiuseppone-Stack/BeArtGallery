using BeArtGallery.Models;
using BeArtGallery.Models.Enums;
using BeArtGallery.Services;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Tests
{
    /// <summary>
    /// Test degli indicatori della dashboard: presenze, occupazione,
    /// tassi con denominatore nullo, feedback e insight automatici.
    /// </summary>
    public sealed class DashboardServiceTests : IDisposable
    {
        // "Adesso" per tutti i test: 1 marzo 2026, ore 10:00
        private static readonly DateTime Now = new(2026, 3, 1, 10, 0, 0);

        private readonly TestDatabase _db = new();

        private readonly FixedTimeProvider _clock = new(Now);


        private DashboardService CreateService(
            Data.ApplicationDbContext context)
        {
            return new DashboardService(context, _clock);
        }


        public void Dispose()
        {
            _db.Dispose();
        }


        private void AddFeedback(int bookingId, string userId, int rating)
        {
            using var context = _db.CreateContext();

            context.VisitFeedbacks.Add(new VisitFeedback
            {
                BookingId = bookingId,
                UserId = userId,
                Rating = rating,
                CreatedAt = Now
            });

            context.SaveChanges();
        }


        // =================================================
        // PRESENZE
        // =================================================

        [Fact]
        public async Task AttendanceRate_CountsOnlyConcludedActivities()
        {
            var anna = _db.AddUser("anna");
            var luca = _db.AddUser("luca");
            var sara = _db.AddUser("sara");

            // Attività conclusa ieri: 3 persone entrate, 1 assente.
            var past = _db.AddActivity(Now.AddDays(-1));
            _db.AddBooking(past, anna, participants: 3, checkedInAt: Now.AddDays(-1));
            _db.AddBooking(past, luca, participants: 1);

            // Attività di domani: nessuno può ancora essere entrato.
            var future = _db.AddActivity(Now.AddDays(1));
            _db.AddBooking(future, sara, participants: 5);

            await using var context = _db.CreateContext();
            var model = await CreateService(context).BuildAsync();

            // 3 entrati su 4 attesi nelle sole attività concluse = 75%.
            // Se si contassero anche i 5 di domani risulterebbe 33,3%.
            Assert.Equal(75.0, model.AttendanceRate);
            Assert.Equal(9, model.ExpectedVisitors);
            Assert.Equal(3, model.CheckedInVisitors);
            Assert.Equal(1, model.NoShowVisitors);
            Assert.Equal(5, model.VisitorsToArrive);
        }


        [Fact]
        public async Task AttendanceRate_WithoutConcludedActivities_IsZero()
        {
            var anna = _db.AddUser("anna");
            var future = _db.AddActivity(Now.AddDays(3));
            _db.AddBooking(future, anna, participants: 2);

            await using var context = _db.CreateContext();
            var model = await CreateService(context).BuildAsync();

            Assert.Equal(0, model.AttendanceRate);
            Assert.Equal(0, model.NoShowVisitors);
            Assert.Equal(2, model.VisitorsToArrive);
        }


        [Fact]
        public async Task CancelledBookings_AreNotCountedAsExpectedVisitors()
        {
            var anna = _db.AddUser("anna");
            var luca = _db.AddUser("luca");
            var past = _db.AddActivity(Now.AddDays(-2));

            _db.AddBooking(past, anna, participants: 2, checkedInAt: Now.AddDays(-2));
            _db.AddBooking(past, luca, participants: 4, status: BookingStatus.Cancelled);

            await using var context = _db.CreateContext();
            var model = await CreateService(context).BuildAsync();

            Assert.Equal(2, model.ExpectedVisitors);
            Assert.Equal(0, model.NoShowVisitors);
            Assert.Equal(100.0, model.AttendanceRate);
            Assert.Equal(1, model.ConfirmedBookings);
        }


        // =================================================
        // OCCUPAZIONE DELLE ATTIVITÀ FUTURE
        // =================================================

        [Fact]
        public async Task OccupancyRate_UsesConfirmedBookingsOfScheduledFutureActivities()
        {
            var anna = _db.AddUser("anna");
            var luca = _db.AddUser("luca");
            var sara = _db.AddUser("sara");

            // Attività futura da 10 posti: 4 confermati, 3 annullati.
            var future = _db.AddActivity(Now.AddDays(2), maxParticipants: 10);
            _db.AddBooking(future, anna, participants: 4);
            _db.AddBooking(future, luca, participants: 3, status: BookingStatus.Cancelled);

            // Attività futura annullata: esclusa dal calcolo.
            var cancelled = _db.AddActivity(
                Now.AddDays(4),
                maxParticipants: 10,
                status: GalleryActivityStatus.Cancelled);
            _db.AddBooking(cancelled, sara, participants: 2);

            // Attività già svolta: esclusa dal calcolo.
            var past = _db.AddActivity(Now.AddDays(-3), maxParticipants: 10);
            _db.AddBooking(past, sara, participants: 10);

            await using var context = _db.CreateContext();
            var model = await CreateService(context).BuildAsync();

            Assert.Equal(40.0m, model.OccupancyRate);
            Assert.Equal(1, model.UpcomingActivities);

            var upcoming = Assert.Single(model.UpcomingActivitiesList);
            Assert.Equal(4, upcoming.BookedParticipants);
        }


        [Fact]
        public async Task HighOccupancy_GeneratesParticipationInsight()
        {
            var anna = _db.AddUser("anna");
            var future = _db.AddActivity(Now.AddDays(1), maxParticipants: 10);
            _db.AddBooking(future, anna, participants: 9);

            await using var context = _db.CreateContext();
            var model = await CreateService(context).BuildAsync();

            Assert.Equal(90.0m, model.OccupancyRate);
            Assert.Contains(model.Insights, i => i.Title == "Elevata partecipazione");
        }


        // =================================================
        // DATABASE VUOTO: NESSUNA DIVISIONE PER ZERO
        // =================================================

        [Fact]
        public async Task EmptyDatabase_AllRatesAreZeroAndNoInsights()
        {
            await using var context = _db.CreateContext();
            var model = await CreateService(context).BuildAsync();

            Assert.Equal(0, model.AttendanceRate);
            Assert.Equal(0m, model.OccupancyRate);
            Assert.Equal(0m, model.ViewToFavoriteRate);
            Assert.Equal(0m, model.FavoriteToInquiryRate);
            Assert.Equal(0, model.AverageRating);
            Assert.Empty(model.Insights);

            // Il riepilogo mensile mostra comunque gli ultimi sei mesi.
            Assert.Equal(6, model.BookingsByMonth.Count);
            Assert.All(model.BookingsByMonth, m => Assert.Equal(0, m.Value));
        }


        // =================================================
        // FEEDBACK
        // =================================================

        [Fact]
        public async Task Feedback_AverageAndLowSatisfactionInsight()
        {
            var activity = _db.AddActivity(Now.AddDays(-1));

            foreach (var (userId, rating) in new[] { ("anna", 2), ("luca", 2), ("sara", 3) })
            {
                _db.AddUser(userId);
                var booking = _db.AddBooking(
                    activity, userId, participants: 1, checkedInAt: Now.AddDays(-1));
                AddFeedback(booking.Id, userId, rating);
            }

            await using var context = _db.CreateContext();
            var model = await CreateService(context).BuildAsync();

            Assert.Equal(3, model.TotalFeedbacks);
            Assert.Equal(2.3, model.AverageRating);
            Assert.Equal(3, model.LatestFeedbacks.Count);
            Assert.Contains(model.Insights, i => i.Title == "Soddisfazione da migliorare");
        }


        [Fact]
        public async Task Feedback_SecondFeedbackOnSameBooking_IsRejectedByDatabase()
        {
            var anna = _db.AddUser("anna");
            var activity = _db.AddActivity(Now.AddDays(-1));
            var booking = _db.AddBooking(
                activity, anna, participants: 1, checkedInAt: Now.AddDays(-1));

            AddFeedback(booking.Id, anna, 5);

            // Il vincolo univoco su BookingId blocca il doppio invio,
            // anche se il controllo dell'interfaccia venisse aggirato.
            Assert.Throws<DbUpdateException>(() => AddFeedback(booking.Id, anna, 4));

            await using var check = _db.CreateContext();
            Assert.Equal(1, await check.VisitFeedbacks.CountAsync());
        }


        // =================================================
        // RIEPILOGO MENSILE
        // =================================================

        [Fact]
        public async Task BookingsByMonth_CountsBookingsInTheirMonth()
        {
            var anna = _db.AddUser("anna");
            var luca = _db.AddUser("luca");
            var activity = _db.AddActivity(Now.AddDays(5), maxParticipants: 20);

            _db.AddBooking(activity, anna, participants: 1);
            var older = _db.AddBooking(activity, luca, participants: 1);

            await using (var update = _db.CreateContext())
            {
                var saved = await update.Bookings.SingleAsync(b => b.Id == older.Id);
                saved.BookingDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
                await update.SaveChangesAsync();
            }

            // La prima prenotazione ha la data reale di creazione:
            // la portiamo a marzo 2026 per allinearla all'orologio fisso.
            await using (var update = _db.CreateContext())
            {
                var saved = await update.Bookings.SingleAsync(b => b.UserId == anna);
                saved.BookingDate = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);
                await update.SaveChangesAsync();
            }

            await using var context = _db.CreateContext();
            var model = await CreateService(context).BuildAsync();

            // Sei mesi: da ottobre 2025 a marzo 2026.
            Assert.Equal(6, model.BookingsByMonth.Count);
            Assert.Equal(1, model.BookingsByMonth[3].Value); // gennaio 2026
            Assert.Equal(0, model.BookingsByMonth[4].Value); // febbraio 2026
            Assert.Equal(1, model.BookingsByMonth[5].Value); // marzo 2026
        }
    }
}
