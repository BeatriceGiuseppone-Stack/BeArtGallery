using BeArtGallery.Authorization;
using BeArtGallery.Models;
using BeArtGallery.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Data
{
    /// <summary>
    /// Inserisce dati di esempio per provare subito il sito:
    /// un utente per ruolo, artisti, opere, attività, prenotazioni e feedback.
    /// Parte solo se "DemoData:Enabled" è true (in appsettings.Development.json)
    /// e il catalogo viene creato solo se non c'è ancora nessun artista.
    /// </summary>
    public static class DemoDataSeeder
    {
        // Email degli utenti di prova
        public const string AdminEmail = "admin@beartgallery.test";
        public const string StaffEmail = "staff@beartgallery.test";
        public const string VisitorEmail = "visitatore@beartgallery.test";

        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            // Servizi necessari
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var timeProvider = services.GetRequiredService<TimeProvider>();

            // Password degli utenti di prova
            var password = configuration["DemoData:Password"] ?? "Admin1!";

            // Utenti di prova (uno per ruolo)

            await EnsureUserAsync(userManager, AdminEmail, password, RoleNames.Admin);
            var staff = await EnsureUserAsync(userManager, StaffEmail, password, RoleNames.Staff);
            var visitor = await EnsureUserAsync(userManager, VisitorEmail, password, RoleNames.Visitore);

            // Il catalogo di prova si crea una sola volta
            if (await context.Artists.AnyAsync())
            {
                return;
            }

            // Artisti e opere di prova (le immagini sono in wwwroot/images/artworks)

            var elena = new Artist
            {
                FirstName = "Elena",
                LastName = "Marchetti",
                Nationality = "Italiana",
                BirthDate = new DateOnly(1984, 3, 12),
                Biography = "Nata a Chioggia, vive e lavora a Venezia. Dopo il diploma all'Accademia di Belle Arti di Venezia si è dedicata alla pittura a olio su lino grezzo, con velature sottili e quasi trasparenti. I suoi quadri partono dalla laguna all'alba e al tramonto, ma la riducono a fasce di colore e luce al confine con l'astrazione."
            };

            var tomas = new Artist
            {
                FirstName = "Tomás",
                LastName = "Herrera",
                Nationality = "Spagnola",
                BirthDate = new DateOnly(1979, 8, 2),
                Biography = "Scultore nato a Bilbao, cresciuto tra i cantieri navali della città. Lavora con ferro corten e legno di recupero, unendo materiali industriali e naturali in forme sospese che sembrano sempre sul punto di cadere. Le sue opere sono state esposte in Spagna, Francia e Italia."
            };

            var aiko = new Artist
            {
                FirstName = "Aiko",
                LastName = "Moriyama",
                Nationality = "Giapponese",
                BirthDate = new DateOnly(1990, 1, 25),
                Biography = "Artista di Kyoto, formata nella calligrafia tradizionale giapponese prima di trasferirsi a Milano nel 2016. Usa inchiostro sumi e foglia d'oro su carta washi fatta a mano: pochi gesti rapidi, molto spazio vuoto. Il suo lavoro racconta il paesaggio come ricordo più che come veduta."
            };

            var luca = new Artist
            {
                FirstName = "Luca",
                LastName = "Bertoni",
                Nationality = "Italiana",
                BirthDate = new DateOnly(1976, 6, 30),
                Biography = "Fotografo milanese, lavora da vent'anni sull'architettura industriale della pianura padana: silos, fornaci, capannoni abbandonati. Scatta all'alba o con la nebbia, in luce piena e senza persone, e stampa in tirature limitate su carta cotone. Le sue serie sono una mappa di un paesaggio che sta scomparendo."
            };

            var nadia = new Artist
            {
                FirstName = "Nadia",
                LastName = "Okafor",
                Nationality = "Britannica",
                BirthDate = new DateOnly(1988, 11, 9),
                Biography = "Artista londinese di origine nigeriana, laureata alla Central Saint Martins. Unisce tessuti wax africani, cucito e pittura acrilica in grandi tele a tecnica mista. Al centro del suo lavoro ci sono la memoria familiare, la migrazione e l'idea di casa tra due culture."
            };

            var jonas = new Artist
            {
                FirstName = "Jonas",
                LastName = "Lindqvist",
                Nationality = "Svedese",
                BirthDate = new DateOnly(1981, 4, 17),
                Biography = "Ceramista nato a Umeå, nel nord della Svezia, con studio a Stoccolma. Realizza vasi scultorei e piccole installazioni in gres, cotti ad alta temperatura con smalti che ricordano il ghiaccio, la roccia e la neve. Ogni pezzo è unico e porta i segni della lavorazione a mano."
            };

            // Salva artisti e opere
            context.Artists.AddRange(elena, tomas, aiko, luca, nadia, jonas);

            context.Artworks.AddRange(
                new Artwork
                {
                    Title = "Laguna, ore sei",
                    Year = 2024,
                    Technique = "Olio su lino",
                    Description = "La laguna di Chioggia alle sei del mattino, quando acqua e cielo hanno quasi lo stesso colore. Marchetti stende decine di velature sottili di olio su lino grezzo, lasciando affiorare la trama della tela. L'orizzonte è solo una linea più chiara: il resto è luce rosata e grigio perla. È l'opera più grande della serie Maree.",
                    WidthCm = 120,
                    HeightCm = 90,
                    Artist = elena,
                    AvailabilityStatus = ArtworkAvailabilityStatus.Available,
                    IsOnDisplay = true,
                    ImageUrl = "/images/artworks/9cd20632890349e8821c1236e25018e1.png"
                },
                new Artwork
                {
                    Title = "Velature d'inverno",
                    Year = 2025,
                    Technique = "Olio su lino",
                    Description = "Un piccolo formato verticale dipinto durante l'inverno 2025, quando la nebbia copre la laguna per giorni. I colori si riducono al bianco, all'ocra e al grigio, e la forma di una bricola emerge appena dalla foschia. L'opera è già stata acquistata da un collezionista privato.",
                    WidthCm = 60,
                    HeightCm = 80,
                    Artist = elena,
                    AvailabilityStatus = ArtworkAvailabilityStatus.SoldOut,
                    IsOnDisplay = true,
                    ImageUrl = "/images/artworks/f6609b8589cc4307b5f021a99fd9c94e.png"
                },
                new Artwork
                {
                    Title = "Equilibrio instabile",
                    Year = 2023,
                    Technique = "Ferro corten e legno di ulivo",
                    Description = "Una lastra di ferro corten piegata a mano sostiene un ramo di ulivo recuperato da un uliveto abbattuto in Andalucía. Le due parti si toccano in un solo punto e sembrano sempre sul punto di separarsi. La ruggine del metallo e le venature del legno cambiano con la luce della sala. Scultura da appoggio, alta 110 cm.",
                    WidthCm = 45,
                    HeightCm = 110,
                    Artist = tomas,
                    AvailabilityStatus = ArtworkAvailabilityStatus.Available,
                    IsOnDisplay = true,
                    ImageUrl = "/images/artworks/ea05e27377754f2eb6e172795ef3e69c.png"
                },
                new Artwork
                {
                    Title = "Il respiro della montagna",
                    Year = 2025,
                    Technique = "Inchiostro sumi e foglia d'oro su carta washi",
                    Description = "Pochi tratti di inchiostro sumi disegnano il profilo di una montagna, lasciando vuota gran parte del foglio. Una sottile linea di foglia d'oro segna il punto in cui la nebbia si apre. Moriyama ha dipinto l'opera in un solo gesto, senza correzioni, su carta washi fatta a mano in Giappone.",
                    WidthCm = 70,
                    HeightCm = 100,
                    Artist = aiko,
                    AvailabilityStatus = ArtworkAvailabilityStatus.Available,
                    IsOnDisplay = true,
                    ImageUrl = "/images/artworks/86d8620a0581423994c8eec43039d2d0.png"
                },
                new Artwork
                {
                    Title = "Silos #3",
                    Year = 2022,
                    Technique = "Stampa fine art a pigmenti su carta cotone, edizione di 7",
                    Description = "Tre silos di cemento nella campagna di Cremona, fotografati all'alba in una giornata di nebbia. Fa parte della serie Pianura, dedicata agli edifici agricoli e industriali dismessi della Lombardia. Stampa a pigmenti su carta cotone, in edizione di 7 esemplari più 2 prove d'artista: in galleria ne restano pochi.",
                    WidthCm = 100,
                    HeightCm = 70,
                    Artist = luca,
                    AvailabilityStatus = ArtworkAvailabilityStatus.LimitedAvailability,
                    IsOnDisplay = true,
                    ImageUrl = "/images/artworks/747e5a61ba324ef6a533ac0fa95f7fd3.png"
                },
                new Artwork
                {
                    Title = "Trame di casa",
                    Year = 2024,
                    Technique = "Tecnica mista: tessuto wax, filo e acrilico su tela",
                    Description = "Una grande tela quadrata in cui ritagli di tessuto wax portati dalla Nigeria dalla madre dell'artista sono cuciti e poi ridipinti con l'acrilico. I motivi geometrici africani si mescolano con la pianta di una casa londinese tracciata a filo. L'opera parla di due case e di come i ricordi si cuciono insieme.",
                    WidthCm = 150,
                    HeightCm = 150,
                    Artist = nadia,
                    AvailabilityStatus = ArtworkAvailabilityStatus.Available,
                    IsOnDisplay = true,
                    ImageUrl = "/images/artworks/cce7da4884824848b8b2e213821e2c15.png"
                },
                new Artwork
                {
                    Title = "Serie Nord, n. 12",
                    Year = 2026,
                    Technique = "Gres smaltato, cottura ad alta temperatura",
                    Description = "Un vaso scultoreo in gres modellato a mano, con una superficie che ricorda la roccia coperta di ghiaccio. Lo smalto bianco e azzurro è colato durante la cottura a 1280 °C, quindi ogni pezzo della serie è diverso. Non è pensato per i fiori: è un oggetto da guardare.",
                    WidthCm = 30,
                    HeightCm = 42,
                    Artist = jonas,
                    AvailabilityStatus = ArtworkAvailabilityStatus.Available,
                    IsOnDisplay = false,
                    ImageUrl = "/images/artworks/02c22d0fbdb9476580da7233d663fb95.png"
                });


            // Attività di prova, con date calcolate a partire da oggi

            var today = timeProvider.GetLocalNow().Date;

            var pastVisit = new GalleryActivity
            {
                Title = "Visita guidata: la laguna di Elena Marchetti",
                Description = "Percorso guidato tra i dipinti della serie Maree.",
                ActivityType = ActivityType.Visita,
                StartDateTime = today.AddDays(-10).AddHours(10),
                DurationMinutes = 60,
                MaxParticipants = 20,
                Location = "Sala principale"
            };

            var pastEvent = new GalleryActivity
            {
                Title = "Incontro con l'artista: Tomás Herrera",
                Description = "Dialogo con lo scultore su ferro, legno e materiali di recupero.",
                ActivityType = ActivityType.Evento,
                StartDateTime = today.AddDays(-3).AddHours(18),
                DurationMinutes = 90,
                MaxParticipants = 40,
                Location = "Sala conferenze"
            };

            var nextVisit = new GalleryActivity
            {
                Title = "Visita guidata del sabato",
                Description = "Visita guidata alle opere in sala.",
                ActivityType = ActivityType.Visita,
                StartDateTime = today.AddDays(4).AddHours(11),
                DurationMinutes = 60,
                MaxParticipants = 15,
                Location = "Ingresso galleria"
            };

            var nextEvent = new GalleryActivity
            {
                Title = "Inaugurazione mostra \"Pianura\"",
                Description = "Vernissage delle fotografie di Luca Bertoni.",
                ActivityType = ActivityType.Evento,
                StartDateTime = today.AddDays(12).AddHours(19),
                DurationMinutes = 120,
                MaxParticipants = 60,
                Location = "Sala principale"
            };

            var workshop = new GalleryActivity
            {
                Title = "Laboratorio di inchiostro sumi",
                Description = "Laboratorio pratico con Aiko Moriyama per adulti, materiali inclusi.",
                ActivityType = ActivityType.Evento,
                StartDateTime = today.AddDays(20).AddHours(15),
                DurationMinutes = 180,
                MaxParticipants = 10,
                Location = "Laboratorio"
            };

            // Salva le attività
            context.GalleryActivities.AddRange( pastVisit, pastEvent, nextVisit, nextEvent, workshop);

            // Prenotazioni e feedback del visitatore di prova
            var utcNow = timeProvider.GetUtcNow().UtcDateTime;

            // Visita passata: check-in fatto e feedback lasciato
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

            // Evento passato: check-in fatto, feedback ancora da lasciare
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

            // Visita futura: prenotazione confermata con QR
            var upcoming = new Booking
            {
                GalleryActivity = nextVisit,
                UserId = visitor.Id,
                NumberOfParticipants = 3,
                BookingDate = utcNow.AddDays(-1),
                Status = BookingStatus.Confirmed,
                QrToken = Guid.NewGuid().ToString("N")
            };

            context.Bookings.AddRange(attendedWithFeedback, attendedWithoutFeedback, upcoming);

            context.VisitFeedbacks.Add(new VisitFeedback
            {
                Booking = attendedWithFeedback,
                UserId = visitor.Id,
                Rating = 5,
                Comment = "Guida molto preparata, percorso chiaro e ben organizzato.",
                CreatedAt = utcNow.AddDays(-9)
            });

            // Salva tutto
            await context.SaveChangesAsync();
        }

        // Crea l'utente se non esiste e gli assegna il ruolo
        private static async Task<ApplicationUser> EnsureUserAsync( UserManager<ApplicationUser> userManager,
            string email, string password, string role)
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
                    throw new InvalidOperationException( $"Impossibile creare l'utente demo {email}: " +
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }

            // Ogni utente ha un solo ruolo
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