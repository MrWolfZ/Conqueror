namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    [TestFixture]
    public sealed class QueryHttpClientRegistrationTests
    {
        [Test]
        public async Task GivenCustomHttpClientFactory_WhenResolvingHandlerRegisteredWithBaseAddressFactory_CallsCustomHttpClientFactory()
        {
            var expectedBaseAddress = new Uri("http://localhost");
            Uri? seenBaseAddress = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCqsHttpClientServices(o =>
                        {
                            o.HttpClientFactory = baseAddress =>
                            {
                                seenBaseAddress = baseAddress;
                                return new();
                            };
                        })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            _ = b.UseHttp(expectedBaseAddress);
                            return new TestQueryTransport();
                        });

            await using var provider = services.ConfigureConqueror().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedBaseAddress, seenBaseAddress);
        }

        [Test]
        public void GivenCustomHttpClientFactory_WhenResolvingHandlerRegisteredWithHttpClientFactory_DoesNotCallCustomHttpClientFactory()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCqsHttpClientServices(o =>
                        {
                            o.HttpClientFactory = _ =>
                            {
                                Assert.Fail("should not have called factory");
                                return new();
                            };
                        })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            _ = b.UseHttp(new HttpClient());
                            return new TestQueryTransport();
                        });

            using var provider = services.ConfigureConqueror().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            Assert.DoesNotThrowAsync(() => client.ExecuteQuery(new(), CancellationToken.None));
        }

        [Test]
        public async Task GivenCustomHttpClientFactory_CallsFactoryWithScopedServiceProvider()
        {
            var seenInstances = new HashSet<ScopingTest>();

            var services = new ServiceCollection();
            _ = services.AddConquerorCqsHttpClientServices(o =>
                        {
                            o.HttpClientFactory = baseAddress =>
                            {
                                _ = seenInstances.Add(o.ServiceProvider.GetRequiredService<ScopingTest>());
                                return new();
                            };
                        })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            _ = b.UseHttp(new Uri("http://localhost"));
                            return new TestQueryTransport();
                        });

            _ = services.AddScoped<ScopingTest>();

            await using var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();

            var client1 = scope1.ServiceProvider.GetRequiredService<ITestQueryHandler>();
            var client2 = scope1.ServiceProvider.GetRequiredService<ITestQueryHandler>();

            _ = await client1.ExecuteQuery(new(), CancellationToken.None);
            _ = await client2.ExecuteQuery(new(), CancellationToken.None);

            using var scope2 = provider.CreateScope();

            var client3 = scope2.ServiceProvider.GetRequiredService<ITestQueryHandler>();

            _ = await client3.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreEqual(2, seenInstances.Count);
        }

        private sealed class TestQueryTransport : IQueryTransportClient
        {
            public Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
                where TQuery : class
            {
                return Task.FromResult((TResponse)(object)new TestQueryResponse());
            }
        }

        private sealed class ScopingTest
        {
        }
    }
}
