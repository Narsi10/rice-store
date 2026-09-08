using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Auth;

public static class JwtAuthExtensions
{
    /// <summary>
    /// Adds JWT bearer authentication using the same signing key/issuer/audience
    /// that the Identity service uses to issue tokens. Any service that calls
    /// this can then protect endpoints with [Authorize] / [Authorize(Roles=...)].
    /// </summary>
    public static IServiceCollection AddJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"] ?? "dev-super-secret-key-change-me-please-32b";
        var issuer = configuration["Jwt:Issuer"] ?? "rice-store";
        var audience = configuration["Jwt:Audience"] ?? "rice-store-clients";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                };
            });

        services.AddAuthorization();
        return services;
    }
}
