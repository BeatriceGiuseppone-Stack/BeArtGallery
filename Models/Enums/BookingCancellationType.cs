namespace BeArtGallery.Models.Enums
{
    // Chi ha annullato la prenotazione: l'utente, lo staff o l'annullamento dell'attività
    public enum BookingCancellationType
    {
        User = 1,
        StaffOrAdmin = 2,
        ActivityCancelled = 3
    }
}