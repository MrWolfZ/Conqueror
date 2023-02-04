using System.Text.Json;

namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic interface generation")]
    public sealed class QueryHttpClientRegistrationTests
    {
        [Test]
        public async Task GivenCustomHttpClientFactory_WhenResolvingHandlerRegisteredWithBaseAddressFactory_CallsCustomHttpClientFactory()
        {
            var expectedBaseAddress = new Uri("http://localhost");
            Uri? seenBaseAddress = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o =>
                        {
                            o.HttpClientFactory = baseAddress =>
                            {
                                seenBaseAddress = baseAddress;
                                return new() { BaseAddress = baseAddress };
                            };
                        })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            _ = b.UseHttp(expectedBaseAddress);
                            return new TestQueryTransport();
                        });

            await using var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedBaseAddress, seenBaseAddress);
        }

        [Test]
        public void GivenCustomHttpClientFactory_WhenResolvingHandlerRegisteredWithHttpClientFactory_DoesNotCallCustomHttpClientFactory()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o =>
                        {
                            o.HttpClientFactory = _ =>
                            {
                                Assert.Fail("should not have called factory");
                                return new();
                            };
                        })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            _ = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") });
                            return new TestQueryTransport();
                        });

            using var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            Assert.DoesNotThrowAsync(() => client.ExecuteQuery(new(), CancellationToken.None));
        }

        [Test]
        public async Task GivenCustomHttpClientFactory_CallsFactoryWithScopedServiceProvider()
        {
            var seenInstances = new HashSet<ScopingTest>();

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o =>
                        {
                            o.HttpClientFactory = baseAddress =>
                            {
                                _ = seenInstances.Add(o.ServiceProvider.GetRequiredService<ScopingTest>());
                                return new() { BaseAddress = baseAddress };
                            };
                        })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            _ = b.UseHttp(new Uri("http://localhost"));
                            return new TestQueryTransport();
                        });

            _ = services.AddScoped<ScopingTest>();

            await using var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

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

        [Test]
        public async Task GivenGlobalJsonSerializerOptions_WhenResolvingHandler_UsesGlobalJsonSerializerOptions()
        {
            var expectedOptions = new JsonSerializerOptions();
            JsonSerializerOptions? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.JsonSerializerOptions = expectedOptions; })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") }) as HttpQueryTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            return new TestQueryTransport();
                        });

            await using var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
        }

        [Test]
        public async Task GivenClientJsonSerializerOptions_WhenResolvingHandler_UsesClientJsonSerializerOptions()
        {
            var expectedOptions = new JsonSerializerOptions();
            JsonSerializerOptions? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") }, o => o.JsonSerializerOptions = expectedOptions) as HttpQueryTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            return new TestQueryTransport();
                        });

            await using var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
        }

        [Test]
        public async Task GivenGlobalAndClientJsonSerializerOptions_WhenResolvingHandler_UsesClientJsonSerializerOptions()
        {
            var globalOptions = new JsonSerializerOptions();
            var expectedOptions = new JsonSerializerOptions();
            JsonSerializerOptions? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.JsonSerializerOptions = globalOptions; })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") }, o => o.JsonSerializerOptions = expectedOptions) as HttpQueryTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            return new TestQueryTransport();
                        });

            await using var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
            Assert.AreNotSame(globalOptions, seenOptions);
        }

        [Test]
        public async Task GivenGlobalPathConvention_WhenResolvingHandler_UsesGlobalPathConvention()
        {
            var expectedOptions = new TestHttpQueryPathConvention();
            IHttpQueryPathConvention? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.QueryPathConvention = expectedOptions; })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") }) as HttpQueryTransportClient;
                            seenOptions = httpTransportClient?.Options.QueryPathConvention;
                            return new TestQueryTransport();
                        });

            await using var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
        }

        [Test]
        public async Task GivenClientPathConvention_WhenResolvingHandler_UsesClientPathConvention()
        {
            var expectedOptions = new TestHttpQueryPathConvention();
            IHttpQueryPathConvention? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") }, o => o.PathConvention = expectedOptions) as HttpQueryTransportClient;
                            seenOptions = httpTransportClient?.Options.QueryPathConvention;
                            return new TestQueryTransport();
                        });

            await using var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
        }

        [Test]
        public async Task GivenCustomHttpClientFactoryWhichDoesNotSetClientBaseAddress_WhenResolvingHandler_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.HttpClientFactory = _ => new(); })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            _ = b.UseHttp(new Uri("http://localhost"));
                            return new TestQueryTransport();
                        });

            await using var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteQuery(new(), CancellationToken.None));
        }

        [Test]
        public async Task GivenCustomHttpClientWhichDoesNotHaveBaseAddressSet_WhenResolvingHandler_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            _ = b.UseHttp(new HttpClient());
                            return new TestQueryTransport();
                        });

            await using var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteQuery(new(), CancellationToken.None));
        }

        [Test]
        public async Task GivenGlobalAndClientPathConvention_WhenResolvingHandler_UsesClientPathConvention()
        {
            var globalOptions = new TestHttpQueryPathConvention();
            var expectedOptions = new TestHttpQueryPathConvention();
            IHttpQueryPathConvention? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.QueryPathConvention = globalOptions; })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") }, o => o.PathConvention = expectedOptions) as HttpQueryTransportClient;
                            seenOptions = httpTransportClient?.Options.QueryPathConvention;
                            return new TestQueryTransport();
                        });

            await using var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
            Assert.AreNotSame(globalOptions, seenOptions);
        }

        [HttpQuery]
        public sealed record TestQuery
        {
            public int Payload { get; init; }
        }

        public sealed record TestQueryResponse;

        public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
        }

        private sealed class TestQueryTransport : IQueryTransportClient
        {
            public Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
                where TQuery : class
            {
                return Task.FromResult((TResponse)(object)new TestQueryResponse());
            }
        }

        private sealed class TestHttpQueryPathConvention : IHttpQueryPathConvention
        {
            public string? GetQueryPath(Type queryType, HttpQueryAttribute attribute)
            {
                return null;
            }
        }

        private sealed class ScopingTest
        {
        }
    }
}
