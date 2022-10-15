using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    public abstract class TestBase
    {
        private HttpClient? client;
        private IHost? host;
        private ServiceProvider? clientServiceProvider;

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

        [SetUp]
        public async Task SetUp()
        {
            var hostBuilder = new HostBuilder().ConfigureWebHost(webHost =>
            {
                _ = webHost.UseTestServer();

                _ = webHost.ConfigureServices(ConfigureServerServices);
                _ = webHost.Configure(Configure);
            });

            host = await hostBuilder.StartAsync();
            client = host.GetTestClient();
            
            var services = new ServiceCollection();
            ConfigureClientServices(services);
            clientServiceProvider = services.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            host?.Dispose();
            client?.Dispose();
            clientServiceProvider?.Dispose();
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
    }
}
