namespace Conqueror.CQS.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public sealed class CommandClientRegistrationTests
{
    [Test]
    public void GivenRegisteredPlainClient_CanResolvePlainClient()
    {
        using var provider = RegisterClient<ICommandHandler<TestCommand, TestCommandResponse>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
    }

    [Test]
    public void GivenRegisteredPlainClientWithAsyncClientFactory_CanResolvePlainClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<ICommandHandler<TestCommand, TestCommandResponse>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolvePlainClient()
    {
        using var provider = RegisterClient<ITestCommandHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolvePlainClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<ITestCommandHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolveCustomClient()
    {
        using var provider = RegisterClient<ITestCommandHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolveCustomClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<ITestCommandHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
    }

    [Test]
    public void GivenRegisteredPlainClientWithoutResponse_CanResolvePlainClient()
    {
        using var provider = RegisterClient<ICommandHandler<TestCommandWithoutResponse>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
    }

    [Test]
    public void GivenRegisteredPlainClientWithoutResponseWithAsyncClientFactory_CanResolvePlainClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<ICommandHandler<TestCommandWithoutResponse>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithoutResponse_CanResolvePlainClient()
    {
        using var provider = RegisterClient<ITestCommandWithoutResponseHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithoutResponseWithAsyncClientFactory_CanResolvePlainClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<ITestCommandWithoutResponseHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithoutResponse_CanResolveCustomClient()
    {
        using var provider = RegisterClient<ITestCommandWithoutResponseHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandWithoutResponseHandler>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithoutResponseWithAsyncClientFactory_CanResolveCustomClient()
    {
        using var provider = RegisterClientWithAsyncClientFactory<ITestCommandWithoutResponseHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandWithoutResponseHandler>());
    }

    [Test]
    public void GivenUnregisteredPlainClient_ThrowsInvalidOperationException()
    {
        using var provider = RegisterClient<ITestCommandHandler>();
        _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICommandHandler<UnregisteredTestCommand, TestCommandResponse>>());
    }

    [Test]
    public void GivenUnregisteredCustomClient_ThrowsInvalidOperationException()
    {
        using var provider = RegisterClient<ITestCommandHandler>();
        _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IUnregisteredTestCommandHandler>());
    }

    [Test]
    public void GivenUnregisteredPlainClientWithoutResponse_ThrowsInvalidOperationException()
    {
        using var provider = RegisterClient<ITestCommandWithoutResponseHandler>();
        _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICommandHandler<UnregisteredTestCommandWithoutResponse>>());
    }

    [Test]
    public void GivenUnregisteredCustomClientWithoutResponse_ThrowsInvalidOperationException()
    {
        using var provider = RegisterClient<ITestCommandWithoutResponseHandler>();
        _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IUnregisteredTestCommandWithoutResponseHandler>());
    }

    [Test]
    public void GivenRegisteredPlainClient_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
    }

    [Test]
    public void GivenRegisteredPlainClientWithAsyncClientFactory_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
                                              {
                                                  await Task.CompletedTask;
                                                  return new TestCommandTransport();
                                              })
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolveCustomClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
    }

    [Test]
    public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolveCustomClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorCommandClient<ITestCommandHandler>(async _ =>
                                              {
                                                  await Task.CompletedTask;
                                                  return new TestCommandTransport();
                                              })
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>((Func<ICommandTransportClientBuilder, ICommandTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringHandlerWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

        _ = services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => Task.FromException<TestCommandResponse>(new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse2>>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringHandlerWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse2>((_, _, _) => Task.FromResult(new TestCommandResponse2())));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand>>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringHandlerWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandlerDelegate<TestCommand>((_, _, _) => Task.CompletedTask));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => Task.FromException<ICommandTransportClient>(new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => Task.FromException<TestCommandResponse>(new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse2>>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse2>((_, _, _) => Task.FromResult(new TestCommandResponse2())));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand>>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandlerDelegate<TestCommand>((_, _, _) => Task.CompletedTask));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithAsyncFactoryWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithAsyncFactoryWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClient_WhenRegisteringClientWithAsyncFactoryWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringClientWithAsyncFactoryWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringWithAsyncClientFactoryWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringWithAsyncClientFactoryWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = services.AddConquerorCommandClient<ITestCommandHandler>((Func<ICommandTransportClientBuilder, ICommandTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>((Func<ICommandTransportClientBuilder, ICommandTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringHandlerWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => Task.FromException<TestCommandResponse>(new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler2>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringHandlerWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse2>((_, _, _) => Task.FromResult(new TestCommandResponse2())));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler3>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringHandlerWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandlerDelegate<TestCommand>((_, _, _) => Task.CompletedTask));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse2>>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand>>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = services.AddConquerorCommandClient<ITestCommandHandler>((Func<ICommandTransportClientBuilder, ICommandTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>((Func<ICommandTransportClientBuilder, ICommandTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => Task.FromException<TestCommandResponse>(new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler2>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse2>((_, _, _) => Task.FromResult(new TestCommandResponse2())));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler3>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringHandlerWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandlerDelegate<TestCommand>((_, _, _) => Task.CompletedTask));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse2>>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand>>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithAsyncFactoryWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithAsyncFactoryWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithAsyncFactoryWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler2>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringCustomClientWithAsyncFactoryWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler3>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithAsyncFactoryWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClient_WhenRegisteringPlainClientWithAsyncFactoryWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(_ => new TestCommandTransport());

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithAsyncFactoryWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithAsyncFactoryWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithAsyncFactoryWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler2>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringCustomClientWithAsyncFactoryWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler3>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithAsyncFactoryWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringPlainClientWithAsyncFactoryWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        });

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>((Func<ICommandTransportClientBuilder, ICommandTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse2>>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringHandlerWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse2>((_, _, _) => Task.FromResult(new TestCommandResponse2())));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand>>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringHandlerWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandlerDelegate<TestCommand>((_, _, _) => Task.CompletedTask));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithAsyncFactoryWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithAsyncFactoryWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse2>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithAsyncFactoryWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ICommandHandler<TestCommand>>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = services.AddConquerorCommandClient<ITestCommandHandler>((Func<ICommandTransportClientBuilder, ICommandTransportClient>)(_ => throw new TestAssertionException()));

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler2>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler3>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithAsyncFactoryWithSameCommandAndResponseType_OverwritesRegistration()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = services.AddConquerorCommandClient<ITestCommandHandler>(async _ =>
        {
            await Task.CompletedTask;
            throw new TestAssertionException();
        });

        _ = Assert.ThrowsAsync<TestAssertionException>(() => services.BuildServiceProvider().GetRequiredService<ITestCommandHandler>().ExecuteCommand(new()));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithAsyncFactoryWithSameCommandAndDifferentResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler2>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithAsyncFactoryWithSameCommandAndWithoutResponseType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorCommandHandler<TestCommandHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandClient<ITestCommandHandler3>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenCustomInterfaceWithExtraMethods_WhenRegistering_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        _ = services;

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCommandClient<ITestCommandHandlerWithExtraMethod>(_ => new TestCommandTransport()));
    }

    [Test]
    public void GivenCustomInterfaceWithExtraMethods_WhenRegisteringWithAsyncClientFactory_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        _ = services;

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCommandClient<ITestCommandHandlerWithExtraMethod>(async _ =>
        {
            await Task.CompletedTask;
            return new TestCommandTransport();
        }));
    }

    [Test]
    public void GivenClient_CanResolveConquerorContextAccessor()
    {
        using var provider = RegisterClient<ICommandHandler<TestCommand, TestCommandResponse>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IConquerorContextAccessor>());
    }

    private static ServiceProvider RegisterClient<TCommandHandler>()
        where TCommandHandler : class, ICommandHandler
    {
        return new ServiceCollection().AddConquerorCommandClient<TCommandHandler>(_ => new TestCommandTransport())
                                      .BuildServiceProvider();
    }

    private static ServiceProvider RegisterClientWithAsyncClientFactory<TCommandHandler>()
        where TCommandHandler : class, ICommandHandler
    {
        return new ServiceCollection().AddConquerorCommandClient<TCommandHandler>(_ => Task.FromResult(new TestCommandTransport() as ICommandTransportClient))
                                      .BuildServiceProvider();
    }

    public sealed record TestCommand;

    public sealed record TestCommandResponse;

    public sealed record TestCommandResponse2;

    public sealed record TestCommandWithoutResponse;

    public sealed record UnregisteredTestCommand;

    public sealed record UnregisteredTestCommandWithoutResponse;

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
    }

    public interface ITestCommandHandler2 : ICommandHandler<TestCommand, TestCommandResponse2>
    {
    }

    public interface ITestCommandHandler3 : ICommandHandler<TestCommand>
    {
    }

    public interface ITestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
    {
    }

    public interface ITestCommandHandlerWithExtraMethod : ICommandHandler<TestCommand, TestCommandResponse>
    {
        void ExtraMethod();
    }

    public interface IUnregisteredTestCommandHandler : ICommandHandler<UnregisteredTestCommand, TestCommandResponse>
    {
    }

    public interface IUnregisteredTestCommandWithoutResponseHandler : ICommandHandler<UnregisteredTestCommandWithoutResponse>
    {
    }

    private sealed class TestCommandTransport : ICommandTransportClient
    {
        public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command,
                                                                         IServiceProvider serviceProvider,
                                                                         CancellationToken cancellationToken)
            where TCommand : class
        {
            await Task.Yield();

            throw new NotSupportedException("should never be called");
        }
    }

    private sealed class TestCommandHandler : ITestCommandHandler
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
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
