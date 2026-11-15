using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.Dtos
{
    // Dati inviati all'API per creare una prenotazione
    public class CreateBookingRequest
    {
        // Attività da prenotare
        [Required]
        public int GalleryActivityId { get; set; }

        // Numero di persone (da 1 a 10)
        [Range(1, 10)]
        public int NumberOfParticipants { get; set; }
    }
}