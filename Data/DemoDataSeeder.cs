using BeArtGallery.Authorization;
using BeArtGallery.Models;
using BeArtGallery.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Data
{
    /// <summary>
    /// Popola il database con dati dimostrativi, così il prototipo
    /// è subito utilizzabile da chi lo valuta: tre utenti (uno per
    /// ruolo), artisti, opere, attività, prenotazioni e feedback.
    /// Viene eseguito solo se "DemoData:Enabled" è true
    /// (di default soltanto in appsettings.Development.json).
    /// I dati del catalogo sono inseriti solo se non esiste
    /// ancora nessun artista, quindi non tocca dati reali.
    /// </summary>
    public static class DemoDataSeeder
    {
        public const string AdminEmail = "admin@beartgallery.test";
        public const string StaffEmail = "staff@beartgallery.test";
        public const string VisitorEmail = "visitatore@beartgallery.test";


        public static async Task SeedAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var timeProvider = services.GetRequiredService<TimeProvider>();

            var password = configuration["DemoData:Password"] ?? "Demo123!";


            // =================================================
            // UTENTI DEMO (uno per ruolo)
            // =================================================

            await EnsureUserAsync(userManager, AdminEmail, password, RoleNames.Admin);
            var staff = await EnsureUserAsync(userManager, StaffEmail, password, RoleNames.Staff);
            var visitor = await EnsureUserAsync(userManager, VisitorEmail, password, RoleNames.Visitore);


            // Il catalogo demo viene creato una sola volta.
            if (await context.Artists.AnyAsync())
            {
                return;
            }


            // =================================================
            // ARTISTI E OPERE (nomi e opere di fantasia)
            // =================================================

            var giulia = new Artist
            {
                FirstName = "Giulia",
                LastName = "Ferrante",
                Nationality = "Italiana",
                BirthDate = new DateOnly(1984, 3, 12),
                Biography = "Pittrice contemporanea, lavora su paesaggi urbani e luce artificiale."
            };

            var marco = new Artist
            {
                FirstName = "Marco",
                LastName = "Delvecchio",
                Nationality = "Italiana",
                BirthDate = new DateOnly(1971, 9, 30),
                Biography = "Scultore e incisore, esplora il rapporto tra materia e memoria."
            };

            var ana = new Artist
            {
                FirstName = "Ana",
                LastName = "Ribeiro",
                Nationality = "Portoghese",
                BirthDate = new DateOnly(1990, 6, 5),
                Biography = "Artista visiva, unisce fotografia e pittura in tecnica mista."
            };

            context.Artists.AddRange(giulia, marco, ana);

            context.Artworks.AddRange(
                new Artwork
                {
                    Title = "Notturno sul porto",
                    Year = 2019,
                    Technique = "Olio su tela",
                    Description = "Il porto di notte, illuminato dalle luci delle gru.",
                    WidthCm = 120,
                    HeightCm = 80,
                    Artist = giulia,
                    AvailabilityStatus = ArtworkAvailabilityStatus.Available
                },
                new Artwork
                {
                    Title = "Fermata Centrale",
                    Year = 2021,
                    Technique = "Acrilico su tela",
                    Description = "Una pensilina dell'autobus nel momento di massima attesa.",
                    WidthCm = 90,
                    HeightCm = 90,
                    Artist = giulia,
                    AvailabilityStatus = ArtworkAvailabilityStatus.LimitedAvailability
                },
                new Artwork
                {
                    Title = "Radici",
                    Year = 2015,
                    Technique = "Bronzo",
                    Description = "Scultura che intreccia forme vegetali e frammenti di architettura.",
                    WidthCm = 40,
                    HeightCm = 110,
                    Artist = marco,
                    AvailabilityStatus = ArtworkAvailabilityStatus.Available
                },
                new Artwork
                {
                    Title = "Archivio di famiglia",
                    Year = 2018,
                    Technique = "Acquaforte",
                    Description = "Serie di incisioni ispirate a fotografie d'epoca.",
                    WidthCm = 50,
                    HeightCm = 70,
                    Artist = marco,
                    AvailabilityStatus = ArtworkAvailabilityStatus.SoldOut
                },
                new Artwork
                {
                    Title = "Atlantico",
                    Year = 2023,
                    Technique = "Tecnica mista su carta",
                    Description = "Fotografia dell'oceano reinterpretata con pittura a strati.",
                    WidthCm = 100,
                    HeightCm = 70,
                    Artist = ana,
                    AvailabilityStatus = ArtworkAvailabilityStatus.Available
                });


            // =================================================
            // ATTIVITÀ (date relative a oggi)
            // =================================================

            var today = timeProvider.GetLocalNow().Date;

            var pastVisit = new GalleryActivity
            {
                Title = "Visita guidata: luce e città",
                Description = "Percorso guidato tra le opere di Giulia Ferrante.",
                ActivityType = ActivityType.Visita,
                StartDateTime = today.AddDays(-10).AddHours(10),
                DurationMinutes = 60,
                MaxParticipants = 20,
                Location = "Sala principale"
            };

            var pastEvent = new GalleryActivity
            {
                Title = "Incontro con l'artista: Marco Delvecchio",
                Description = "Dialogo con lo scultore sul tema della memoria.",
                ActivityType = ActivityType.Evento,
                StartDateTime = today.AddDays(-3).AddHours(18),
                DurationMinutes = 90,
                MaxParticipants = 40,
                Location = "Sala conferenze"
            };

            var nextVisit = new GalleryActivity
            {
                Title = "Visita guidata del sabato",
                Description = "Visita guidata alla collezione permanente.",
                ActivityType = ActivityType.Visita,
                StartDateTime = today.AddDays(4).AddHours(11),
                DurationMinutes = 60,
                MaxParticipants = 15,
                Location = "Ingresso galleria"
            };

            var nextEvent = new GalleryActivity
            {
                Title = "Inaugurazione mostra \"Atlantico\"",
                Description = "Vernissage della nuova mostra di Ana Ribeiro.",
                ActivityType = ActivityType.Evento,
                StartDateTime = today.AddDays(12).AddHours(19),
                DurationMinutes = 120,
                MaxParticipants = 60,
                Location = "Sala principale"
            };

            var workshop = new GalleryActivity
            {
                Title = "Laboratorio di incisione",
                Description = "Laboratorio pratico per adulti, materiali inclusi.",
                ActivityType = ActivityType.Evento,
                StartDateTime = today.AddDays(20).AddHours(15),
                DurationMinutes = 180,
                MaxParticipants = 10,
                Location = "Laboratorio"
            };

            context.GalleryActivities.AddRange(
                pastVisit, pastEvent, nextVisit, nextEvent, workshop);


            // =================================================
            // PRENOTAZIONI E FEEDBACK DEL VISITATORE DEMO
            // =================================================

            var utcNow = timeProvider.GetUtcNow().UtcDateTime;

            // Visita passata: check-in effettuato e feedback lasciato.
            var attendedWithFeedback = new Booking
            {
                GalleryActivity = pastVisit,
                UserId = visitor.Id,
                NumberOfParticipants = 2,
                BookingDate = utcNow.AddDays(-15),
                Status = BookingStatus.Confirmed,
                QrToken = Guid.NewGuid().ToString("N"),
                CheckedInAt = utcNow.AddDays(-10),
                CheckedInByUserId = staff.Id
            };

            // Evento passato: check-in effettuato, feedback ancora
            // da lasciare (utile per provare la funzione).
            var attendedWithoutFeedback = new Booking
            {
                GalleryActivity = pastEvent,
                UserId = visitor.Id,
                NumberOfParticipants = 1,
                BookingDate = utcNow.AddDays(-8),
                Status = BookingStatus.Confirmed,
                QrToken = Guid.NewGuid().ToString("N"),
                CheckedInAt = utcNow.AddDays(-3),
                CheckedInByUserId = staff.Id
            };

            // Visita futura: prenotazione confermata con QR.
            var upcoming = new Booking
            {
                GalleryActivity = nextVisit,
                UserId = visitor.Id,
                NumberOfParticipants = 3,
                BookingDate = utcNow.AddDays(-1),
                Status = BookingStatus.Confirmed,
                QrToken = Guid.NewGuid().ToString("N")
            };

            context.Bookings.AddRange(
                attendedWithFeedback,
                attendedWithoutFeedback,
                upcoming);

            context.VisitFeedbacks.Add(new VisitFeedback
            {
                Booking = attendedWithFeedback,
                UserId = visitor.Id,
                Rating = 5,
                Comment = "Guida molto preparata, percorso chiaro e ben organizzato.",
                CreatedAt = utcNow.AddDays(-9)
            });

            await context.SaveChangesAsync();
        }


        private static async Task<ApplicationUser> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Impossibile creare l'utente demo {email}: " +
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }

            // Un solo ruolo per utente, coerente con l'IdentitySeeder.
            var currentRoles = await userManager.GetRolesAsync(user);

            if (!currentRoles.Contains(role))
            {
                if (currentRoles.Count > 0)
                {
                    await userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                await userManager.AddToRoleAsync(user, role);
            }

            return user;
        }
    }
}
