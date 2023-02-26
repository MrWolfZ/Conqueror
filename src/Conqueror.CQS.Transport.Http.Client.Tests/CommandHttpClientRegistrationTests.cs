using System.Net;
using System.Text.Json;

namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic interface generation")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "false positive")]
    public sealed class CommandHttpClientRegistrationTests
    {
        [Test]
        public async Task GivenCustomHttpClient_WhenResolvingClient_UsesCustomHttpClient()
        {
            using var expectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClient(expectedHttpClient))
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpCommandTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

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
                        .AddConquerorCommandClient<ITestCommandHandler>(b => b.UseHttp(unexpectedBaseAddress));

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

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
                        .AddConquerorCommandClient<ITestCommandHandler>(b => b.UseHttp(expectedBaseAddress));

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.IsNotNull(testClient.LastSeenRequestUri);
            Assert.IsTrue(expectedBaseAddress.IsBaseOf(testClient.LastSeenRequestUri!));
        }

        [Test]
        public async Task GivenCustomHttpClientForSameCommandType_WhenResolvingClient_UsesCustomHttpClient()
        {
            using var expectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClientForCommand<TestCommand>(expectedHttpClient))
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpCommandTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedHttpClient, seenHttpClient);
        }

        [Test]
        public async Task GivenGlobalCustomHttpClientAndCustomHttpClientForSameCommandType_WhenResolvingClient_UsesCustomHttpClientForCommandType()
        {
            using var expectedHttpClient = new HttpClient();
            using var unexpectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClient(unexpectedHttpClient).UseHttpClientForCommand<TestCommand>(expectedHttpClient))
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpCommandTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedHttpClient, seenHttpClient);
            Assert.AreNotSame(unexpectedHttpClient, seenHttpClient);
        }

        // test different order of calling UseHttpClientForCommand and UseHttpClient
        [Test]
        public async Task GivenCustomHttpClientForSameCommandTypeAndGlobalCustomHttpClient_WhenResolvingClient_UsesCustomHttpClientForCommandType()
        {
            using var expectedHttpClient = new HttpClient();
            using var unexpectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClientForCommand<TestCommand>(expectedHttpClient).UseHttpClient(unexpectedHttpClient))
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpCommandTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedHttpClient, seenHttpClient);
            Assert.AreNotSame(unexpectedHttpClient, seenHttpClient);
        }

        [Test]
        public async Task GivenGlobalCustomHttpClientAndCustomHttpClientForDifferentCommandType_WhenResolvingClient_UsesGlobalCustomHttpClient()
        {
            using var expectedHttpClient = new HttpClient();
            using var unexpectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClient(expectedHttpClient).UseHttpClientForCommand<TestCommand2>(unexpectedHttpClient))
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpCommandTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedHttpClient, seenHttpClient);
            Assert.AreNotSame(unexpectedHttpClient, seenHttpClient);
        }

        [Test]
        public async Task GivenCustomHttpClientForDifferentCommandType_WhenResolvingClient_DoesNotUseCustomHttpClient()
        {
            using var unexpectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClientForCommand<TestCommand2>(unexpectedHttpClient))
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpCommandTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreNotSame(unexpectedHttpClient, seenHttpClient);
        }

        [Test]
        public async Task GivenCustomHttpClientForQueryType_WhenResolvingClient_DoesNotUseCustomHttpClient()
        {
            using var unexpectedHttpClient = new HttpClient();
            HttpClient? seenHttpClient = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClientForQuery<TestCommand>(unexpectedHttpClient))
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var transportClient = b.UseHttp(new("http://localhost")) as HttpCommandTransportClient;

                            seenHttpClient = transportClient?.Options.HttpClient;

                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreNotSame(unexpectedHttpClient, seenHttpClient);
        }

        [Test]
        public async Task GivenCustomConfiguration_WhenResolvingClient_ConfiguresOptionsWithScopedServiceProvider()
        {
            var seenInstances = new HashSet<ScopingTest>();

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { _ = seenInstances.Add(o.ServiceProvider.GetRequiredService<ScopingTest>()); })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            _ = b.UseHttp(new("http://localhost"));
                            return new TestCommandTransportClient();
                        });

            _ = services.AddScoped<ScopingTest>();

            await using var provider = services.BuildServiceProvider();

            using var scope1 = provider.CreateScope();

            var client1 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandler>();
            var client2 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandler>();

            _ = await client1.ExecuteCommand(new(), CancellationToken.None);
            _ = await client2.ExecuteCommand(new(), CancellationToken.None);

            using var scope2 = provider.CreateScope();

            var client3 = scope2.ServiceProvider.GetRequiredService<ITestCommandHandler>();

            _ = await client3.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreEqual(2, seenInstances.Count);
        }

        [Test]
        public async Task GivenGlobalJsonSerializerOptions_WhenExecutingHandler_UsesGlobalJsonSerializerOptions()
        {
            var expectedOptions = new JsonSerializerOptions();
            JsonSerializerOptions? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.JsonSerializerOptions = expectedOptions; })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost")) as HttpCommandTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
        }

        [Test]
        public async Task GivenClientJsonSerializerOptions_WhenExecutingHandler_UsesClientJsonSerializerOptions()
        {
            var expectedOptions = new JsonSerializerOptions();
            JsonSerializerOptions? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost"), o => o.JsonSerializerOptions = expectedOptions) as HttpCommandTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

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
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost"), o => o.JsonSerializerOptions = expectedOptions) as HttpCommandTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
            Assert.AreNotSame(globalOptions, seenOptions);
        }

        [Test]
        public async Task GivenGlobalPathConvention_WhenExecutingHandler_UsesGlobalPathConvention()
        {
            var expectedConvention = new TestHttpCommandPathConvention();
            IHttpCommandPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.CommandPathConvention = expectedConvention; })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost")) as HttpCommandTransportClient;
                            seenConvention = httpTransportClient?.Options.CommandPathConvention;
                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedConvention, seenConvention);
        }

        [Test]
        public async Task GivenClientPathConvention_WhenExecutingHandler_UsesClientPathConvention()
        {
            var expectedConvention = new TestHttpCommandPathConvention();
            IHttpCommandPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost"), o => o.PathConvention = expectedConvention) as HttpCommandTransportClient;
                            seenConvention = httpTransportClient?.Options.CommandPathConvention;
                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedConvention, seenConvention);
        }

        [Test]
        public async Task GivenGlobalAndClientPathConvention_WhenExecutingHandler_UsesClientPathConvention()
        {
            var globalConvention = new TestHttpCommandPathConvention();
            var expectedConvention = new TestHttpCommandPathConvention();
            IHttpCommandPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.CommandPathConvention = globalConvention; })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost"), o => o.PathConvention = expectedConvention) as HttpCommandTransportClient;
                            seenConvention = httpTransportClient?.Options.CommandPathConvention;
                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedConvention, seenConvention);
            Assert.AreNotSame(globalConvention, seenConvention);
        }

        [Test]
        public async Task GivenMultipleOptionsConfigurationsFromAddServices_WhenExecutingHandler_UsesMergedOptions()
        {
            var unexpectedOptions = new JsonSerializerOptions();
            var expectedOptions = new JsonSerializerOptions();
            var expectedConvention = new TestHttpCommandPathConvention();
            JsonSerializerOptions? seenOptions = null;
            IHttpCommandPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o =>
                        {
                            o.JsonSerializerOptions = unexpectedOptions;
                            o.CommandPathConvention = expectedConvention;
                        })
                        .AddConquerorCQSHttpClientServices(o => { o.JsonSerializerOptions = expectedOptions; })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost")) as HttpCommandTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            seenConvention = httpTransportClient?.Options.CommandPathConvention;
                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
            Assert.AreNotSame(unexpectedOptions, seenOptions);
            Assert.AreSame(expectedConvention, seenConvention);
        }

        [Test]
        public async Task GivenMultipleOptionsConfigurations_WhenExecutingHandler_UsesMergedOptions()
        {
            var unexpectedOptions = new JsonSerializerOptions();
            var expectedOptions = new JsonSerializerOptions();
            var expectedConvention = new TestHttpCommandPathConvention();
            JsonSerializerOptions? seenOptions = null;
            IHttpCommandPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o =>
                        {
                            o.JsonSerializerOptions = unexpectedOptions;
                            o.CommandPathConvention = expectedConvention;
                        })
                        .ConfigureConquerorCQSHttpClientOptions(o => { o.JsonSerializerOptions = expectedOptions; })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new("http://localhost")) as HttpCommandTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            seenConvention = httpTransportClient?.Options.CommandPathConvention;
                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
            Assert.AreNotSame(unexpectedOptions, seenOptions);
            Assert.AreSame(expectedConvention, seenConvention);
        }

        [Test]
        public async Task GivenClientConfigurationWithRelativeBaseAddress_WhenExecutingHandler_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            _ = b.UseHttp(new("/", UriKind.Relative));
                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteCommand(new(), CancellationToken.None));
        }

        [Test]
        public async Task GivenCustomHttpClientWithBaseAddressAndClientConfigurationWithRelativeBaseAddress_WhenExecutingHandler_DoesNotThrowException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => o.UseHttpClient(new() { BaseAddress = new("http://localhost") }))
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            _ = b.UseHttp(new("/", UriKind.Relative));
                            return new TestCommandTransportClient();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            Assert.DoesNotThrowAsync(() => client.ExecuteCommand(new(), CancellationToken.None));
        }

        [HttpCommand]
        public sealed record TestCommand
        {
            public int Payload { get; init; }
        }

        public sealed record TestCommandResponse;

        [HttpCommand]
        public sealed record TestCommand2;

        public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
        }

        private sealed class TestCommandTransportClient : ICommandTransportClient
        {
            public Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
                where TCommand : class
            {
                return Task.FromResult((TResponse)(object)new TestCommandResponse());
            }
        }

        private sealed class TestHttpCommandPathConvention : IHttpCommandPathConvention
        {
            public string? GetCommandPath(Type commandType, HttpCommandAttribute attribute)
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
