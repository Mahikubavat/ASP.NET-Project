using BlogPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Admin", "Writer", "Reader" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed a default admin (development only). Change credentials or remove for production.
            var adminEmail = "admin@example.com";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin@example.com",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Bio = "Administrator account"
                };
                var result = await userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }

        public static async Task SeedCategoriesAsync(ApplicationDbContext db)
        {
            if (db.Categories == null || await db.Categories.AnyAsync())
                return;

            var defaults = new[] { "Technology", "Lifestyle", "Travel", "Food", "Business" };
            foreach (var name in defaults)
            {
                db.Categories!.Add(new Category
                {
                    Name = name,
                    Slug = name.ToLowerInvariant()
                });
            }
            await db.SaveChangesAsync();
        }
    }
}