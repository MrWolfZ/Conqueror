using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Conqueror.Common.Transport.Http.Server.AspNetCore.Tests;

public abstract class TestBase
{
    private const string Authority = "https://auth.test.com";
    private const string Audience = "test-audience";
    private const string Issuer = "https://issuer.test.com";

    private readonly Lazy<SymmetricSecurityKey> signingKeyLazy = new(CreateSigningKey);

    private HttpClient? client;
    private IHost? host;

    protected HttpClient HttpClient
    {
        get
        {
            if (client == null)
            {
                throw new InvalidOperationException("test fixture must be initialized before using http client");
            }

            return client;
        }
    }

    protected IHost Host
    {
        get
        {
            if (host == null)
            {
                throw new InvalidOperationException("test fixture must be initialized before using host");
            }

            return host;
        }
    }

    [SetUp]
    public async Task SetUp()
    {
        var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
        {
            _ = webHost.UseTestServer();

            _ = webHost.ConfigureServices(services =>
            {
                _ = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(options =>
                            {
                                options.TokenValidationParameters.NameClaimType = "id";
                                options.TokenValidationParameters.RoleClaimType = "role";
                            });

                _ = services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
                                                             options =>
                                                             {
                                                                 options.Audience = Audience;
                                                                 options.Authority = Authority;

                                                                 options.TokenValidationParameters.ValidAudience = Audience;
                                                                 options.TokenValidationParameters.ValidIssuer = Issuer;

                                                                 options.TokenValidationParameters.IssuerSigningKeyResolver = (_, _, _, _) => [signingKeyLazy.Value];
                                                             });
            });

            _ = webHost.ConfigureServices(ConfigureServices);
            _ = webHost.Configure(Configure);
        });

        host = await hostBuilder.StartAsync();
        client = host.GetTestClient();
    }

    [TearDown]
    public void TearDown()
    {
        client?.Dispose();
        host?.Dispose();
    }

    protected abstract void ConfigureServices(IServiceCollection services);

    protected abstract void Configure(IApplicationBuilder app);

    protected T Resolve<T>()
        where T : notnull => Host.Services.GetRequiredService<T>();

    protected void WithAuthenticatedPrincipal(HttpRequestHeaders requestHeaders, string name)
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

    private string BuildToken(string name)
    {
        var signingCredentials = new SigningCredentials(signingKeyLazy.Value, SecurityAlgorithms.HmacSha256);

        var principal = CreateUserPrincipal(name);
        var notBefore = DateTime.UtcNow.AddMinutes(-10);
        var expires = DateTime.UtcNow.AddHours(1);
        var token = new JwtSecurityToken(Issuer, Audience, principal.Claims, notBefore, expires, signingCredentials);
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
