using BeArtGallery.Authorization;
using BeArtGallery.Data;
using BeArtGallery.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers
{
    // Area riservata all'amministratore: gestione degli utenti e dei ruoli
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Admin/Users
        // Elenco di tutti gli utenti con il loro ruolo
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var model = new List<UserManagementViewModel>();

            // Per ogni utente legge il ruolo assegnato
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                model.Add(new UserManagementViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? RoleNames.Visitore
                });
            }

            return View(model);
        }

        // POST: /Admin/ChangeRole
        // Cambia il ruolo di un utente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(
            string userId,
            string role)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Ruoli che si possono assegnare
            string[] allowedRoles =
            {
        RoleNames.Visitore,
        RoleNames.Staff,
        RoleNames.Admin
    };

            // Rifiuta ruoli diversi da quelli previsti
            if (!allowedRoles.Contains(role))
            {
                return BadRequest();
            }

            // Controlla che il ruolo esista nel database
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return BadRequest();
            }

            var currentRoles =
                await _userManager.GetRolesAsync(user);

            var currentRole =
                currentRoles.FirstOrDefault();

            // Se il ruolo è già quello attuale non c'è niente da cambiare
            if (currentRole == role)
            {
                TempData["SuccessMessage"] =
                    $"L'utente {user.Email} possiede già il ruolo {role}.";

                return RedirectToAction(nameof(Users));
            }

            // L'admin collegato non può togliersi da solo il ruolo di Admin
            var currentUserId =
                _userManager.GetUserId(User);

            if (currentUserId == user.Id &&
                role != RoleNames.Admin)
            {
                TempData["ErrorMessage"] =
                    "Non puoi rimuovere il ruolo Admin dal tuo account mentre sei autenticato.";

                return RedirectToAction(nameof(Users));
            }

            // Deve rimanere almeno un Admin nel sistema
            if (currentRole == RoleNames.Admin &&
                role != RoleNames.Admin)
            {
                var admins =
                    await _userManager.GetUsersInRoleAsync(
                        RoleNames.Admin);

                if (admins.Count <= 1)
                {
                    TempData["ErrorMessage"] =
                        "Non è possibile modificare il ruolo: deve esistere almeno un Admin nel sistema.";

                    return RedirectToAction(nameof(Users));
                }
            }

            // Toglie i ruoli che l'utente aveva prima
            if (currentRoles.Count > 0)
            {
                var removeResult =
                    await _userManager.RemoveFromRolesAsync(
                        user,
                        currentRoles);

                if (!removeResult.Succeeded)
                {
                    TempData["ErrorMessage"] =
                        "Non è stato possibile modificare il ruolo.";

                    return RedirectToAction(nameof(Users));
                }
            }

            // Assegna il nuovo ruolo
            var addResult =
                await _userManager.AddToRoleAsync(
                    user,
                    role);

            if (!addResult.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "Non è stato possibile assegnare il nuovo ruolo.";

                return RedirectToAction(nameof(Users));
            }

            TempData["SuccessMessage"] =
                $"Ruolo di {user.Email} aggiornato da {currentRole} a {role}.";

            return RedirectToAction(nameof(Users));
        }

        // GET: /Admin/CreateUser
        // Form per creare un nuovo utente
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        // POST: /Admin/CreateUser
        // Crea l'utente con il ruolo scelto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(
            CreateUserViewModel model)
        {
            string[] allowedRoles =
            {
                RoleNames.Visitore,
                RoleNames.Staff,
                RoleNames.Admin
            };

            if (!allowedRoles.Contains(model.Role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Il ruolo selezionato non è valido.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Controlla che l'email non sia già usata
            var existingUser =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Esiste già un utente con questa email.");

                return View(model);
            }

            // Crea l'utente con la password indicata
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var createResult =
                await _userManager.CreateAsync(
                    user,
                    model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            // Assegna il ruolo al nuovo utente
            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    model.Role);

            if (!roleResult.Succeeded)
            {
                // Se il ruolo non si può assegnare, l'utente appena creato viene eliminato
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            TempData["SuccessMessage"] =
                $"Utente {model.Email} creato con ruolo {model.Role}.";

            return RedirectToAction(nameof(Users));
        }

        // GET: /Admin/ResetPassword
        // Form per impostare una nuova password a un utente
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new AdminResetPasswordViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty
            };

            return View(model);
        }


        // POST: /Admin/ResetPassword
        // Imposta la nuova password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            AdminResetPasswordViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
            {
                return NotFound();
            }

            // L'email viene sempre presa dal database
            model.Email = user.Email ?? user.UserName ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Identity crea un token di reset e lo usa subito per cambiare la password
            var token =
                await _userManager.GeneratePasswordResetTokenAsync(user);

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            TempData["SuccessMessage"] =
                $"Password dell'utente {model.Email} reimpostata correttamente.";

            return RedirectToAction(nameof(Users));
        }
    }
}