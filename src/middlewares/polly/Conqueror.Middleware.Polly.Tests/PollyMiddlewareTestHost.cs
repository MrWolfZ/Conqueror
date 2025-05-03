using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Polly.Tests;

internal sealed class PollyMiddlewareTestHost : IAsyncDisposable
{
    private PollyMiddlewareTestHost()
    {
    }

    public required IHost Host { get; init; }

    public required TimeSpan TestTimeout { get; init; }

    public CancellationToken TestTimeoutToken => TimeoutCancellationTokenSource.Token;

    private CancellationTokenSource TimeoutCancellationTokenSource { get; } = new();

    public static async Task<PollyMiddlewareTestHost> Create(
        Action<IServiceCollection>? configureServices = null,
        Action<ILoggingBuilder>? configureLogging = null,
        TimeSpan? testTimeout = null)
    {
        var hostBuilder = new HostBuilder().ConfigureLogging(logging =>
                                           {
                                               _ = logging.SetMinimumLevel(LogLevel.Trace);

                                               configureLogging?.Invoke(logging);
                                           })
                                           .UseEnvironment(Environments.Development)
                                           .ConfigureServices(services =>
                                           {
                                               configureServices?.Invoke(services);
                                           });

        var host = await hostBuilder.StartAsync();

        var testHost = new PollyMiddlewareTestHost
        {
            Host = host,
            TestTimeout = testTimeout ?? TimeSpan.FromSeconds(Environment.GetEnvironmentVariable("GITHUB_ACTION") is null ? 2 : 10),
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
