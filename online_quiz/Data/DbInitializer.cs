using Microsoft.AspNetCore.Identity;
using online_quiz.Models;

namespace online_quiz.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "ADMIN", "FACULTY", "PARTICIPANT" };

            foreach (string role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            string adminEmail = "admin@quiz.com";
            string adminPassword = "Admin123";

            ApplicationUser admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quiz Administrator",
                    IsActive = true,
                    EmailConfirmed = true
                };

                IdentityResult result = await userManager.CreateAsync(admin, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "ADMIN");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(admin, "ADMIN"))
                {
                    await userManager.AddToRoleAsync(admin, "ADMIN");
                }
            }
        }
    }
}