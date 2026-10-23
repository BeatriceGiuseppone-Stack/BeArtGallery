using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.ViewModels
{
    public class ArtworkAudioGuideViewModel
    {
        [Required]
        public int ArtworkId { get; set; }


        [Required(ErrorMessage = "Seleziona la lingua.")]
        [Display(Name = "Lingua")]
        public string LanguageCode { get; set; } = "it-IT";


        [Required(ErrorMessage = "Inserisci un titolo.")]
        [MaxLength(200)]
        [Display(Name = "Titolo")]
        public string Title { get; set; } = string.Empty;


        [Display(Name = "File audio")]
        public IFormFile? AudioFile { get; set; }


        [MaxLength(5000)]
        [Display(Name = "Trascrizione")]
        public string? Transcript { get; set; }


        [Display(Name = "Attiva")]
        public bool IsActive { get; set; } = true;
    }
}