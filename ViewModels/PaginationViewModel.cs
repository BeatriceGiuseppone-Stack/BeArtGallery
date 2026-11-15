namespace BeArtGallery.ViewModels
{
    // Dati per la barra delle pagine (_Pagination.cshtml)
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; } = 1;

        // Azione e controller dei link delle pagine
        public string Action { get; set; } = "Index";

        public string Controller { get; set; } = string.Empty;

        // Testo letto dai lettori di schermo (es. "Pagine del catalogo")
        public string AriaLabel { get; set; } = "Paginazione";
    }
}