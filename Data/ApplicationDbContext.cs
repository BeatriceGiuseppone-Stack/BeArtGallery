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
        }
    }
}