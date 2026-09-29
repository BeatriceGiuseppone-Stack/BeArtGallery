using BeArtGallery.Authorization;
using BeArtGallery.Data;
using BeArtGallery.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Controllers
{
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
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var model = new List<UserManagementViewModel>();

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

            string[] allowedRoles =
            {
        RoleNames.Visitore,
        RoleNames.Staff,
        RoleNames.Admin
    };

            // Impedisce l'invio di ruoli non previsti
            if (!allowedRoles.Contains(role))
            {
                return BadRequest();
            }

            // Controlla che il ruolo esista realmente
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return BadRequest();
            }

            var currentRoles =
                await _userManager.GetRolesAsync(user);

            var currentRole =
                currentRoles.FirstOrDefault();

            // Se il ruolo selezionato è già quello attuale,
            // non dobbiamo fare nulla.
            if (currentRole == role)
            {
                TempData["SuccessMessage"] =
                    $"L'utente {user.Email} possiede già il ruolo {role}.";

                return RedirectToAction(nameof(Users));
            }

            // L'Admin attualmente autenticato non può
            // rimuovere da solo il proprio ruolo Admin.
            var currentUserId =
                _userManager.GetUserId(User);

            if (currentUserId == user.Id &&
                role != RoleNames.Admin)
            {
                TempData["ErrorMessage"] =
                    "Non puoi rimuovere il ruolo Admin dal tuo account mentre sei autenticato.";

                return RedirectToAction(nameof(Users));
            }

            // Impedisce di rimuovere l'ultimo Admin del sistema.
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

            // Rimuove i ruoli precedenti
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
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        // POST: /Admin/CreateUser
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

            var existingUser =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Esiste già un utente con questa email.");

                return View(model);
            }

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

            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    model.Role);

            if (!roleResult.Succeeded)
            {
                // Se il ruolo non può essere assegnato,
                // eliminiamo l'utente appena creato.
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

            // Usiamo sempre l'email reale presente nel database
            model.Email = user.Email ?? user.UserName ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Genera un token sicuro di reset tramite ASP.NET Core Identity
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