using DotLearn.Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotLearn.Auth.Data;

public static class DbSeeder
{
    public static async Task EnsurePopulatedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        try
        {
            if (context.Database.IsSqlServer())
            {
                await context.Database.MigrateAsync();
            }

            bool adminExists = await context.Users.AnyAsync(u => u.Role == "Admin" && u.IsActive && !u.IsDeleted);
            if (!adminExists)
            {
                var email = configuration["SEED_ADMIN_EMAIL"] ?? "admin@dotlearn.com";
                var password = configuration["SEED_ADMIN_PASSWORD"] ?? "Admin@123";

                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = "System Admin",
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12),
                    Role = "Admin",
                    IsActive = true,
                    IsDeleted = false,
                    AuthProvider = "Local"
                };

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();
                
                logger.LogInformation("Successfully provisioned default Admin account utilizing environment fallback bindings.");
            }
            else
            {
                logger.LogInformation("Seed skipped: Dedicated active Admin user safely detected inside native context.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical fault dynamically seeding the native User environment.");
        }
    }
}
