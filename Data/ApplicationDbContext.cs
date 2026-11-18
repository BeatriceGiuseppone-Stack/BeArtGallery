using BeArtGallery.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Data
{
    // Collegamento tra le classi del progetto e le tabelle del database.
    // Comprende anche le tabelle degli utenti di Identity.
    public class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        // Tabelle del database
        public DbSet<Artist> Artists { get; set; }

        public DbSet<Artwork> Artworks { get; set; }

        public DbSet<GalleryActivity> GalleryActivities { get; set; }

        public DbSet<Booking> Bookings { get; set; }

        public DbSet<ArtworkFavorite> ArtworkFavorites { get; set; }

        public DbSet<ArtworkInquiry> ArtworkInquiries { get; set; }

        public DbSet<ArtworkInquiryMessage> ArtworkInquiryMessages { get; set; }

        public DbSet<ArtworkView> ArtworkViews { get; set; }

        public DbSet<ArtworkAudioGuide> ArtworkAudioGuides { get; set; }

        public DbSet<ArtworkQrScan> ArtworkQrScans { get; set; }

        public DbSet<VisitFeedback> VisitFeedbacks { get; set; }

        // Regole delle relazioni tra le tabelle
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Prenotazioni: l'utente non si può eliminare se ha prenotazioni
            builder.Entity<Booking>().HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Restrict);

            // Chi ha annullato la prenotazione
            builder.Entity<Booking>().HasOne(b => b.CancelledByUser).WithMany().HasForeignKey(b => b.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Chi ha annullato l'attività
            builder.Entity<GalleryActivity>().HasOne(a => a.CancelledByUser).WithMany().HasForeignKey(a => a.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Preferiti: si cancellano insieme all'opera o all'utente

            builder.Entity<ArtworkFavorite>().HasOne(f => f.Artwork).WithMany(a => a.Favorites).HasForeignKey(f => f.ArtworkId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ArtworkFavorite>().HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);

            // Un utente non può avere due volte la stessa opera nei preferiti

            builder.Entity<ArtworkFavorite>().HasIndex(f => new
                {
                    f.UserId,
                    f.ArtworkId
                }).IsUnique();

            // Richieste di informazioni
            builder.Entity<ArtworkInquiry>().HasOne(i => i.Artwork).WithMany(a => a.Inquiries).HasForeignKey(i => i.ArtworkId)
                // Se l'opera viene eliminata la richiesta resta, senza collegamento
                .OnDelete(DeleteBehavior.SetNull);

            // Se l'utente viene eliminato la richiesta resta, senza utente
            builder.Entity<ArtworkInquiry>().HasOne(i => i.User).WithMany().HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.SetNull);

            // I messaggi si cancellano insieme alla loro richiesta
            builder.Entity<ArtworkInquiryMessage>().HasOne(m => m.ArtworkInquiry).WithMany(i => i.Messages)
                .HasForeignKey(m => m.ArtworkInquiryId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ArtworkInquiryMessage>().HasOne(m => m.SenderUser).WithMany().HasForeignKey(m => m.SenderUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indici per cercare più velocemente richieste e messaggi
            builder.Entity<ArtworkInquiry>().HasIndex(i => i.UserId);

            builder.Entity<ArtworkInquiryMessage>().HasIndex(m => new
                {
                    m.ArtworkInquiryId,
                    m.SenderType,
                    m.ReadAt
                });

            // Visualizzazioni delle opere: si cancellano insieme all'opera
            builder.Entity<ArtworkView>().HasOne(v => v.Artwork).WithMany(a => a.Views).HasForeignKey(v => v.ArtworkId)
    .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ArtworkView>().HasOne(v => v.User).WithMany().HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.SetNull);

            // Indici per le statistiche e per il controllo delle visite ripetute
            builder.Entity<ArtworkView>()
                .HasIndex(v => new
                {
                    v.ArtworkId,
                    v.ViewedAt
                });

            builder.Entity<ArtworkView>()
                .HasIndex(v => new
                {
                    v.SessionId,
                    v.ArtworkId
                });

            // Audioguide: si cancellano insieme all'opera
            builder.Entity<ArtworkAudioGuide>().HasOne(a => a.Artwork).WithMany(a => a.AudioGuides).HasForeignKey(a => a.ArtworkId)
    .OnDelete(DeleteBehavior.Cascade);

            // Al massimo un'audioguida per lingua per ogni opera
            builder.Entity<ArtworkAudioGuide>()
                .HasIndex(a => new
                {
                    a.ArtworkId,
                    a.LanguageCode
                })
                .IsUnique();

            // Scansioni dei QR delle opere
            builder.Entity<ArtworkQrScan>().HasOne(s => s.Artwork).WithMany(a => a.QrScans).HasForeignKey(s => s.ArtworkId)
    .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ArtworkQrScan>().HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ArtworkQrScan>()
                .HasIndex(s => new
                {
                    s.ArtworkId,
                    s.ScannedAt
                });

            // Operatore che ha registrato il check-in
            builder.Entity<Booking>().HasOne(b => b.CheckedInByUser).WithMany().HasForeignKey(b => b.CheckedInByUserId).OnDelete(DeleteBehavior.SetNull);

            // Il codice del QR deve essere unico
            builder.Entity<Booking>().HasIndex(b => b.QrToken).IsUnique().HasFilter("[QrToken] IS NOT NULL");

            // Feedback degli eventi

            // Relazione uno a uno: un solo feedback per prenotazione

            builder.Entity<VisitFeedback>().HasOne(f => f.Booking).WithOne(b => b.Feedback).HasForeignKey<VisitFeedback>(f => f.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VisitFeedback>().HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indice per ordinare i feedback per data
            builder.Entity<VisitFeedback>().HasIndex(f => f.CreatedAt);
        }
    }
}