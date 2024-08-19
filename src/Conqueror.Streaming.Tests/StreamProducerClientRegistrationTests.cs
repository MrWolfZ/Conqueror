using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public sealed class StreamProducerClientRegistrationTests
{
    [Test]
    public void GivenRegisteredPlainClient_CanResolvePlainClient()
    {
        using var provider = RegisterClient<IStreamProducer<TestStreamingRequest, TestItem>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredPlainClientWithAsyncClientFactory_CanResolvePlainClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<IStreamProducer<TestStreamingRequest, TestItem>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolvePlainClient()
    {
        using var provider = RegisterClient<ITestStreamProducer>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolvePlainClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<ITestStreamProducer>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolveCustomClient()
    {
        using var provider = RegisterClient<ITestStreamProducer>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamProducer>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolveCustomClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<ITestStreamProducer>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamProducer>());
    }

    [Test]
    public void GivenUnregisteredPlainClient_ThrowsInvalidOperationException()
    {
        using var provider = RegisterClient<ITestStreamProducer>();
        _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IStreamProducer<UnregisteredTestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenUnregisteredCustomClient_ThrowsInvalidOperationException()
    {
        using var provider = RegisterClient<ITestStreamProducer>();
        _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IUnregisteredTestStreamProducer>());
    }

    [Test]
    public void GivenRegisteredPlainClient_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(_ => new TestStreamProducerTransport())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredPlainClientWithAsyncClientFactory_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(async _ =>
                                              {
                                                  await Task.CompletedTask;
                                                  return new TestStreamProducerTransport();
                                              })
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolveCustomClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducerClient<ITestStreamProducer>(_ => new TestStreamProducerTransport())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamProducer>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolveCustomClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
                                              {
                                                  await Task.CompletedTask;
                                                  return new TestStreamProducerTransport();
                                              })
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamProducer>());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(_ => new TestStreamProducerTransport());

        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>((Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringProducerWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(_ => new TestStreamProducerTransport());

        _ = services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((_, _, _) => throw new TestAssertionException());

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(_ => new TestStreamProducerTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem2>>(_ => new TestStreamProducerTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringProducerWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(_ => new TestStreamProducerTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem2>((_, _, _) => AsyncEnumerableHelper.Empty<TestItem2>()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(_ => Task.FromException<IStreamProducerTransportClient>(new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringProducerWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((_, _, _) => throw new TestAssertionException());

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem2>>(_ => new TestStreamProducerTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringProducerWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem2>((_, _, _) => AsyncEnumerableHelper.Empty<TestItem2>()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(_ => new TestStreamProducerTransport());

        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(_ => new TestStreamProducerTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringWithAsyncClientFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(_ => new TestStreamProducerTransport());

        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>((Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(_ => new TestStreamProducerTransport());

        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>((Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringProducerWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(_ => new TestStreamProducerTransport());

        _ = services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((_, _, _) => throw new TestAssertionException());

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(_ => new TestStreamProducerTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<ITestStreamProducer2>(_ => new TestStreamProducerTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringProducerWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(_ => new TestStreamProducerTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem2>((_, _, _) => AsyncEnumerableHelper.Empty<TestItem2>()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(_ => new TestStreamProducerTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem2>>(_ => new TestStreamProducerTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>((Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>((Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringProducerWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((_, _, _) => throw new TestAssertionException());

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<ITestStreamProducer2>(_ => new TestStreamProducerTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringProducerWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem2>((_, _, _) => AsyncEnumerableHelper.Empty<TestItem2>()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem2>>(_ => new TestStreamProducerTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(_ => new TestStreamProducerTransport());

        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(_ => new TestStreamProducerTransport());

        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(_ => new TestStreamProducerTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<ITestStreamProducer2>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(_ => new TestStreamProducerTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<ITestStreamProducer2>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredProducer_WhenRegisteringPlainClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducer<TestStreamProducer>();

        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>((Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredProducer_WhenRegisteringPlainClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducer<TestStreamProducer>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem2>>(_ => new TestStreamProducerTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredProducer_WhenRegisteringProducerWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducer<TestStreamProducer>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem2>((_, _, _) => AsyncEnumerableHelper.Empty<TestItem2>()));
    }

    [Test]
    public void GivenAlreadyRegisteredProducer_WhenRegisteringPlainClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducer<TestStreamProducer>();

        _ = services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredProducer_WhenRegisteringPlainClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducer<TestStreamProducer>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<IStreamProducer<TestStreamingRequest, TestItem2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredProducer_WhenRegisteringCustomClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducer<TestStreamProducer>();

        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>((Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredProducer_WhenRegisteringCustomClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducer<TestStreamProducer>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<ITestStreamProducer2>(_ => new TestStreamProducerTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredProducer_WhenRegisteringCustomClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducer<TestStreamProducer>();

        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamProducer>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredProducer_WhenRegisteringCustomClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamProducer<TestStreamProducer>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducerClient<ITestStreamProducer2>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        }));
    }

    [Test]
    public void GivenCustomInterfaceWithExtraMethods_WhenRegistering_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        _ = services;

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorStreamProducerClient<ITestStreamProducerWithExtraMethod>(_ => new TestStreamProducerTransport()));
    }

    [Test]
    public void GivenCustomInterfaceWithExtraMethods_WhenRegisteringWithAsyncClientFactory_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        _ = services;

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorStreamProducerClient<ITestStreamProducerWithExtraMethod>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamProducerTransport();
        }));
    }

    [Test]
    public void GivenClient_CanResolveConquerorContextAccessor()
    {
        using var provider = RegisterClient<IStreamProducer<TestStreamingRequest, TestItem>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IConquerorContextAccessor>());
    }

    private static ServiceProvider RegisterClient<TProducer>()
        where TProducer : class, IStreamProducer
    {
        return new ServiceCollection().AddConquerorStreamProducerClient<TProducer>(_ => new TestStreamProducerTransport())
                                      .BuildServiceProvider();
    }

    private static ServiceProvider RegisterClientWithAsyncClientFactory<TProducer>()
        where TProducer : class, IStreamProducer
    {
        return new ServiceCollection().AddConquerorStreamProducerClient<TProducer>(_ => Task.FromResult(new TestStreamProducerTransport() as IStreamProducerTransportClient))
                                      .BuildServiceProvider();
    }

    public sealed record TestStreamingRequest;

    public sealed record TestItem;

    public sealed record TestItem2;

    public sealed record TestStreamingRequestWithoutResponse;

    public sealed record UnregisteredTestStreamingRequest;

    public sealed record UnregisteredTestStreamingRequestWithoutResponse;

    public interface ITestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
    {
    }

    public interface ITestStreamProducer2 : IStreamProducer<TestStreamingRequest, TestItem2>
    {
    }

    public interface ITestStreamProducerWithExtraMethod : IStreamProducer<TestStreamingRequest, TestItem>
    {
        void ExtraMethod();
    }

    public interface IUnregisteredTestStreamProducer : IStreamProducer<UnregisteredTestStreamingRequest, TestItem>
    {
    }

    private sealed class TestStreamProducerTransport : IStreamProducerTransportClient
    {
        public async IAsyncEnumerable<TItem> ExecuteRequest<TRequest, TItem>(TRequest request,
                                                                             IServiceProvider serviceProvider,
                                                                             [EnumeratorCancellation] CancellationToken cancellationToken)
            where TRequest : class
        {
            await Task.Yield();

            if (request != null)
            {
                throw new NotSupportedException("should never be called");
            }

            yield break;
        }
    }

    private sealed class TestStreamProducer : ITestStreamProducer
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (request != null)
            {
                throw new NotSupportedException("should never be called");
            }

            yield break;
        }
    }

    [SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "test code")]
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "test code")]
    private sealed class TestAssertionException : Exception
    {
    }
}
