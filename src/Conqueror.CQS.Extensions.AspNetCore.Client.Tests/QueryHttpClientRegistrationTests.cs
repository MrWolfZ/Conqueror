using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Extensions.AspNetCore.Client.Tests
{
    [TestFixture]
    public sealed class QueryHttpClientRegistrationTests
    {
        [Test]
        public void GivenRegisteredPlainClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<IQueryHandler<TestQuery, TestQueryResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ITestQueryHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolveCustomClient()
        {
            using var provider = RegisterClient<ITestQueryHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
        }

        [Test]
        public void GivenRegisteredPlainPostClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestPostQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomPostClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ITestPostQueryHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestPostQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomPostClient_CanResolveCustomClient()
        {
            using var provider = RegisterClient<ITestPostQueryHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestPostQueryHandler>());
        }

        [Test]
        public void GivenUnregisteredPlainClient_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClient<ITestQueryHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IQueryHandler<NonHttpTestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenUnregisteredCustomClient_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClient<ITestQueryHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<INonHttpTestQueryHandler>());
        }

        [Test]
        public void GivenNonHttpPlainQueryHandlerType_RegistrationThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => RegisterClient<IQueryHandler<NonHttpTestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenNonHttpCustomQueryHandlerType_RegistrationThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => RegisterClient<INonHttpTestQueryHandler>());
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
                        .AddQueryHttpClient<ITestQueryHandler>();

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestQueryHandler>();

            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void GivenCustomHttpClientFactoryAndNoDefaultFactory_CallsCustomFactory()
        {
            var wasCalled = false;

            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients()
                        .AddQueryHttpClient<ITestQueryHandler>(o =>
                        {
                            o.HttpClientFactory = _ =>
                            {
                                wasCalled = true;
                                return new();
                            };
                        });

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestQueryHandler>();

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
                        .AddQueryHttpClient<ITestQueryHandler>(o =>
                        {
                            o.HttpClientFactory = _ =>
                            {
                                customWasCalled = true;
                                return new();
                            };
                        });

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestQueryHandler>();

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
                        .AddQueryHttpClient<ITestQueryHandler>();

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestQueryHandler>();

            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void GivenCustomJsonSerializerOptionsFactoryAndNoDefaultFactory_CallsCustomFactory()
        {
            var wasCalled = false;

            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients()
                        .AddQueryHttpClient<ITestQueryHandler>(o =>
                        {
                            o.JsonSerializerOptionsFactory = _ =>
                            {
                                wasCalled = true;
                                return new();
                            };
                        });

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestQueryHandler>();

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
                        .AddQueryHttpClient<ITestQueryHandler>(o =>
                        {
                            o.JsonSerializerOptionsFactory = _ =>
                            {
                                customWasCalled = true;
                                return new();
                            };
                        });

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestQueryHandler>();

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
                        .AddQueryHttpClient<ITestQueryHandler>();

            _ = services.AddScoped<ScopingTest>();

            using var provider = services.BuildServiceProvider();

            using var scope1 = provider.CreateScope();

            _ = scope1.ServiceProvider.GetRequiredService<ITestQueryHandler>();

            using var scope2 = provider.CreateScope();

            _ = scope2.ServiceProvider.GetRequiredService<ITestQueryHandler>();

            Assert.AreEqual(2, seenInstances.Count);
        }

        [Test]
        public void GivenAlreadyRegisteredHandlerWhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients().AddQueryHttpClient<ITestQueryHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorHttpClients().AddQueryHttpClient<ITestQueryHandler>());
        }

        private ServiceProvider RegisterClient<TQueryHandler>()
            where TQueryHandler : class, IQueryHandler
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorHttpClients().AddQueryHttpClient<TQueryHandler>();
            return services.BuildServiceProvider();
        }

        private sealed class ScopingTest
        {
        }
    }
}
