using System.Net;
using System.Text.Json;

namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic interface generation")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "false positive")]
    public sealed class QueryHttpClientRegistrationTests
    {
        [Test]
        public async Task GivenCustomHttpClient_WhenResolvingClient_UsesCustomHttpClient()
        {
            using var expectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClient(expectedHttpClient))
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpQueryTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedHttpClient, seenHttpClient);
        }

        [Test]
        public async Task GivenCustomHttpClientWithBaseAddress_WhenResolvingClient_UsesCustomHttpClientsBaseAddress()
        {
            var expectedBaseAddress = new Uri("http://expected.localhost");
            var unexpectedBaseAddress = new Uri("http://unexpected.localhost");
            using var testClient = new TestHttpClient { BaseAddress = expectedBaseAddress };

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClient(testClient))
                        .AddConquerorQueryClient<ITestQueryHandler>(b => b.UseHttp(unexpectedBaseAddress));

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.IsNotNull(testClient.LastSeenRequestUri);
            Assert.IsTrue(expectedBaseAddress.IsBaseOf(testClient.LastSeenRequestUri!));
            Assert.IsFalse(unexpectedBaseAddress.IsBaseOf(testClient.LastSeenRequestUri!));
        }

        [Test]
        public async Task GivenCustomHttpClientWithoutBaseAddress_WhenResolvingClient_UsesProvidedBaseAddress()
        {
            var expectedBaseAddress = new Uri("http://expected.localhost");
            using var testClient = new TestHttpClient();

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClient(testClient))
                        .AddConquerorQueryClient<ITestQueryHandler>(b => b.UseHttp(expectedBaseAddress));

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.IsNotNull(testClient.LastSeenRequestUri);
            Assert.IsTrue(expectedBaseAddress.IsBaseOf(testClient.LastSeenRequestUri!));
        }

        [Test]
        public async Task GivenCustomHttpClientForSameQueryType_WhenResolvingClient_UsesCustomHttpClient()
        {
            using var expectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClientForQuery<TestQuery>(expectedHttpClient))
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpQueryTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedHttpClient, seenHttpClient);
        }

        [Test]
        public async Task GivenGlobalCustomHttpClientAndCustomHttpClientForSameQueryType_WhenResolvingClient_UsesCustomHttpClientForQueryType()
        {
            using var expectedHttpClient = new HttpClient();
            using var unexpectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClient(unexpectedHttpClient).UseHttpClientForQuery<TestQuery>(expectedHttpClient))
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpQueryTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedHttpClient, seenHttpClient);
            Assert.AreNotSame(unexpectedHttpClient, seenHttpClient);
        }

        // test different order of calling UseHttpClientForQuery and UseHttpClient
        [Test]
        public async Task GivenCustomHttpClientForSameQueryTypeAndGlobalCustomHttpClient_WhenResolvingClient_UsesCustomHttpClientForQueryType()
        {
            using var expectedHttpClient = new HttpClient();
            using var unexpectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClientForQuery<TestQuery>(expectedHttpClient).UseHttpClient(unexpectedHttpClient))
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpQueryTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedHttpClient, seenHttpClient);
            Assert.AreNotSame(unexpectedHttpClient, seenHttpClient);
        }

        [Test]
        public async Task GivenGlobalCustomHttpClientAndCustomHttpClientForDifferentQueryType_WhenResolvingClient_UsesGlobalCustomHttpClient()
        {
            using var expectedHttpClient = new HttpClient();
            using var unexpectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClient(expectedHttpClient).UseHttpClientForQuery<TestQuery2>(unexpectedHttpClient))
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpQueryTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedHttpClient, seenHttpClient);
            Assert.AreNotSame(unexpectedHttpClient, seenHttpClient);
        }

        [Test]
        public async Task GivenCustomHttpClientForDifferentQueryType_WhenResolvingClient_DoesNotUseCustomHttpClient()
        {
            using var unexpectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClientForQuery<TestQuery2>(unexpectedHttpClient))
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpQueryTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreNotSame(unexpectedHttpClient, seenHttpClient);
        }

        [Test]
        public async Task GivenCustomHttpClientForCommandType_WhenResolvingClient_DoesNotUseCustomHttpClient()
        {
            using var unexpectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClientForCommand<TestQuery>(unexpectedHttpClient))
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpQueryTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreNotSame(unexpectedHttpClient, seenHttpClient);
        }

        [Test]
        public async Task GivenCustomConfiguration_WhenResolvingClient_ConfiguresOptionsWithScopedServiceProvider()
        {
            var seenInstances = new HashSet<ScopingTest>();

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { _ = seenInstances.Add(o.ServiceProvider.GetRequiredService<ScopingTest>()); })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            _ = b.UseHttp(new("http://localhost"));
                            return new TestQueryTransportClient();
                        });

            _ = services.AddScoped<ScopingTest>();

            await using var provider = services.BuildServiceProvider();

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
        public async Task GivenGlobalJsonSerializerOptions_WhenExecutingHandler_UsesGlobalJsonSerializerOptions()
        {
            var expectedOptions = new JsonSerializerOptions();
            JsonSerializerOptions? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.JsonSerializerOptions = expectedOptions; })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost")) as HttpQueryTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
        }

        [Test]
        public async Task GivenClientJsonSerializerOptions_WhenExecutingHandler_UsesClientJsonSerializerOptions()
        {
            var expectedOptions = new JsonSerializerOptions();
            JsonSerializerOptions? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost"), o => o.JsonSerializerOptions = expectedOptions) as HttpQueryTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
        }

        [Test]
        public async Task GivenGlobalAndClientJsonSerializerOptions_WhenExecutingHandler_UsesClientJsonSerializerOptions()
        {
            var globalOptions = new JsonSerializerOptions();
            var expectedOptions = new JsonSerializerOptions();
            JsonSerializerOptions? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.JsonSerializerOptions = globalOptions; })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost"), o => o.JsonSerializerOptions = expectedOptions) as HttpQueryTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
            Assert.AreNotSame(globalOptions, seenOptions);
        }

        [Test]
        public async Task GivenGlobalPathConvention_WhenExecutingHandler_UsesGlobalPathConvention()
        {
            var expectedConvention = new TestHttpQueryPathConvention();
            IHttpQueryPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.QueryPathConvention = expectedConvention; })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost")) as HttpQueryTransportClient;
                            seenConvention = httpTransportClient?.Options.QueryPathConvention;
                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedConvention, seenConvention);
        }

        [Test]
        public async Task GivenClientPathConvention_WhenExecutingHandler_UsesClientPathConvention()
        {
            var expectedConvention = new TestHttpQueryPathConvention();
            IHttpQueryPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost"), o => o.PathConvention = expectedConvention) as HttpQueryTransportClient;
                            seenConvention = httpTransportClient?.Options.QueryPathConvention;
                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedConvention, seenConvention);
        }

        [Test]
        public async Task GivenGlobalAndClientPathConvention_WhenExecutingHandler_UsesClientPathConvention()
        {
            var globalConvention = new TestHttpQueryPathConvention();
            var expectedConvention = new TestHttpQueryPathConvention();
            IHttpQueryPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.QueryPathConvention = globalConvention; })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost"), o => o.PathConvention = expectedConvention) as HttpQueryTransportClient;
                            seenConvention = httpTransportClient?.Options.QueryPathConvention;
                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedConvention, seenConvention);
            Assert.AreNotSame(globalConvention, seenConvention);
        }

        [Test]
        public async Task GivenMultipleOptionsConfigurationsFromAddServices_WhenExecutingHandler_UsesMergedOptions()
        {
            var unexpectedOptions = new JsonSerializerOptions();
            var expectedOptions = new JsonSerializerOptions();
            var expectedConvention = new TestHttpQueryPathConvention();
            JsonSerializerOptions? seenOptions = null;
            IHttpQueryPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o =>
                        {
                            o.JsonSerializerOptions = unexpectedOptions;
                            o.QueryPathConvention = expectedConvention;
                        })
                        .AddConquerorCQSHttpClientServices(o => { o.JsonSerializerOptions = expectedOptions; })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost")) as HttpQueryTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            seenConvention = httpTransportClient?.Options.QueryPathConvention;
                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
            Assert.AreNotSame(unexpectedOptions, seenOptions);
            Assert.AreSame(expectedConvention, seenConvention);
        }

        [Test]
        public async Task GivenMultipleOptionsConfigurations_WhenExecutingHandler_UsesMergedOptions()
        {
            var unexpectedOptions = new JsonSerializerOptions();
            var expectedOptions = new JsonSerializerOptions();
            var expectedConvention = new TestHttpQueryPathConvention();
            JsonSerializerOptions? seenOptions = null;
            IHttpQueryPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o =>
                        {
                            o.JsonSerializerOptions = unexpectedOptions;
                            o.QueryPathConvention = expectedConvention;
                        })
                        .ConfigureConquerorCQSHttpClientOptions(o => { o.JsonSerializerOptions = expectedOptions; })
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost")) as HttpQueryTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            seenConvention = httpTransportClient?.Options.QueryPathConvention;
                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = await client.ExecuteQuery(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
            Assert.AreNotSame(unexpectedOptions, seenOptions);
            Assert.AreSame(expectedConvention, seenConvention);
        }

        [Test]
        public async Task GivenClientConfigurationWithRelativeBaseAddress_WhenExecutingHandler_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorQueryClient<ITestQueryHandler>(b => b.UseHttp(new("/", UriKind.Relative)));

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteQuery(new(), CancellationToken.None));
        }

        [Test]
        public async Task GivenCustomHttpClientWithBaseAddressAndClientConfigurationWithRelativeBaseAddress_WhenExecutingHandler_DoesNotThrowException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClient(new() { BaseAddress = new("http://localhost") }))
                        .AddConquerorQueryClient<ITestQueryHandler>(b =>
                        {
                            _ = b.UseHttp(new("/", UriKind.Relative));
                            return new TestQueryTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            Assert.DoesNotThrowAsync(() => client.ExecuteQuery(new(), CancellationToken.None));
        }

        [Test]
        public async Task GivenClientConfigurationWithNullBaseAddress_WhenExecutingHandler_ThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorQueryClient<ITestQueryHandler>(b => b.UseHttp(null!));

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            var thrownException = Assert.ThrowsAsync<ArgumentNullException>(() => client.ExecuteQuery(new(), CancellationToken.None));

            Assert.That(thrownException?.ParamName, Is.EqualTo("baseAddress"));
        }

        [Test]
        public async Task GivenNonHttpPlainHandlerInterface_WhenExecutingHandler_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorQueryClient<IQueryHandler<NonHttpTestQuery, TestQueryResponse>>(b => b.UseHttp(new("http://localhost")));

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<IQueryHandler<NonHttpTestQuery, TestQueryResponse>>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteQuery(new(), CancellationToken.None));
        }

        [Test]
        public async Task GivenNonHttpCustomHandlerInterface_WhenExecutingHandler_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorQueryClient<INonHttpTestQueryHandler>(b => b.UseHttp(new("http://localhost")));

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<INonHttpTestQueryHandler>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteQuery(new(), CancellationToken.None));
        }

        [HttpQuery]
        public sealed record TestQuery
        {
            public int Payload { get; init; }
        }

        public sealed record TestQueryResponse;

        [HttpQuery]
        public sealed record TestQuery2;

        public sealed record NonHttpTestQuery;

        public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
        }

        public interface INonHttpTestQueryHandler : IQueryHandler<NonHttpTestQuery, TestQueryResponse>
        {
        }

        private sealed class TestQueryTransportClient : IQueryTransportClient
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

        private sealed class TestHttpClient : HttpClient
        {
            public Uri? LastSeenRequestUri { get; set; }

            public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastSeenRequestUri = request.RequestUri;

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}"),
                };

                return Task.FromResult(response);
            }
        }
    }
}
