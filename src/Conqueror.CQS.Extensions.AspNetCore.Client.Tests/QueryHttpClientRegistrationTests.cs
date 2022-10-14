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
        public void GivenRegisteredPlainClient_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
        {
            var provider = new ServiceCollection().AddConquerorQueryHttpClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new())
                                                  .BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
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
                        .AddConquerorQueryHttpClient<ITestQueryHandler>(_ => expectedBaseAddress);

            using var provider = services.BuildServiceProvider();

            _ = provider.GetRequiredService<ITestQueryHandler>();

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
                        .AddConquerorQueryHttpClient<ITestQueryHandler>(_ => new());

            using var provider = services.BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
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
                        .AddConquerorQueryHttpClient<ITestQueryHandler>(_ => new("http://localhost"));

            _ = services.AddScoped<ScopingTest>();

            using var provider = services.BuildServiceProvider();

            using var scope1 = provider.CreateScope();

            _ = scope1.ServiceProvider.GetRequiredService<ITestQueryHandler>();
            _ = scope1.ServiceProvider.GetRequiredService<ITestQueryHandler>();

            using var scope2 = provider.CreateScope();

            _ = scope2.ServiceProvider.GetRequiredService<ITestQueryHandler>();

            Assert.AreEqual(2, seenInstances.Count);
        }

        [Test]
        public void GivenAlreadyRegisteredHandlerWhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCqsHttpClientServices().AddConquerorQueryHttpClient<ITestQueryHandler>(_ => new());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCqsHttpClientServices().AddConquerorQueryHttpClient<ITestQueryHandler>(_ => new()));
        }

        private ServiceProvider RegisterClient<TQueryHandler>()
            where TQueryHandler : class, IQueryHandler
        {
            return new ServiceCollection().AddConquerorCqsHttpClientServices()
                                          .AddConquerorQueryHttpClient<TQueryHandler>(_ => new())
                                          .BuildServiceProvider();
        }

        private sealed class ScopingTest
        {
        }
    }
}
