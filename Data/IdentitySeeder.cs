using BeArtGallery.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BeArtGallery.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            var roleManager =
                services.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                services.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles =
            {
                RoleNames.Visitore,
                RoleNames.Staff,
                RoleNames.Admin
            };

            // Crea i ruoli se non esistono
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(roleName));
                }
            }

            // Garantisce soltanto l'Admin iniziale
            var adminEmail =
                configuration["RoleBootstrap:AdminEmail"];

            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                var admin =
                    await userManager.FindByEmailAsync(adminEmail);

                if (admin != null)
                {
                    var adminRoles =
                        await userManager.GetRolesAsync(admin);

                    if (!adminRoles.Contains(RoleNames.Admin))
                    {
                        if (adminRoles.Count > 0)
                        {
                            await userManager.RemoveFromRolesAsync(
                                admin,
                                adminRoles);
                        }

                        await userManager.AddToRoleAsync(
                            admin,
                            RoleNames.Admin);
                    }
                }
            }

            // Gli utenti senza ruolo diventano Visitore
            var users = await userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                var userRoles =
                    await userManager.GetRolesAsync(user);

                if (userRoles.Count == 0)
                {
                    await userManager.AddToRoleAsync(
                        user,
                        RoleNames.Visitore);
                }
            }
        }
    }
}