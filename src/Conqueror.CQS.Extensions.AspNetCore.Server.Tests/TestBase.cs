using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Conqueror.CQS.Extensions.AspNetCore.Server.Tests
{
    public abstract class TestBase
    {
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

                _ = webHost.ConfigureServices(ConfigureServices);
                _ = webHost.Configure(Configure);
            });

            host = await hostBuilder.StartAsync();
            client = host.GetTestClient();
        }

        [TearDown]
        public void TearDown()
        {
            host?.Dispose();
            client?.Dispose();
        }

        protected abstract void ConfigureServices(IServiceCollection services);

        protected abstract void Configure(IApplicationBuilder app);

        protected T Resolve<T>()
            where T : notnull => Host.Services.GetRequiredService<T>();
    }
}
