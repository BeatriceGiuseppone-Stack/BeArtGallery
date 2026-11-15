namespace BeArtGallery.Models
{
    // Dati per la pagina di errore
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
