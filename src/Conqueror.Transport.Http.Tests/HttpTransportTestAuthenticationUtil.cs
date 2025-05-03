using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Conqueror.Transport.Http.Tests;

internal static class HttpTransportTestAuthenticationUtil
{
    public const string Authority = "https://auth.test.com";
    public const string Audience = "test-audience";
    public const string Issuer = "https://issuer.test.com";

    private static readonly Lazy<SymmetricSecurityKey> SigningKeyLazy = new(CreateSigningKey);

    public static SymmetricSecurityKey SigningKey => SigningKeyLazy.Value;

    public static void WithAuthenticatedPrincipal(this HttpRequestHeaders requestHeaders, string name)
    {
        var authenticationHeaderValue = new AuthenticationHeaderValue("Bearer", BuildToken(name));
        requestHeaders.Authorization = authenticationHeaderValue;
    }

    private static ClaimsPrincipal CreateUserPrincipal(string name)
    {
        var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme, "id", "role");
        identity.AddClaim(new(identity.NameClaimType, name));

        return new(identity);
    }

    private static string BuildToken(string name)
    {
        var signingCredentials = new SigningCredentials(SigningKeyLazy.Value, SecurityAlgorithms.HmacSha256);

        var principal = CreateUserPrincipal(name);
        var notBefore = DateTime.UtcNow.AddMinutes(-10);
        var expires = DateTime.UtcNow.AddHours(1);
        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            principal.Claims,
            notBefore,
            expires,
            signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static SymmetricSecurityKey CreateSigningKey()
    {
        var keyBytes = new byte[32];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);

        return new(keyBytes) { KeyId = Guid.NewGuid().ToString() };
    }
}
