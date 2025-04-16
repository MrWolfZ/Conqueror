using System.Diagnostics;
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
        var hostBuilder = new HostBuilder().ConfigureLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Trace)

                                                                               // set some very verbose loggers to info to reduce noise in the logs
                                                                               .AddFilter(typeof(FileSystemXmlRepository).FullName, LogLevel.Information)
                                                                               .AddFilter(typeof(XmlKeyManager).FullName, LogLevel.Information))
                                           .UseEnvironment(Environments.Development)
                                           .ConfigureWebHost(webHost =>
                                           {
                                               _ = webHost.UseTestServer();

                                               _ = webHost.ConfigureServices(configureServices ?? (_ => { }));
                                               _ = webHost.Configure(configure ?? (_ => { }));
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
}
