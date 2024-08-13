using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public sealed class StreamingRequestClientRegistrationTests
{
    [Test]
    public void GivenRegisteredPlainClient_CanResolvePlainClient()
    {
        using var provider = RegisterClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredPlainClientWithAsyncClientFactory_CanResolvePlainClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolvePlainClient()
    {
        using var provider = RegisterClient<ITestStreamingRequestHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolvePlainClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<ITestStreamingRequestHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolveCustomClient()
    {
        using var provider = RegisterClient<ITestStreamingRequestHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingRequestHandler>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolveCustomClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<ITestStreamingRequestHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingRequestHandler>());
    }

    [Test]
    public void GivenUnregisteredPlainClient_ThrowsInvalidOperationException()
    {
        using var provider = RegisterClient<ITestStreamingRequestHandler>();
        _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IStreamingRequestHandler<UnregisteredTestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenUnregisteredCustomClient_ThrowsInvalidOperationException()
    {
        using var provider = RegisterClient<ITestStreamingRequestHandler>();
        _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IUnregisteredTestStreamingRequestHandler>());
    }

    [Test]
    public void GivenRegisteredPlainClient_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(_ => new TestStreamingRequestTransport())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredPlainClientWithAsyncClientFactory_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(async _ =>
                                              {
                                                  await Task.CompletedTask;
                                                  return new TestStreamingRequestTransport();
                                              })
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolveCustomClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(_ => new TestStreamingRequestTransport())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingRequestHandler>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolveCustomClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
                                              {
                                                  await Task.CompletedTask;
                                                  return new TestStreamingRequestTransport();
                                              })
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingRequestHandler>());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(_ => new TestStreamingRequestTransport());

        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>((Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringHandlerWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(_ => new TestStreamingRequestTransport());

        _ = services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((_, _, _) => throw new TestAssertionException());

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(_ => new TestStreamingRequestTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem2>>(_ => new TestStreamingRequestTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringHandlerWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(_ => new TestStreamingRequestTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem2>((_, _, _) => AsyncEnumerableHelper.Empty<TestItem2>()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(_ => Task.FromException<IStreamingRequestTransportClient>(new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((_, _, _) => throw new TestAssertionException());

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem2>>(_ => new TestStreamingRequestTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem2>((_, _, _) => AsyncEnumerableHelper.Empty<TestItem2>()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(_ => new TestStreamingRequestTransport());

        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(_ => new TestStreamingRequestTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringWithAsyncClientFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(_ => new TestStreamingRequestTransport());

        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>((Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(_ => new TestStreamingRequestTransport());

        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>((Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringHandlerWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(_ => new TestStreamingRequestTransport());

        _ = services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((_, _, _) => throw new TestAssertionException());

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(_ => new TestStreamingRequestTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler2>(_ => new TestStreamingRequestTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringHandlerWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(_ => new TestStreamingRequestTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem2>((_, _, _) => AsyncEnumerableHelper.Empty<TestItem2>()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(_ => new TestStreamingRequestTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem2>>(_ => new TestStreamingRequestTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>((Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>((Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem>((_, _, _) => throw new TestAssertionException());

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler2>(_ => new TestStreamingRequestTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem2>((_, _, _) => AsyncEnumerableHelper.Empty<TestItem2>()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem2>>(_ => new TestStreamingRequestTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(_ => new TestStreamingRequestTransport());

        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(_ => new TestStreamingRequestTransport());

        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(_ => new TestStreamingRequestTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler2>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(_ => new TestStreamingRequestTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler2>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>((Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem2>>(_ => new TestStreamingRequestTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringHandlerWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestHandlerDelegate<TestStreamingRequest, TestItem2>((_, _, _) => AsyncEnumerableHelper.Empty<TestItem2>()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        _ = services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>((Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler2>(_ => new TestStreamingRequestTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithAsyncFactoryWithSameRequestAndItemType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestStreamingRequestHandler>().ExecuteRequest(new()).Drain());
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithAsyncFactoryWithSameRequestAndDifferentItemType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler2>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        }));
    }

    [Test]
    public void GivenCustomInterfaceWithExtraMethods_WhenRegistering_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        _ = services;

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandlerWithExtraMethod>(_ => new TestStreamingRequestTransport()));
    }

    [Test]
    public void GivenCustomInterfaceWithExtraMethods_WhenRegisteringWithAsyncClientFactory_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        _ = services;

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandlerWithExtraMethod>(async _ =>
        {
            await Task.CompletedTask;
            return new TestStreamingRequestTransport();
        }));
    }

    [Test]
    public void GivenClient_CanResolveConquerorContextAccessor()
    {
        using var provider = RegisterClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IConquerorContextAccessor>());
    }

    private static ServiceProvider RegisterClient<TRequestHandler>()
        where TRequestHandler : class, IStreamingRequestHandler
    {
        return new ServiceCollection().AddConquerorStreamingRequestClient<TRequestHandler>(_ => new TestStreamingRequestTransport())
                                      .BuildServiceProvider();
    }

    private static ServiceProvider RegisterClientWithAsyncClientFactory<TRequestHandler>()
        where TRequestHandler : class, IStreamingRequestHandler
    {
        return new ServiceCollection().AddConquerorStreamingRequestClient<TRequestHandler>(_ => Task.FromResult(new TestStreamingRequestTransport() as IStreamingRequestTransportClient))
                                      .BuildServiceProvider();
    }

    public sealed record TestStreamingRequest;

    public sealed record TestItem;

    public sealed record TestItem2;

    public sealed record TestStreamingRequestWithoutResponse;

    public sealed record UnregisteredTestStreamingRequest;

    public sealed record UnregisteredTestStreamingRequestWithoutResponse;

    public interface ITestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
    }

    public interface ITestStreamingRequestHandler2 : IStreamingRequestHandler<TestStreamingRequest, TestItem2>
    {
    }

    public interface ITestStreamingRequestHandlerWithExtraMethod : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        void ExtraMethod();
    }

    public interface IUnregisteredTestStreamingRequestHandler : IStreamingRequestHandler<UnregisteredTestStreamingRequest, TestItem>
    {
    }

    private sealed class TestStreamingRequestTransport : IStreamingRequestTransportClient
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

    private sealed class TestStreamingRequestHandler : ITestStreamingRequestHandler
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
