using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Tests;

internal sealed class LoggingMiddlewareTestHost : IAsyncDisposable
{
    private LoggingMiddlewareTestHost()
    {
    }

    public required IHost Host { get; init; }

    public required TimeSpan TestTimeout { get; init; }

    public CancellationToken TestTimeoutToken => TimeoutCancellationTokenSource.Token;

    private CancellationTokenSource TimeoutCancellationTokenSource { get; } = new();

    public static async Task<LoggingMiddlewareTestHost> Create(
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

        var testHost = new LoggingMiddlewareTestHost
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
