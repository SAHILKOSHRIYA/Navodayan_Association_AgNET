using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NAU.Domain.Constants;
using NAU.Domain.Entities;
using NAU.Infrastructure.Identity;

namespace NAU.Infrastructure.Persistence;

/// <summary>
/// Idempotent startup seeding (Phase 2 §5.3): migrations, JNV Raipur school,
/// the five roles, and one SuperAdmin whose credentials come from configuration.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        if (config.GetValue("Database:MigrateOnStartup", false))
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied");
        }

        // School (pilot: JNV Raipur)
        var school = await db.Schools.FirstOrDefaultAsync(s => s.Code == "JNV-RAIPUR");
        if (school is null)
        {
            school = new School
            {
                Id = Guid.NewGuid(),
                Name = "Jawahar Navodaya Vidyalaya, Raipur",
                Code = "JNV-RAIPUR",
                District = "Raipur",
                State = "Chhattisgarh",
            };
            db.Schools.Add(school);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded school {Code}", school.Code);
        }

        // Roles
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new AppRole(role));

        // SuperAdmin from configuration (env vars in production — never hard-coded).
        var adminEmail = config["Seed:SuperAdmin:Email"];
        var adminPassword = config["Seed:SuperAdmin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("Seed:SuperAdmin not configured — skipping super admin creation");
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                FullName = config["Seed:SuperAdmin:FullName"] ?? "Platform Admin",
                SchoolId = school.Id,
                EmailConfirmed = true,
                EmailVerifiedAt = DateTime.UtcNow,
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);
                logger.LogInformation("Seeded SuperAdmin {Email}", adminEmail);
            }
            else
            {
                logger.LogError("SuperAdmin seed failed: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
