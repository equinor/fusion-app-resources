
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace Fusion.Infra.Cli;

public class Utils
{

    public static void AnalyseToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenContent = tokenHandler.ReadJwtToken(token);

        Console.WriteLine($"-- Token | object id: {tokenContent.Claims}");
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