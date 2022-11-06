﻿namespace Conqueror.CQS.Transport.Http.Client.Tests
{
    [TestFixture]
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
                                return new();
                            };
                        })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            _ = b.UseHttp(expectedBaseAddress);
                            return new TestCommandTransport();
                        });

            await using var provider = services.ConfigureConqueror().BuildServiceProvider();

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
                            _ = b.UseHttp(new HttpClient());
                            return new TestCommandTransport();
                        });

            using var provider = services.ConfigureConqueror().BuildServiceProvider();

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
                                return new();
                            };
                        })
                        .AddConquerorCommandClient<ITestCommandHandler>(b =>
                        {
                            _ = b.UseHttp(new Uri("http://localhost"));
                            return new TestCommandTransport();
                        });

            _ = services.AddScoped<ScopingTest>();

            await using var provider = services.ConfigureConqueror().BuildServiceProvider();

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

        private sealed class TestCommandTransport : ICommandTransportClient
        {
            public Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
                where TCommand : class
            {
                return Task.FromResult((TResponse)(object)new TestCommandResponse());
            }
        }

        private sealed class ScopingTest
        {
        }
    }
}