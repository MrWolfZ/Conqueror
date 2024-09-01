using System.Diagnostics;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore.Tests;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "resources are disposed in test teardown")]
public abstract class TestBase
{
    private HttpClient? client;
    private IHost? host;
    private CancellationTokenSource? timeoutCancellationTokenSource;
    private WebSocketClient? webSocketClient;

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

    protected WebSocketClient WebSocketClient
    {
        get
        {
            if (webSocketClient == null)
            {
                throw new InvalidOperationException("test fixture must be initialized before using web socket client");
            }

            return webSocketClient;
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
    public async Task SetUp()
    {
        timeoutCancellationTokenSource = new();

        var hostBuilder = new HostBuilder().ConfigureLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Trace))
                                           .ConfigureWebHost(webHost =>
                                           {
                                               _ = webHost.UseTestServer();

                                               _ = webHost.ConfigureServices(ConfigureServices);
                                               _ = webHost.Configure(Configure);
                                           });

        host = await hostBuilder.StartAsync(TestTimeoutToken);
        client = host.GetTestClient();
        webSocketClient = host.GetTestServer().CreateWebSocketClient();

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
        client?.Dispose();
        timeoutCancellationTokenSource?.Dispose();
    }

    protected abstract void ConfigureServices(IServiceCollection services);

    protected abstract void Configure(IApplicationBuilder app);

    protected T Resolve<T>()
        where T : notnull => Host.Services.GetRequiredService<T>();

    protected Task<WebSocket> ConnectToWebSocket(string path)
    {
        return WebSocketClient.ConnectAsync(new UriBuilder(HttpClient.BaseAddress!) { Scheme = "ws", Path = path }.Uri, CancellationToken.None);
    }
}
