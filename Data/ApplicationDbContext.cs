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
                .OnDelete(DeleteBehavior.Restrict);

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
        }
    }
}