using System.Security.Cryptography;
using System.Text;
using Identity.API.Data;
using Identity.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Controllers;

public record RegisterRequest(string Email, string Password, string FullName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string UserId, string Email, string FullName, string Role, string Token);

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly TokenService _tokens;

    public AuthController(IdentityDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict("Email already registered.");

        var user = new AppUser
        {
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = Hash(request.Password),
            Role = "Customer"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(ToResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || user.PasswordHash != Hash(request.Password))
            return Unauthorized("Invalid credentials.");

        return Ok(ToResponse(user));
    }

    private AuthResponse ToResponse(AppUser user) =>
        new(user.Id.ToString(), user.Email, user.FullName, user.Role, _tokens.CreateToken(user));

    // Simple SHA-256 hash for the scaffold. Use a salted KDF (e.g. PBKDF2/BCrypt)
    // in production.
    private static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
