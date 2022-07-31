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
        public void GivenDefaultHttpClientFactoryAndNoCustomFactory_CallsDefaultFactory()
        {
            var wasCalled = false;

            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients()
                        .ConfigureDefaultHttpClientOptions(o =>
                        {
                            o.HttpClientFactory = _ =>
                            {
                                wasCalled = true;
                                return new();
                            };
                        })
                        .AddCommandHttpClient<ITestCommandHandler>();

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestCommandHandler>();

            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void GivenCustomHttpClientFactoryAndNoDefaultFactory_CallsCustomFactory()
        {
            var wasCalled = false;

            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients()
                        .AddCommandHttpClient<ITestCommandHandler>(o =>
                        {
                            o.HttpClientFactory = _ =>
                            {
                                wasCalled = true;
                                return new();
                            };
                        });

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestCommandHandler>();

            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void GivenCustomAndDefaultHttpClientFactory_CallsCustomFactory()
        {
            var defaultWasCalled = false;
            var customWasCalled = false;

            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients()
                        .ConfigureDefaultHttpClientOptions(o =>
                        {
                            o.HttpClientFactory = _ =>
                            {
                                defaultWasCalled = true;
                                return new();
                            };
                        })
                        .AddCommandHttpClient<ITestCommandHandler>(o =>
                        {
                            o.HttpClientFactory = _ =>
                            {
                                customWasCalled = true;
                                return new();
                            };
                        });

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestCommandHandler>();

            Assert.IsFalse(defaultWasCalled);
            Assert.IsTrue(customWasCalled);
        }

        [Test]
        public void GivenDefaultJsonSerializerOptionsFactoryAndNoCustomFactory_CallsDefaultFactory()
        {
            var wasCalled = false;

            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients()
                        .ConfigureDefaultHttpClientOptions(o =>
                        {
                            o.JsonSerializerOptionsFactory = _ =>
                            {
                                wasCalled = true;
                                return new();
                            };
                        })
                        .AddCommandHttpClient<ITestCommandHandler>();

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestCommandHandler>();

            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void GivenCustomJsonSerializerOptionsFactoryAndNoDefaultFactory_CallsCustomFactory()
        {
            var wasCalled = false;

            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients()
                        .AddCommandHttpClient<ITestCommandHandler>(o =>
                        {
                            o.JsonSerializerOptionsFactory = _ =>
                            {
                                wasCalled = true;
                                return new();
                            };
                        });

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestCommandHandler>();

            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void GivenCustomAndDefaultJsonSerializerOptionsFactory_CallsCustomFactory()
        {
            var defaultWasCalled = false;
            var customWasCalled = false;

            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients()
                        .ConfigureDefaultHttpClientOptions(o =>
                        {
                            o.JsonSerializerOptionsFactory = _ =>
                            {
                                defaultWasCalled = true;
                                return new();
                            };
                        })
                        .AddCommandHttpClient<ITestCommandHandler>(o =>
                        {
                            o.JsonSerializerOptionsFactory = _ =>
                            {
                                customWasCalled = true;
                                return new();
                            };
                        });

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestCommandHandler>();

            Assert.IsFalse(defaultWasCalled);
            Assert.IsTrue(customWasCalled);
        }

        [Test]
        public void GivenDefaultHttpClientFactory_CallsDefaultFactoryWithScopedServiceProvider()
        {
            var seenInstances = new HashSet<ScopingTest>();

            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients()
                        .ConfigureDefaultHttpClientOptions(o =>
                        {
                            o.HttpClientFactory = p =>
                            {
                                _ = seenInstances.Add(p.GetRequiredService<ScopingTest>());
                                return new();
                            };
                        })
                        .AddCommandHttpClient<ITestCommandHandler>();

            _ = services.AddScoped<ScopingTest>();

            using var provider = services.BuildServiceProvider();

            using var scope1 = provider.CreateScope();

            _ = scope1.ServiceProvider.GetRequiredService<ITestCommandHandler>();

            using var scope2 = provider.CreateScope();

            _ = scope2.ServiceProvider.GetRequiredService<ITestCommandHandler>();

            Assert.AreEqual(2, seenInstances.Count);
        }

        [Test]
        public void GivenAlreadyRegisteredHandlerWhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients().AddCommandHttpClient<ITestCommandHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorHttpClients().AddCommandHttpClient<ITestCommandHandler>());
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
            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients().AddCommandHttpClient<TCommandHandler>();
            return services.BuildServiceProvider();
        }

        private sealed class ScopingTest
        {
        }
    }
}
