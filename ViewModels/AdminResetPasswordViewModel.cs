using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.ViewModels
{
    public class AdminResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Inserisci la nuova password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nuova password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Conferma la nuova password.")]
        [DataType(DataType.Password)]
        [Compare(
            nameof(NewPassword),
            ErrorMessage = "Le password non coincidono.")]
        [Display(Name = "Conferma nuova password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}