using System.ComponentModel.DataAnnotations;

namespace BeArtGallery.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(
            100,
            ErrorMessage = "La password deve contenere almeno {2} caratteri.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(
            nameof(Password),
            ErrorMessage = "Le password non coincidono.")]
        [Display(Name = "Conferma password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Ruolo")]
        public string Role { get; set; } = string.Empty;
    }
}