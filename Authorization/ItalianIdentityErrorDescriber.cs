using Microsoft.AspNetCore.Identity;

namespace BeArtGallery.Authorization
{
    // Messaggi di errore di ASP.NET Identity in italiano
    // (registrazione, cambio password, reimpostazione).
    public class ItalianIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() =>
            new() { Code = nameof(DefaultError), Description = "Si è verificato un errore imprevisto." };

        public override IdentityError PasswordMismatch() =>
            new() { Code = nameof(PasswordMismatch), Description = "Password non corretta." };

        public override IdentityError InvalidToken() =>
            new() { Code = nameof(InvalidToken), Description = "Il link non è valido o è scaduto." };

        public override IdentityError InvalidEmail(string? email) =>
            new() { Code = nameof(InvalidEmail), Description = $"L'email '{email}' non è valida." };

        public override IdentityError InvalidUserName(string? userName) =>
            new() { Code = nameof(InvalidUserName), Description = $"Il nome utente '{userName}' non è valido." };

        public override IdentityError DuplicateEmail(string email) =>
            new() { Code = nameof(DuplicateEmail), Description = $"L'email '{email}' è già registrata." };

        public override IdentityError DuplicateUserName(string userName) =>
            new() { Code = nameof(DuplicateUserName), Description = $"L'account '{userName}' esiste già." };

        public override IdentityError PasswordTooShort(int length) =>
            new() { Code = nameof(PasswordTooShort), Description = $"La password deve avere almeno {length} caratteri." };

        public override IdentityError PasswordRequiresNonAlphanumeric() =>
            new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "La password deve contenere almeno un simbolo (per esempio ! o ?)." };

        public override IdentityError PasswordRequiresDigit() =>
            new() { Code = nameof(PasswordRequiresDigit), Description = "La password deve contenere almeno un numero." };

        public override IdentityError PasswordRequiresLower() =>
            new() { Code = nameof(PasswordRequiresLower), Description = "La password deve contenere almeno una lettera minuscola." };

        public override IdentityError PasswordRequiresUpper() =>
            new() { Code = nameof(PasswordRequiresUpper), Description = "La password deve contenere almeno una lettera maiuscola." };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
            new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"La password deve contenere almeno {uniqueChars} caratteri diversi." };

        public override IdentityError UserAlreadyHasPassword() =>
            new() { Code = nameof(UserAlreadyHasPassword), Description = "L'utente ha già una password." };

        public override IdentityError UserAlreadyInRole(string role) =>
            new() { Code = nameof(UserAlreadyInRole), Description = $"L'utente ha già il ruolo '{role}'." };

        public override IdentityError UserNotInRole(string role) =>
            new() { Code = nameof(UserNotInRole), Description = $"L'utente non ha il ruolo '{role}'." };
    }
}
