namespace BeArtGallery.ViewModels
{
    // Dati per la barra di paginazione condivisa (_Pagination.cshtml).
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; } = 1;

        // Azione e controller a cui puntano i link delle pagine.
        public string Action { get; set; } = "Index";

        public string Controller { get; set; } = string.Empty;

        // Descrizione per i lettori di schermo (es. "Pagine del catalogo").
        public string AriaLabel { get; set; } = "Paginazione";
    }
}
