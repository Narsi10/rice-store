using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Data;

/// <summary>
/// Seeds the initial admin account so there's always an admin to log in as.
/// Credentials come from configuration / environment variables — NOT hardcoded —
/// so no secrets live in source control. Set these (e.g. via environment vars):
///   Admin__Email, Admin__Password, Admin__FullName
/// If they aren't set, no admin is seeded (and a warning is logged by the caller).
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IdentityDbContext db, IConfiguration config)
    {
        await db.Database.EnsureCreatedAsync();

        var email = config["Admin:Email"];
        var password = config["Admin:Password"];
        var fullName = config["Admin:FullName"] ?? "Administrator";

        // No admin credentials configured -> skip seeding (nothing hardcoded).
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        if (await db.Users.AnyAsync(u => u.Email == email)) return;

        db.Users.Add(new AppUser
        {
            Email = email,
            FullName = fullName,
            PasswordHash = Hash(password),
            Role = "Admin"
        });

        await db.SaveChangesAsync();
    }

    private static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
