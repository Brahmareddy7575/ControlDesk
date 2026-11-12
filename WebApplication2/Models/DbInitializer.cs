using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebApplication2.Models
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

            try
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Users>>();

                if (!await db.Database.CanConnectAsync())
                {
                    logger.LogWarning("Database is not reachable during startup seeding.");
                    return;
                }

                // Ensure default Admin exists without touching existing users or products
                var hasAdmin = await db.Users.AnyAsync(u => u.RoleId == 1 || u.Role == "Admin");
                if (!hasAdmin)
                {
                    var defaultAdmin = new Users
                    {
                        Name = "admin",
                        Email = "admin@admindashboard.local",
                        Role = "Admin",
                        RoleId = 1,
                        ApprovalStatusId = 2,
                        IsApproved = true,
                        Department = "Administration",
                        PersonalDetails = "Default system administrator account",
                        CreatedAt = DateTime.UtcNow
                    };
                    defaultAdmin.password = hasher.HashPassword(defaultAdmin, "Admin@123");

                    db.Users.Add(defaultAdmin);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Skipped startup seeding because database schema or connection is not ready yet.");
            }
        }
    }
}

