namespace Conqueror.Tests.Messaging;

public abstract partial class MessageHandlerFunctionalityTests
{
    protected abstract IServiceCollection RegisterHandler(IServiceCollection services);

    protected abstract IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services);

    protected virtual IMessageHandler<TestMessage, TestMessageResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IMessageClients>()
                              .For(TestMessage.T);
    }

    protected virtual IMessageHandler<TestMessageWithoutResponse> ResolveHandlerWithoutResponse(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IMessageClients>()
                              .For(TestMessageWithoutResponse.T);
    }

    protected static TestMessage CreateMessage() => new(10);

    protected static TestMessageWithoutResponse CreateMessageWithoutResponse() => new(20);

    protected static TestMessageResponse CreateExpectedResponse() => new(11);

    [Test]
    public async Task GivenMessage_HandlerReceivesMessage()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var message = CreateMessage();

        _ = await handler.Handle(message);

        Assert.That(observations.Messages, Is.EqualTo(new[] { message }));
    }

    [Test]
    public async Task GivenMessageWithoutResponse_HandlerReceivesMessage()
    {
        var observations = new TestObservations();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandlerWithoutResponse(provider);

        var message = CreateMessageWithoutResponse();

        await handler.Handle(message);

        Assert.That(observations.Messages, Is.EqualTo(new[] { message }));
    }

    [Test]
    public async Task GivenCancellationToken_HandlerReceivesCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.Handle(CreateMessage(), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenCancellationTokenForHandlerWithoutResponse_HandlerReceivesCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandlerWithoutResponse(provider);
        using var tokenSource = new CancellationTokenSource();

        await handler.Handle(CreateMessageWithoutResponse(), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenNoCancellationToken_HandlerReceivesDefaultCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        _ = await handler.Handle(CreateMessage());

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { CancellationToken.None }));
    }

    [Test]
    public async Task GivenNoCancellationToken_HandlerWithoutResponseReceivesDefaultCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandlerWithoutResponse(provider);

        await handler.Handle(CreateMessageWithoutResponse());

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { CancellationToken.None }));
    }

    [Test]
    public async Task GivenMessage_HandlerReturnsResponse()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var message = CreateMessage();

        var response = await handler.Handle(message);

        Assert.That(response, Is.EqualTo(CreateExpectedResponse()));
    }

    [Test]
    public void GivenExceptionInHandler_InvocationThrowsSameException()
    {
        var observations = new TestObservations();
        var exception = new Exception();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .AddSingleton(exception)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(CreateMessage()));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenExceptionInHandlerWithoutResponse_InvocationThrowsSameException()
    {
        var observations = new TestObservations();
        var exception = new Exception();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                       .AddSingleton(observations)
                       .AddSingleton(exception)
                       .BuildServiceProvider();

        var handler = ResolveHandlerWithoutResponse(provider);

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(CreateMessageWithoutResponse()));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenHandler_HandlerIsResolvedFromResolutionScope()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = ResolveHandler(scope1.ServiceProvider);
        var handler2 = ResolveHandler(scope2.ServiceProvider);

        _ = await handler1.Handle(CreateMessage());
        _ = await handler1.Handle(CreateMessage());
        _ = await handler2.Handle(CreateMessage());

        Assert.That(observations.ServiceProviders, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProviders[0], Is.SameAs(observations.ServiceProviders[1]));
        Assert.That(observations.ServiceProviders[0], Is.Not.SameAs(observations.ServiceProviders[2]));
    }

    [Test]
    public async Task GivenHandlerWithoutResponse_HandlerIsResolvedFromResolutionScope()
    {
        var observations = new TestObservations();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = ResolveHandlerWithoutResponse(scope1.ServiceProvider);
        var handler2 = ResolveHandlerWithoutResponse(scope2.ServiceProvider);

        await handler1.Handle(CreateMessageWithoutResponse());
        await handler1.Handle(CreateMessageWithoutResponse());
        await handler2.Handle(CreateMessageWithoutResponse());

        Assert.That(observations.ServiceProviders, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProviders[0], Is.SameAs(observations.ServiceProviders[1]));
        Assert.That(observations.ServiceProviders[0], Is.Not.SameAs(observations.ServiceProviders[2]));
    }

    [Test]
    public async Task GivenHandler_WhenCallingItThroughGeneratedHandlerInterface_HandlerReceivesMessage()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider).AsIHandler();

        var message = CreateMessage();

        _ = await handler.Handle(message);

        Assert.That(observations.Messages, Is.EqualTo(new[] { message }));
    }

    [Test]
    public async Task GivenHandlerWithoutResponse_WhenCallingItThroughGeneratedHandlerInterface_HandlerReceivesMessage()
    {
        var observations = new TestObservations();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandlerWithoutResponse(provider).AsIHandler();

        var message = CreateMessageWithoutResponse();

        await handler.Handle(message);

        Assert.That(observations.Messages, Is.EqualTo(new[] { message }));
    }

    [Message<TestMessageResponse>]
    public sealed partial record TestMessage(int Payload);

    public sealed record TestMessageResponse(int Payload);

    [Message]
    public sealed partial record TestMessageWithoutResponse(int Payload);

    public sealed class TestObservations
    {
        public List<object> Messages { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public List<IServiceProvider> ServiceProviders { get; } = [];

        public List<IServiceProvider> ServiceProvidersFromTransportFactory { get; } = [];
    }
}

[TestFixture]
public sealed partial class MessageHandlerFunctionalityDefaultTests : MessageHandlerFunctionalityTests
{
    [Test]
    public async Task GivenDisposableHandler_WhenServiceProviderIsDisposed_ThenHandlerIsDisposed()
    {
        var services = new ServiceCollection();
        var observation = new DisposalObservation();

        _ = services.AddMessageHandler<DisposableMessageHandler>()
                    .AddSingleton(observation);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageClients>()
                              .For(TestMessage.T);

        _ = await handler.Handle(CreateMessage());

        await provider.DisposeAsync();

        Assert.That(observation.WasDisposed, Is.True);
    }

    [Test]
    public async Task GivenHandlerForBaseMessageType_WhenHandlerIsCalledWithMessageSubType_HandlerIsCalledCorrectly()
    {
        var observations = new TestObservations();

        var provider = new ServiceCollection().AddMessageHandler<TestMessageBaseHandler>()
                                              .AddSingleton(observations)
                                              .BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageClients>()
                              .For(TestMessageBase.T);

        var message = new TestMessageSub(10, -1);

        var response = await handler.Handle(message);

        Assert.That(observations.Messages, Is.EqualTo(new[] { message }));
        Assert.That(response, Is.EqualTo(new TestMessageResponse(11)));
    }

    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddMessageHandler<TestMessageHandler>();
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return services.AddMessageHandler<TestMessageWithoutResponseHandler>();
    }

    private sealed class TestMessageHandler(TestObservations observations, IServiceProvider serviceProvider, Exception? exception = null)
        : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
            return new(message.Payload + 1);
        }
    }

    private sealed class TestMessageWithoutResponseHandler(TestObservations observations, IServiceProvider serviceProvider, Exception? exception = null)
        : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }

    private sealed class DisposableMessageHandler(DisposalObservation observation) : TestMessage.IHandler, IDisposable
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(message.Payload);
        }

        public void Dispose() => observation.WasDisposed = true;
    }

    [Message<TestMessageResponse>]
    private partial record TestMessageBase(int PayloadBase);

    private sealed record TestMessageSub(int PayloadBase, int PayloadSub) : TestMessageBase(PayloadBase);

    private sealed class TestMessageBaseHandler(TestObservations observations, IServiceProvider serviceProvider, Exception? exception = null)
        : TestMessageBase.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessageBase message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
            return new(message.PayloadBase + 1);
        }
    }

    private sealed class DisposalObservation
    {
        public bool WasDisposed { get; set; }
    }
}

[TestFixture]
public sealed class MessageHandlerFunctionalityDelegateTests : MessageHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddMessageHandlerDelegate<TestMessage, TestMessageResponse>(async (message, p, cancellationToken) =>
        {
            await Task.Yield();

            var exception = p.GetService<Exception>();
            if (exception is not null)
            {
                throw exception;
            }

            var obs = p.GetRequiredService<TestObservations>();
            obs.Messages.Add(message);
            obs.CancellationTokens.Add(cancellationToken);
            obs.ServiceProviders.Add(p);
            return new(message.Payload + 1);
        });
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return services.AddMessageHandlerDelegate<TestMessageWithoutResponse>(async (message, p, cancellationToken) =>
        {
            await Task.Yield();

            var exception = p.GetService<Exception>();
            if (exception is not null)
            {
                throw exception;
            }

            var obs = p.GetRequiredService<TestObservations>();
            obs.Messages.Add(message);
            obs.CancellationTokens.Add(cancellationToken);
            obs.ServiceProviders.Add(p);
        });
    }
}

[TestFixture]
public sealed class MessageHandlerFunctionalityAssemblyScanningTests : MessageHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddMessageHandlersFromExecutingAssembly();
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return services.AddMessageHandlersFromExecutingAssembly();
    }

    // ReSharper disable once UnusedType.Global (accessed via reflection)
    public sealed class TestMessageForAssemblyScanningHandler(TestObservations observations, IServiceProvider serviceProvider, Exception? exception = null)
        : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
            return new(message.Payload + 1);
        }
    }

    // ReSharper disable once UnusedType.Global (accessed via reflection)
    public sealed class TestMessageWithoutResponseForAssemblyScanningHandler(TestObservations observations, IServiceProvider serviceProvider, Exception? exception = null)
        : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }
}

public abstract class MessageHandlerFunctionalityClientTests : MessageHandlerFunctionalityTests
{
    [Test]
    public async Task GivenHandlerClient_WhenCallingClient_ServiceProviderInTransportBuilderIsFromResolutionScope()
    {
        var observations = new TestObservations();

        await using var provider = RegisterHandler(new ServiceCollection())
                                   .AddSingleton(observations)
                                   .BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = ResolveHandler(scope1.ServiceProvider);
        var handler2 = ResolveHandler(scope2.ServiceProvider);

        _ = await handler1.Handle(CreateMessage());
        _ = await handler1.Handle(CreateMessage());
        _ = await handler2.Handle(CreateMessage());

        Assert.That(observations.ServiceProvidersFromTransportFactory, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProvidersFromTransportFactory[0], Is.SameAs(observations.ServiceProvidersFromTransportFactory[1]));
        Assert.That(observations.ServiceProvidersFromTransportFactory[0], Is.Not.SameAs(observations.ServiceProvidersFromTransportFactory[2]));
    }

    [Test]
    public async Task GivenHandlerClientWithoutResponse_WhenCallingClient_ServiceProviderInTransportBuilderIsFromResolutionScope()
    {
        var observations = new TestObservations();

        await using var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                                   .AddSingleton(observations)
                                   .BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = ResolveHandlerWithoutResponse(scope1.ServiceProvider);
        var handler2 = ResolveHandlerWithoutResponse(scope2.ServiceProvider);

        await handler1.Handle(CreateMessageWithoutResponse());
        await handler1.Handle(CreateMessageWithoutResponse());
        await handler2.Handle(CreateMessageWithoutResponse());

        Assert.That(observations.ServiceProvidersFromTransportFactory, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProvidersFromTransportFactory[0], Is.SameAs(observations.ServiceProvidersFromTransportFactory[1]));
        Assert.That(observations.ServiceProvidersFromTransportFactory[0], Is.Not.SameAs(observations.ServiceProvidersFromTransportFactory[2]));
    }

    [Test]
    public async Task GivenHandlerClientWithInProcessClientIfAvailable_WhenCallingClientWithInProcessAvailable_InProcessTransportIsUsed()
    {
        var observations = new TestObservations();
        var handlerWasCalled = false;

        await using var provider = RegisterHandler(new ServiceCollection())
                                   .AddMessageHandlerDelegate<TestMessage, TestMessageResponse>((message, _, _) =>
                                   {
                                       handlerWasCalled = true;
                                       return new(message.Payload + 1);
                                   })
                                   .AddSingleton(observations)
                                   .BuildServiceProvider();

        var handler = ConfigureWithTransport(ResolveHandler(provider), b => b.UseInProcessIfAvailable());

        _ = await handler.Handle(CreateMessage());

        Assert.That(observations.Messages, Has.Count.Zero);
        Assert.That(handlerWasCalled, Is.True);
    }

    [Test]
    public async Task GivenHandlerClientWithoutResponseWithInProcessClientIfAvailable_WhenCallingClientWithInProcessAvailable_InProcessTransportIsUsed()
    {
        var observations = new TestObservations();
        var handlerWasCalled = false;

        await using var provider = RegisterHandler(new ServiceCollection())
                                   .AddMessageHandlerDelegate<TestMessageWithoutResponse>((_, _, _) => { handlerWasCalled = true; })
                                   .AddSingleton(observations)
                                   .BuildServiceProvider();

        var handler = ConfigureWithTransportWithoutResponse(ResolveHandlerWithoutResponse(provider), b => b.UseInProcessIfAvailable());

        await handler.Handle(CreateMessageWithoutResponse());

        Assert.That(observations.Messages, Has.Count.Zero);
        Assert.That(handlerWasCalled, Is.True);
    }

    [Test]
    public async Task GivenHandlerClientWithInProcessClientIfAvailable_WhenCallingClientWithInProcessNotAvailable_OtherTransportIsUsed()
    {
        var observations = new TestObservations();

        await using var provider = RegisterHandler(new ServiceCollection())
                                   .AddSingleton(observations)
                                   .BuildServiceProvider();

        var handler = ConfigureWithTransport(ResolveHandler(provider), b => b.UseInProcessIfAvailable());

        _ = await handler.Handle(CreateMessage());

        Assert.That(observations.Messages, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GivenHandlerClientWithoutResponseWithInProcessClientIfAvailable_WhenCallingClientWithInProcessNotAvailable_OtherTransportIsUsed()
    {
        var observations = new TestObservations();

        await using var provider = RegisterHandler(new ServiceCollection())
                                   .AddSingleton(observations)
                                   .BuildServiceProvider();

        var handler = ConfigureWithTransportWithoutResponse(ResolveHandlerWithoutResponse(provider), b => b.UseInProcessIfAvailable());

        await handler.Handle(CreateMessageWithoutResponse());

        Assert.That(observations.Messages, Has.Count.EqualTo(1));
    }

    protected abstract IMessageHandler<TestMessage, TestMessageResponse> ConfigureWithTransport(
        IMessageHandler<TestMessage, TestMessageResponse> builder,
        Func<IMessageTransportClientBuilder<TestMessage, TestMessageResponse>, IMessageTransportClient<TestMessage, TestMessageResponse>?>? baseConfigure = null);

    protected abstract IMessageHandler<TestMessageWithoutResponse> ConfigureWithTransportWithoutResponse(
        IMessageHandler<TestMessageWithoutResponse> builder,
        Func<IMessageTransportClientBuilder<TestMessageWithoutResponse, UnitMessageResponse>, IMessageTransportClient<TestMessageWithoutResponse, UnitMessageResponse>?>? baseConfigure = null);

    protected sealed override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddConqueror().AddSingleton(typeof(TestMessageTransport<,>));
    }

    protected sealed override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return services.AddConqueror().AddSingleton(typeof(TestMessageTransport<,>));
    }

    protected sealed override IMessageHandler<TestMessage, TestMessageResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return ConfigureWithTransport(base.ResolveHandler(serviceProvider));
    }

    protected sealed override IMessageHandler<TestMessageWithoutResponse> ResolveHandlerWithoutResponse(IServiceProvider serviceProvider)
    {
        return ConfigureWithTransportWithoutResponse(base.ResolveHandlerWithoutResponse(serviceProvider));
    }

    protected sealed class TestMessageTransport<TMessage, TResponse>(Exception? exception = null) : IMessageTransportClient<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public string TransportTypeName => "test";

        public async Task<TResponse> Send(TMessage message,
                                          IServiceProvider serviceProvider,
                                          ConquerorContext conquerorContext,
                                          CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            var observations = serviceProvider.GetRequiredService<TestObservations>();
            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            if (message is TestMessageWithoutResponse)
            {
                return (TResponse)(object)UnitMessageResponse.Instance;
            }

            var cmd = (TestMessage)(object)message;
            return (TResponse)(object)new TestMessageResponse(cmd.Payload + 1);
        }
    }
}

[TestFixture]
public sealed class MessageHandlerFunctionalityClientWithSyncTransportFactoryTests : MessageHandlerFunctionalityClientTests
{
    protected override IMessageHandler<TestMessage, TestMessageResponse> ConfigureWithTransport(
        IMessageHandler<TestMessage, TestMessageResponse> builder,
        Func<IMessageTransportClientBuilder<TestMessage, TestMessageResponse>, IMessageTransportClient<TestMessage, TestMessageResponse>?>? baseConfigure = null)
    {
        return builder.WithTransport(b =>
        {
            b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
            return baseConfigure?.Invoke(b) ?? b.ServiceProvider.GetRequiredService<TestMessageTransport<TestMessage, TestMessageResponse>>();
        });
    }

    protected override IMessageHandler<TestMessageWithoutResponse> ConfigureWithTransportWithoutResponse(
        IMessageHandler<TestMessageWithoutResponse> builder,
        Func<IMessageTransportClientBuilder<TestMessageWithoutResponse, UnitMessageResponse>, IMessageTransportClient<TestMessageWithoutResponse, UnitMessageResponse>?>? baseConfigure = null)
    {
        return builder.WithTransport(b =>
        {
            b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
            return baseConfigure?.Invoke(b) ?? b.ServiceProvider.GetRequiredService<TestMessageTransport<TestMessageWithoutResponse, UnitMessageResponse>>();
        });
    }
}

[TestFixture]
public sealed class MessageHandlerFunctionalityClientWithAsyncTransportFactoryTests : MessageHandlerFunctionalityClientTests
{
    protected override IMessageHandler<TestMessage, TestMessageResponse> ConfigureWithTransport(
        IMessageHandler<TestMessage, TestMessageResponse> builder,
        Func<IMessageTransportClientBuilder<TestMessage, TestMessageResponse>, IMessageTransportClient<TestMessage, TestMessageResponse>?>? baseConfigure = null)
    {
        return builder.WithTransport(async b =>
        {
            await Task.Delay(1);
            b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
            return baseConfigure?.Invoke(b) ?? b.ServiceProvider.GetRequiredService<TestMessageTransport<TestMessage, TestMessageResponse>>();
        });
    }

    protected override IMessageHandler<TestMessageWithoutResponse> ConfigureWithTransportWithoutResponse(
        IMessageHandler<TestMessageWithoutResponse> builder,
        Func<IMessageTransportClientBuilder<TestMessageWithoutResponse, UnitMessageResponse>, IMessageTransportClient<TestMessageWithoutResponse, UnitMessageResponse>?>? baseConfigure = null)
    {
        return builder.WithTransport(async b =>
        {
            await Task.Delay(1);
            b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
            return baseConfigure?.Invoke(b) ?? b.ServiceProvider.GetRequiredService<TestMessageTransport<TestMessageWithoutResponse, UnitMessageResponse>>();
        });
    }
}
