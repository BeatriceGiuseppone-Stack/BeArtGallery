using BeArtGallery.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Data
{
    public class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(b => b.CancelledByUser)
                .WithMany()
                .HasForeignKey(b => b.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GalleryActivity>()
                .HasOne(a => a.CancelledByUser)
                .WithMany()
                .HasForeignKey(a => a.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------------------------
            // PREFERITI OPERE
            // -------------------------------------------------

            builder.Entity<ArtworkFavorite>()
                .HasOne(f => f.Artwork)
                .WithMany(a => a.Favorites)
                .HasForeignKey(f => f.ArtworkId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<ArtworkFavorite>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // Un utente non può aggiungere due volte
            // la stessa opera ai preferiti.

            builder.Entity<ArtworkFavorite>()
                .HasIndex(f => new
                {
                    f.UserId,
                    f.ArtworkId
                })
                .IsUnique();

            builder.Entity<ArtworkInquiry>()
                .HasOne(i => i.Artwork)
                .WithMany(a => a.Inquiries)
                .HasForeignKey(i => i.ArtworkId)
                // Eliminando l'opera la richiesta resta, senza collegamento.
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ArtworkInquiry>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ArtworkInquiryMessage>()
    .HasOne(m => m.ArtworkInquiry)
    .WithMany(i => i.Messages)
    .HasForeignKey(m => m.ArtworkInquiryId)
    .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<ArtworkInquiryMessage>()
                .HasOne(m => m.SenderUser)
                .WithMany()
                .HasForeignKey(m => m.SenderUserId)
                .OnDelete(DeleteBehavior.SetNull);


            builder.Entity<ArtworkInquiry>()
                .HasIndex(i => i.UserId);


            builder.Entity<ArtworkInquiryMessage>()
                .HasIndex(m => new
                {
                    m.ArtworkInquiryId,
                    m.SenderType,
                    m.ReadAt
                });

            builder.Entity<ArtworkView>()
    .HasOne(v => v.Artwork)
    .WithMany(a => a.Views)
    .HasForeignKey(v => v.ArtworkId)
    .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<ArtworkView>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.SetNull);


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

            builder.Entity<ArtworkAudioGuide>()
    .HasOne(a => a.Artwork)
    .WithMany(a => a.AudioGuides)
    .HasForeignKey(a => a.ArtworkId)
    .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<ArtworkAudioGuide>()
                .HasIndex(a => new
                {
                    a.ArtworkId,
                    a.LanguageCode
                })
                .IsUnique();

            builder.Entity<ArtworkQrScan>()
    .HasOne(s => s.Artwork)
    .WithMany(a => a.QrScans)
    .HasForeignKey(s => s.ArtworkId)
    .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<ArtworkQrScan>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);


            builder.Entity<ArtworkQrScan>()
                .HasIndex(s => new
                {
                    s.ArtworkId,
                    s.ScannedAt
                });

            builder.Entity<Booking>()
    .HasOne(b => b.CheckedInByUser)
    .WithMany()
    .HasForeignKey(b => b.CheckedInByUserId)
    .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Booking>()
    .HasIndex(b => b.QrToken)
    .IsUnique()
    .HasFilter("[QrToken] IS NOT NULL");

            // -------------------------------------------------
            // FEEDBACK VISITE
            // -------------------------------------------------

            // Relazione 1:1 con la prenotazione:
            // un solo feedback per prenotazione.

            builder.Entity<VisitFeedback>()
                .HasOne(f => f.Booking)
                .WithOne(b => b.Feedback)
                .HasForeignKey<VisitFeedback>(f => f.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VisitFeedback>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<VisitFeedback>()
                .HasIndex(f => f.CreatedAt);
        }
    }
}