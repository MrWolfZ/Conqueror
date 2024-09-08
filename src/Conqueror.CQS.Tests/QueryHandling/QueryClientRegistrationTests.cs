namespace Conqueror.CQS.Tests.QueryHandling;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public sealed class QueryClientRegistrationTests
{
    [Test]
    public void GivenRegisteredPlainClient_CanResolvePlainClient()
    {
        using var provider = RegisterClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
    }

    [Test]
    public void GivenRegisteredPlainClientWithAsyncClientFactory_CanResolvePlainClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<IQueryHandler<TestQuery, TestQueryResponse>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolvePlainClient()
    {
        using var provider = RegisterClient<ITestQueryHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolvePlainClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<ITestQueryHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolveCustomClient()
    {
        using var provider = RegisterClient<ITestQueryHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolveCustomClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<ITestQueryHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
    }

    [Test]
    public void GivenUnregisteredPlainClient_ThrowsInvalidOperationException()
    {
        using var provider = RegisterClient<ITestQueryHandler>();
        _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IQueryHandler<UnregisteredTestQuery, TestQueryResponse>>());
    }

    [Test]
    public void GivenUnregisteredCustomClient_ThrowsInvalidOperationException()
    {
        using var provider = RegisterClient<ITestQueryHandler>();
        _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IUnregisteredTestQueryHandler>());
    }

    [Test]
    public void GivenRegisteredPlainClient_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
    }

    [Test]
    public void GivenRegisteredPlainClientWithAsyncClientFactory_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
                                              {
                                                  await Task.CompletedTask;
                                                  return new TestQueryTransport();
                                              })
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolveCustomClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolveCustomClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorQueryClient<ITestQueryHandler>(async _ =>
                                              {
                                                  await Task.CompletedTask;
                                                  return new TestQueryTransport();
                                              })
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport());

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>((Func<IQueryTransportClientBuilder, IQueryTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringHandlerWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport());

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => Task.FromException<TestQueryResponse>(new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse2>>(_ => new TestQueryTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringHandlerWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse2>((_, _, _) => Task.FromResult(new TestQueryResponse2())));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => Task.FromException<IQueryTransportClient>(new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => Task.FromException<TestQueryResponse>(new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse2>>(_ => new TestQueryTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse2>((_, _, _) => Task.FromResult(new TestQueryResponse2())));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithAsyncFactoryWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport());

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithAsyncFactoryWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithAsyncFactoryWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringWithAsyncClientFactoryWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

        _ = services.AddConquerorQueryClient<ITestQueryHandler>((Func<IQueryTransportClientBuilder, IQueryTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>((Func<IQueryTransportClientBuilder, IQueryTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringHandlerWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => Task.FromException<TestQueryResponse>(new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler2>(_ => new TestQueryTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringHandlerWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse2>((_, _, _) => Task.FromResult(new TestQueryResponse2())));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse2>>(_ => new TestQueryTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = services.AddConquerorQueryClient<ITestQueryHandler>((Func<IQueryTransportClientBuilder, IQueryTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>((Func<IQueryTransportClientBuilder, IQueryTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => Task.FromException<TestQueryResponse>(new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler2>(_ => new TestQueryTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse2>((_, _, _) => Task.FromResult(new TestQueryResponse2())));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse2>>(_ => new TestQueryTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithAsyncFactoryWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithAsyncFactoryWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithAsyncFactoryWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler2>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithAsyncFactoryWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithAsyncFactoryWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithAsyncFactoryWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithAsyncFactoryWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler2>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithAsyncFactoryWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryHandler<TestQueryHandler>();

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>((Func<IQueryTransportClientBuilder, IQueryTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryHandler<TestQueryHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse2>>(_ => new TestQueryTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringHandlerWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryHandler<TestQueryHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse2>((_, _, _) => Task.FromResult(new TestQueryResponse2())));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithAsyncFactoryWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryHandler<TestQueryHandler>();

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithAsyncFactoryWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryHandler<TestQueryHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryHandler<TestQueryHandler>();

        _ = services.AddConquerorQueryClient<ITestQueryHandler>((Func<IQueryTransportClientBuilder, IQueryTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryHandler<TestQueryHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler2>(_ => new TestQueryTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithAsyncFactoryWithSameQueryAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryHandler<TestQueryHandler>();

        _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestQueryHandler>().ExecuteQuery(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithAsyncFactoryWithSameQueryAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorQueryHandler<TestQueryHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler2>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        }));
    }

    [Test]
    public void GivenCustomInterfaceWithExtraMethods_WhenRegistering_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        _ = services;

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorQueryClient<ITestQueryHandlerWithExtraMethod>(_ => new TestQueryTransport()));
    }

    [Test]
    public void GivenCustomInterfaceWithExtraMethods_WhenRegisteringWithAsyncClientFactory_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        _ = services;

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorQueryClient<ITestQueryHandlerWithExtraMethod>(async _ =>
        {
            await Task.CompletedTask;
            return new TestQueryTransport();
        }));
    }

    [Test]
    public void GivenClient_CanResolveConquerorContextAccessor()
    {
        using var provider = RegisterClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IConquerorContextAccessor>());
    }

    private static ServiceProvider RegisterClient<TQueryHandler>()
        where TQueryHandler : class, IQueryHandler
    {
        return new ServiceCollection().AddConquerorQueryClient<TQueryHandler>(_ => new TestQueryTransport())
                                      .BuildServiceProvider();
    }

    private static ServiceProvider RegisterClientWithAsyncClientFactory<TQueryHandler>()
        where TQueryHandler : class, IQueryHandler
    {
        return new ServiceCollection().AddConquerorQueryClient<TQueryHandler>(_ => Task.FromResult(new TestQueryTransport() as IQueryTransportClient))
                                      .BuildServiceProvider();
    }

    public sealed record TestQuery;

    public sealed record TestQueryResponse;

    public sealed record TestQueryResponse2;

    public sealed record TestQueryWithoutResponse;

    public sealed record UnregisteredTestQuery;

    public sealed record UnregisteredTestQueryWithoutResponse;

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
    }

    public interface ITestQueryHandler2 : IQueryHandler<TestQuery, TestQueryResponse2>
    {
    }

    public interface ITestQueryHandlerWithExtraMethod : IQueryHandler<TestQuery, TestQueryResponse>
    {
        void ExtraMethod();
    }

    public interface IUnregisteredTestQueryHandler : IQueryHandler<UnregisteredTestQuery, TestQueryResponse>
    {
    }

    private sealed class TestQueryTransport : IQueryTransportClient
    {
        public QueryTransportType TransportType { get; } = new("test", QueryTransportRole.Client);

        public async Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query,
                                                                     IServiceProvider serviceProvider,
                                                                     CancellationToken cancellationToken)
            where TQuery : class
        {
            await Task.Yield();

            throw new NotSupportedException("should never be called");
        }
    }

    private sealed class TestQueryHandler : ITestQueryHandler
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            throw new NotSupportedException("should never be called");
        }
    }

    [SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "test code")]
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "test code")]
    private sealed class TestAssertionException : Exception
    {
    }
}
