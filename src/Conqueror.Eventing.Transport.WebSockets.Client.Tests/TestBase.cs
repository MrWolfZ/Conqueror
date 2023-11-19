using System.Diagnostics;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Conqueror.Eventing.Transport.WebSockets.Client.Tests;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "fields are disposed in test teardown")]
public abstract class TestBase
{
    private HttpClient? client;
    private IHost? clientHost;
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

    protected IHost ClientHost
    {
        get
        {
            if (clientHost == null)
            {
                throw new InvalidOperationException("test fixture must be initialized before using client host");
            }

            return clientHost;
        }
    }

    protected virtual TimeSpan TestTimeout => TimeSpan.FromSeconds(2);

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
                                               _ = webHost.UseTestServer(o => o.BaseAddress = new("http://conqueror1.test"));

                                               _ = webHost.ConfigureServices(ConfigureServerServices);
                                               _ = webHost.Configure(Configure);
                                           });

        host = await hostBuilder.StartAsync(TestTimeoutToken);
        client = host.GetTestClient();
        webSocketClient = host.GetTestServer().CreateWebSocketClient();

        if (!Debugger.IsAttached)
        {
            TimeoutCancellationTokenSource.CancelAfter(TestTimeout);
        }

        var clientHostBuilder = new HostBuilder().ConfigureLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Trace))
                                                 .ConfigureServices(ConfigureClientServices);

        clientHost = await clientHostBuilder.StartAsync(TestTimeoutToken);
    }

    [TearDown]
    public void TearDown()
    {
        timeoutCancellationTokenSource?.Cancel();
        clientHost?.Dispose();
        host?.Dispose();
        client?.Dispose();
        timeoutCancellationTokenSource?.Dispose();
    }

    protected abstract void ConfigureServerServices(IServiceCollection services);

    protected abstract void ConfigureClientServices(IServiceCollection services);

    protected abstract void Configure(IApplicationBuilder app);

    protected T Resolve<T>()
        where T : notnull => Host.Services.GetRequiredService<T>();

    protected T ResolveOnClient<T>()
        where T : notnull
    {
        return ClientHost.Services.GetRequiredService<T>();
    }

    protected Task<WebSocket> ConnectToWebSocket(string path, string query)
    {
        return WebSocketClient.ConnectAsync(new UriBuilder(HttpClient.BaseAddress!) { Scheme = "ws", Path = path, Query = query }.Uri, CancellationToken.None);
    }
}
