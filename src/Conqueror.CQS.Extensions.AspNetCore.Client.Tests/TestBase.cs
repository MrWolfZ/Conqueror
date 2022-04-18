using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
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

                _ = webHost.ConfigureServices(ConfigureServerServices);
                _ = webHost.Configure(Configure);
            });

            host = await hostBuilder.StartAsync();
            client = host.GetTestClient();
        }

        [TearDown]
        public void TearDown()
        {
            host?.Dispose();
            HttpClient.Dispose();
        }

        protected abstract void ConfigureServerServices(IServiceCollection services);

        protected abstract void ConfigureClientServices(IServiceCollection services);

        protected abstract void Configure(IApplicationBuilder app);

        protected T Resolve<T>()
            where T : notnull => Host.Services.GetRequiredService<T>();

        protected T ResolveOnClient<T>()
            where T : notnull
        {
            var services = new ServiceCollection();
            ConfigureClientServices(services);
            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<T>();
        }
    }
}
