
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace Fusion.Infra.Cli;

public class Utils
{

    /// <summary>
    /// Check if there are any obvious problems with the token, and give some convenience debug output to the console.
    /// </summary>
    public static void AnalyseToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenContent = tokenHandler.ReadJwtToken(token);

            Console.WriteLine($"-- Token | object id: {tokenContent.Claims.FirstOrDefault(c => c.Type == "oid")?.Value}");

            var requiredRole = tokenContent.Claims.FirstOrDefault(c => c.Type == "roles" && c.Value == "Fusion.Infrastructure.Database.Manage");
            var roles = string.Join(", ", tokenContent.Claims.Where(c => c.Type == "roles").Select(c => c.Value));
            if (requiredRole is null)
            {
                Console.WriteLine($"-- Token | warning: Could not locate the role [Fusion.Infrastructure.Database.Manage]. Might be missing permissions?");
                Console.WriteLine($"-- Token | roles: {roles}");
            }
        } catch
        {
            Console.WriteLine("# WARN - Could not read token to analyse");
        }
    }

    public static string? GetCurrentUserFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenContent = tokenHandler.ReadJwtToken(token);

        return tokenContent.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
    }

    public static void PrintToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenContent = tokenHandler.ReadJwtToken(token);

        var serializedContent = JsonSerializer.Serialize(new
        {
            claims = tokenContent.Claims.Select(c => new { type = c.Type, value = c.Value }),
            actor = tokenContent.Actor,
            audiences = tokenContent.Audiences,
            issuedAt = tokenContent.IssuedAt,
            id = tokenContent.Id,
            subject = tokenContent.Subject,
            validFrom = tokenContent.ValidFrom,
            valiedTo = tokenContent.ValidTo
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });

        Console.WriteLine(serializedContent);
    }    
}