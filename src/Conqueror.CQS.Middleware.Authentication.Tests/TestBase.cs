using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Conqueror.CQS.Middleware.Authentication.Tests;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "resources are disposed in test teardown")]
public abstract class TestBase
{
    private IHost? host;
    private CancellationTokenSource? timeoutCancellationTokenSource;

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

    protected virtual TimeSpan TestTimeout => TimeSpan.FromSeconds(Environment.GetEnvironmentVariable("GITHUB_ACTION") is null ? 2 : 10);

    protected CancellationToken TestTimeoutToken => TimeoutCancellationTokenSource.Token;

    private CancellationTokenSource TimeoutCancellationTokenSource
    {
        get
        {
            if (timeoutCancellationTokenSource == null)
            {
                throw new InvalidOperationException("test fixture must be initialized before timeout cancellation token source");
            }

            return timeoutCancellationTokenSource;
        }
    }

    [SetUp]
    public void SetUp()
    {
        timeoutCancellationTokenSource = new();

        var hostBuilder = new HostBuilder().ConfigureServices(ConfigureServices);

        host = hostBuilder.Build();

        if (!Debugger.IsAttached)
        {
            TimeoutCancellationTokenSource.CancelAfter(TestTimeout);
        }
    }

    [TearDown]
    public void TearDown()
    {
        timeoutCancellationTokenSource?.Cancel();
        host?.Dispose();
        timeoutCancellationTokenSource?.Dispose();
    }

    protected abstract void ConfigureServices(IServiceCollection services);

    protected T Resolve<T>()
        where T : notnull => Host.Services.GetRequiredService<T>();
}
