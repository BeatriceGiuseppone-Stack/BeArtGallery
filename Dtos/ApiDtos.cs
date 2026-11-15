using BeArtGallery.Models.Enums;

namespace BeArtGallery.Dtos
{
    // DTO: oggetti restituiti dalle API in formato JSON.
    // Tengono separati i dati del database da quelli inviati ai client,
    // così non escono relazioni interne o dati riservati (es. degli utenti).

    /// <summary>
    /// Risposta standard delle operazioni di scrittura
    /// </summary>
    public record ApiMessageResponse(bool Success,string Message);

    /// <summary>
    /// Risposta quando si crea una prenotazione
    /// </summary>
    public record BookingCreatedResponse(bool Success, string Message, int BookingId);

    /// <summary>
    /// Attività (visita o evento) prenotabile
    /// </summary>
    public record ActivityDto( int Id, string Title, string? Description, ActivityType ActivityType,
        DateTime StartDateTime, int DurationMinutes, string? Location, int MaxParticipants,
        int BookedParticipants, int AvailablePlaces, GalleryActivityStatus Status);

    /// <summary>
    /// Opera del catalogo in versione breve, per gli elenchi
    /// </summary>
    public record ArtworkSummaryDto(int Id, string Title, int? Year, string? Technique,
        string? ImageUrl, int ArtistId, string ArtistName, ArtworkAvailabilityStatus AvailabilityStatus, bool IsOnDisplay);

    /// <summary>
    /// Opera del catalogo con tutti i dettagli e le audioguide attive
    /// </summary>
    public record ArtworkDetailDto(int Id, string Title, int? Year, string? Technique,
        string? Description, string? ImageUrl, decimal? WidthCm, decimal? HeightCm, int ArtistId,
        string ArtistName, string? ArtistNationality, ArtworkAvailabilityStatus AvailabilityStatus, bool IsOnDisplay,
        IReadOnlyList<AudioGuideDto> AudioGuides);

    /// <summary>
    /// Audioguida di un'opera
    /// </summary>
    public record AudioGuideDto(int Id, string LanguageCode, string Title, string AudioUrl);

    /// <summary>
    /// Prenotazione dell'utente collegato
    /// </summary>
    public record MyBookingDto(int BookingId, int GalleryActivityId, string ActivityTitle, DateTime StartDateTime,
        string? Location, int NumberOfParticipants, DateTime BookingDateUtc, BookingStatus Status, DateTime? CheckedInAtUtc,
        bool CanCancel);

    /// <summary>
    /// Indicatori principali della dashboard
    /// </summary>
    public record DashboardStatsDto( int TotalUsers, int TotalArtworks, int AvailableArtworks, int ConfirmedBookings,
        int UpcomingActivities, decimal OccupancyRate, int ExpectedVisitors, int CheckedInVisitors, int NoShowVisitors,
        double AttendanceRate, int TotalArtworkViews, int TotalFavorites, int OpenInquiries, int TotalFeedbacks,
        double AverageRating, IReadOnlyList<MonthlyValueDto> BookingsByMonth);

    /// <summary>
    /// Valore di un mese, per i grafici
    /// </summary>
    public record MonthlyValueDto(string Label, int Value);
}