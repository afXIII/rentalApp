using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalApp.Web.Domain;

namespace RentalApp.Web.Data.Seed;

// Runs once at startup. Laravel: `php artisan migrate --seed`, but automatic.
public class DatabaseInitializer(AppDbContext db, RoleManager<IdentityRole> roleManager)
{
    public async Task InitializeAsync()
    {
        // Creates the database if it does not exist, then applies any pending migrations.
        await db.Database.MigrateAsync();
        await SeedRolesAsync(roleManager);
    }

    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
