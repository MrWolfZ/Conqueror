using System.Text.Json;

namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic interface generation")]
    public sealed class CommandHttpClientRegistrationTests
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
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            _ = b.UseHttp(expectedBaseAddress);
                            return new TestCommandTransport();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

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
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            _ = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") });
                            return new TestCommandTransport();
                        });

            using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            Assert.DoesNotThrowAsync(() => client.ExecuteCommand(new(), CancellationToken.None));
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
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            _ = b.UseHttp(new Uri("http://localhost"));
                            return new TestCommandTransport();
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
        public async Task GivenGlobalJsonSerializerOptions_WhenResolvingHandler_UsesGlobalJsonSerializerOptions()
        {
            var expectedOptions = new JsonSerializerOptions();
            JsonSerializerOptions? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.JsonSerializerOptions = expectedOptions; })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") }) as HttpCommandTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            return new TestCommandTransport();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
        }

        [Test]
        public async Task GivenClientJsonSerializerOptions_WhenResolvingHandler_UsesClientJsonSerializerOptions()
        {
            var expectedOptions = new JsonSerializerOptions();
            JsonSerializerOptions? seenOptions = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") }, o => o.JsonSerializerOptions = expectedOptions) as HttpCommandTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            return new TestCommandTransport();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

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
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") }, o => o.JsonSerializerOptions = expectedOptions) as HttpCommandTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            return new TestCommandTransport();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
            Assert.AreNotSame(globalOptions, seenOptions);
        }

        [Test]
        public async Task GivenGlobalPathConvention_WhenResolvingHandler_UsesGlobalPathConvention()
        {
            var expectedConvention = new TestHttpCommandPathConvention();
            IHttpCommandPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.CommandPathConvention = expectedConvention; })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") }) as HttpCommandTransportClient;
                            seenConvention = httpTransportClient?.Options.CommandPathConvention;
                            return new TestCommandTransport();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedConvention, seenConvention);
        }

        [Test]
        public async Task GivenClientPathConvention_WhenResolvingHandler_UsesClientPathConvention()
        {
            var expectedConvention = new TestHttpCommandPathConvention();
            IHttpCommandPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") }, o => o.PathConvention = expectedConvention) as HttpCommandTransportClient;
                            seenConvention = httpTransportClient?.Options.CommandPathConvention;
                            return new TestCommandTransport();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedConvention, seenConvention);
        }

        [Test]
        public async Task GivenGlobalAndClientPathConvention_WhenResolvingHandler_UsesClientPathConvention()
        {
            var globalConvention = new TestHttpCommandPathConvention();
            var expectedConvention = new TestHttpCommandPathConvention();
            IHttpCommandPathConvention? seenConvention = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.CommandPathConvention = globalConvention; })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new HttpClient { BaseAddress = new("http://localhost") }, o => o.PathConvention = expectedConvention) as HttpCommandTransportClient;
                            seenConvention = httpTransportClient?.Options.CommandPathConvention;
                            return new TestCommandTransport();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedConvention, seenConvention);
            Assert.AreNotSame(globalConvention, seenConvention);
        }

        [Test]
        public async Task GivenMultipleOptionsConfigurationsFromAddServices_WhenResolvingHandler_UsesMergedOptions()
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
                        .AddConquerorCQSHttpClientServices(o =>
                        {
                            o.JsonSerializerOptions = expectedOptions;
                        })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new Uri("http://localhost")) as HttpCommandTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            seenConvention = httpTransportClient?.Options.CommandPathConvention;
                            return new TestCommandTransport();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
            Assert.AreNotSame(unexpectedOptions, seenOptions);
            Assert.AreSame(expectedConvention, seenConvention);
        }

        [Test]
        public async Task GivenMultipleOptionsConfigurations_WhenResolvingHandler_UsesMergedOptions()
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
                        .ConfigureConquerorCQSHttpClientOptions(o =>
                        {
                            o.JsonSerializerOptions = expectedOptions;
                        })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            var httpTransportClient = b.UseHttp(new Uri("http://localhost")) as HttpCommandTransportClient;
                            seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                            seenConvention = httpTransportClient?.Options.CommandPathConvention;
                            return new TestCommandTransport();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = await client.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreSame(expectedOptions, seenOptions);
            Assert.AreNotSame(unexpectedOptions, seenOptions);
            Assert.AreSame(expectedConvention, seenConvention);
        }

        [Test]
        public async Task GivenCustomHttpClientFactoryWhichDoesNotSetClientBaseAddress_WhenResolvingHandler_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices(o => { o.HttpClientFactory = _ => new(); })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            _ = b.UseHttp(new Uri("http://localhost"));
                            return new TestCommandTransport();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteCommand(new(), CancellationToken.None));
        }

        [Test]
        public async Task GivenCustomHttpClientWhichDoesNotHaveBaseAddressSet_WhenResolvingHandler_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQSHttpClientServices()
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            _ = b.UseHttp(new HttpClient());
                            return new TestCommandTransport();
                        });

            await using var provider = services.BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteCommand(new(), CancellationToken.None));
        }

        [HttpCommand]
        public sealed record TestCommand
        {
            public int Payload { get; init; }
        }

        public sealed record TestCommandResponse;

        public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
        }

        private sealed class TestCommandTransport : ICommandTransportClient
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
    }
}
