using BeArtGallery.Data;
using BeArtGallery.Models;
using BeArtGallery.Models.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Tests
{
    /// <summary>
    /// Database SQLite in memoria, nuovo per ogni test.
    /// La connessione resta aperta per tutto il test, altrimenti SQLite cancella il database.
    /// </summary>
    public sealed class TestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;

        public TestDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            using var context = CreateContext();
            context.Database.EnsureCreated();
        }


        // Crea un contesto collegato al database di test
        public ApplicationDbContext CreateContext()
        {
            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite(_connection)
                    .Options;

            return new ApplicationDbContext(options);
        }


        // Aggiunge un utente di prova
        public string AddUser(string id)
        {
            using var context = CreateContext();

            context.Users.Add(new ApplicationUser
            {
                Id = id,
                UserName = $"{id}@test.local",
                Email = $"{id}@test.local"
            });

            context.SaveChanges();

            return id;
        }


        // Aggiunge un'attività di prova
        public int AddActivity(
            DateTime startDateTime,
            int maxParticipants = 10,
            GalleryActivityStatus status = GalleryActivityStatus.Scheduled,
            bool isActive = true)
        {
            using var context = CreateContext();

            var activity = new GalleryActivity
            {
                Title = "Evento di prova",
                ActivityType = ActivityType.Evento,
                StartDateTime = startDateTime,
                DurationMinutes = 60,
                MaxParticipants = maxParticipants,
                Status = status,
                IsActive = isActive
            };

            context.GalleryActivities.Add(activity);
            context.SaveChanges();

            return activity.Id;
        }


        // Aggiunge una prenotazione di prova
        public Booking AddBooking(
            int activityId,
            string userId,
            int participants,
            BookingStatus status = BookingStatus.Confirmed,
            DateTime? checkedInAt = null)
        {
            using var context = CreateContext();

            var booking = new Booking
            {
                GalleryActivityId = activityId,
                UserId = userId,
                NumberOfParticipants = participants,
                Status = status,
                QrToken = Guid.NewGuid().ToString("N"),
                CheckedInAt = checkedInAt
            };

            context.Bookings.Add(booking);
            context.SaveChanges();

            return booking;
        }


        // Chiude la connessione e cancella il database
        public void Dispose()
        {
            _connection.Dispose();
        }
    }


    /// <summary>
    /// Orologio fermo: così i test non dipendono dall'ora reale.
    /// Come fuso "locale" si usa UTC.
    /// </summary>
    public sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(
                DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
