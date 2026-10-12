using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.ViewModels
{
    public class ArtworkInquiryViewModel
    {
        [Required]
        public int ArtworkId { get; set; }


        [Required(ErrorMessage = "Inserisci il tuo nome.")]
        [MaxLength(100)]
        [Display(Name = "Nome")]
        public string Name { get; set; } = string.Empty;


        [Required(ErrorMessage = "Inserisci il tuo indirizzo email.")]
        [EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido.")]
        [MaxLength(256)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Inserisci un messaggio.")]
        [StringLength(
            2000,
            MinimumLength = 10,
            ErrorMessage = "Il messaggio deve contenere almeno 10 caratteri.")]
        [Display(Name = "Messaggio")]
        public string Message { get; set; } = string.Empty;
    }
}