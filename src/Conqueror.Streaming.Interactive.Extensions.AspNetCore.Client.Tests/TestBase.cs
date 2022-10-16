using System.Diagnostics;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Client.Tests
{
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "resources are disposed in test teardown")]
    public abstract class TestBase
    {
        private HttpClient? client;
        private ServiceProvider? clientServiceProvider;
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

        protected IServiceProvider ClientServiceProvider
        {
            get
            {
                if (clientServiceProvider == null)
                {
                    throw new InvalidOperationException("test fixture must be initialized before using client service provider");
                }

                return clientServiceProvider;
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

            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                _ = webHost.UseTestServer();

                _ = webHost.ConfigureServices(ConfigureServerServices);
                _ = webHost.Configure(Configure);
            });

            host = await hostBuilder.StartAsync(TestTimeoutToken);
            client = host.GetTestClient();
            webSocketClient = host.GetTestServer().CreateWebSocketClient();

            var services = new ServiceCollection();
            ConfigureClientServices(services);
            clientServiceProvider = services.BuildServiceProvider();

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
            clientServiceProvider?.Dispose();
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
            return ClientServiceProvider.GetRequiredService<T>();
        }

        protected Task<WebSocket> ConnectToWebSocket(string path, string query)
        {
            return WebSocketClient.ConnectAsync(new UriBuilder(HttpClient.BaseAddress!) { Scheme = "ws", Path = path, Query = query }.Uri, CancellationToken.None);
        }
    }
}
