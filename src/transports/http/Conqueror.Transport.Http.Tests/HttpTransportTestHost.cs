using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Conqueror.Transport.Http.Tests;

internal sealed class HttpTransportTestHost : IAsyncDisposable
{
    private HttpTransportTestHost()
    {
    }

    public required HttpClient HttpClient { get; init; }
    public required IHost Host { get; init; }

    public required TimeSpan TestTimeout { get; init; }

    public CancellationToken TestTimeoutToken => TimeoutCancellationTokenSource.Token;

    private CancellationTokenSource TimeoutCancellationTokenSource { get; } = new();

    public static async Task<HttpTransportTestHost> Create(
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configure = null,
        TimeSpan? testTimeout = null)
    {
        var hostBuilder = new HostBuilder().ConfigureLogging(logging => logging.AddConsole()
                                                                               .SetMinimumLevel(LogLevel.Trace)

                                                                               // set some very verbose loggers to info to reduce noise in the logs
                                                                               .AddFilter(typeof(FileSystemXmlRepository).FullName, LogLevel.Information)
                                                                               .AddFilter(typeof(XmlKeyManager).FullName, LogLevel.Information))
                                           .UseEnvironment(Environments.Development)
                                           .ConfigureWebHost(webHost =>
                                           {
                                               _ = webHost.UseTestServer();

                                               _ = webHost.ConfigureServices(services =>
                                               {
                                                   ConfigureBearerAuthentication(services);

                                                   configureServices?.Invoke(services);
                                               });

                                               if (configure is not null)
                                               {
                                                   _ = webHost.Configure(configure);
                                               }
                                           });

        var host = await hostBuilder.StartAsync();
        var client = host.GetTestClient();

        var testHost = new HttpTransportTestHost
        {
            HttpClient = client,
            Host = host,
            TestTimeout = testTimeout ?? TimeSpan.FromSeconds(2),
        };

        if (!Debugger.IsAttached)
        {
            testHost.TimeoutCancellationTokenSource.CancelAfter(testHost.TestTimeout);
        }

        return testHost;
    }

    public T Resolve<T>()
        where T : notnull => Host.Services.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(TimeoutCancellationTokenSource);
        await CastAndDispose(HttpClient);
        await CastAndDispose(Host);

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }

    private static void ConfigureBearerAuthentication(IServiceCollection services)
    {
        _ = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters.NameClaimType = "id";
                        options.TokenValidationParameters.RoleClaimType = "role";
                    });

        _ = services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.Audience = HttpTransportTestAuthenticationUtil.Audience;
                options.Authority = HttpTransportTestAuthenticationUtil.Authority;

                options.TokenValidationParameters.ValidAudience = HttpTransportTestAuthenticationUtil.Audience;
                options.TokenValidationParameters.ValidIssuer = HttpTransportTestAuthenticationUtil.Issuer;

                options.TokenValidationParameters.IssuerSigningKeyResolver = (
                    _,
                    _,
                    _,
                    _) => [HttpTransportTestAuthenticationUtil.SigningKey];
            });
    }
}
