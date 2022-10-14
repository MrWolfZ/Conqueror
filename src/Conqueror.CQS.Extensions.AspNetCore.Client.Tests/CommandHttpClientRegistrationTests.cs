using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    [TestFixture]
    public sealed class CommandHttpClientRegistrationTests
    {
        [Test]
        public void GivenRegisteredPlainClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ITestCommandHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolveCustomClient()
        {
            using var provider = RegisterClient<ITestCommandHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
        }

        [Test]
        public void GivenRegisteredPlainClientWithoutResponse_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ICommandHandler<TestCommandWithoutResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClientWithoutResponse_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ITestCommandWithoutResponseHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClientWithoutResponse_CanResolveCustomClient()
        {
            using var provider = RegisterClient<ITestCommandWithoutResponseHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandWithoutResponseHandler>());
        }

        [Test]
        public void GivenUnregisteredPlainClient_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClient<ITestCommandHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICommandHandler<NonHttpTestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenUnregisteredCustomClient_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClient<ITestCommandHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<INonHttpTestCommandHandler>());
        }

        [Test]
        public void GivenNonHttpPlainCommandHandlerType_RegistrationThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => RegisterClient<ICommandHandler<NonHttpTestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenNonHttpCustomCommandHandlerType_RegistrationThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => RegisterClient<INonHttpTestCommandHandler>());
        }

        [Test]
        public void GivenNonHttpPlainCommandHandlerTypeWithoutResponse_RegistrationThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => RegisterClient<ICommandHandler<NonHttpTestCommandWithoutResponse>>());
        }

        [Test]
        public void GivenNonHttpCustomCommandHandlerTypeWithoutResponse_RegistrationThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => RegisterClient<INonHttpTestCommandWithoutResponseHandler>());
        }

        [Test]
        public void GivenRegisteredPlainClient_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
        {
            var provider = new ServiceCollection().AddConquerorCommandHttpClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new())
                                                  .BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }

        [Test]
        public void GivenCustomHttpClientFactory_WhenResolvingHandlerRegisteredWithBaseAddressFactory_CallsCustomHttpClientFactory()
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
                        .AddConquerorCommandHttpClient<ITestCommandHandler>(_ => expectedBaseAddress);

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestCommandHandler>();

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
                        .AddConquerorCommandHttpClient<ITestCommandHandler>(_ => new());

            using var provider = services.BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
        }

        [Test]
        public void GivenCustomHttpClientFactory_CallsFactoryWithScopedServiceProvider()
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
                        .AddConquerorCommandHttpClient<ITestCommandHandler>(_ => new("http://localhost"));

            _ = services.AddScoped<ScopingTest>();

            using var provider = services.BuildServiceProvider();

            using var scope1 = provider.CreateScope();

            _ = scope1.ServiceProvider.GetRequiredService<ITestCommandHandler>();
            _ = scope1.ServiceProvider.GetRequiredService<ITestCommandHandler>();

            using var scope2 = provider.CreateScope();

            _ = scope2.ServiceProvider.GetRequiredService<ITestCommandHandler>();

            Assert.AreEqual(2, seenInstances.Count);
        }

        [Test]
        public void GivenAlreadyRegisteredHandlerWhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCqsHttpClientServices().AddConquerorCommandHttpClient<ITestCommandHandler>(_ => new());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCqsHttpClientServices().AddConquerorCommandHttpClient<ITestCommandHandler>(_ => new()));
        }

        [Test]
        public void GivenClient_CanResolveConquerorContextAccessor()
        {
            using var provider = RegisterClient<ICommandHandler<TestCommand, TestCommandResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IConquerorContextAccessor>());
        }

        private ServiceProvider RegisterClient<TCommandHandler>()
            where TCommandHandler : class, ICommandHandler
        {
            return new ServiceCollection().AddConquerorCqsHttpClientServices()
                                          .AddConquerorCommandHttpClient<TCommandHandler>(_ => new())
                                          .BuildServiceProvider();
        }

        private sealed class ScopingTest
        {
        }
    }
}
