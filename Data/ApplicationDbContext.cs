using BeArtGallery.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Data
{
    public class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext(options)
    {
        public DbSet<Artist> Artists { get; set; }

        public DbSet<Artwork> Artworks { get; set; }

        public DbSet<GalleryActivity> GalleryActivities { get; set; }

        public DbSet<Booking> Bookings { get; set; }
    }
}