namespace Conqueror.Tests.Messaging;

public abstract partial class MessageHandlerFunctionalityTests
{
    [Test]
    public async Task GivenMessage_HandlerReceivesMessage()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var message = CreateMessage();

        _ = await handler.Handle(message, CancellationToken.None);

        Assert.That(observations.Messages, Is.EqualTo([message]));
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

        await handler.Handle(message, CancellationToken.None);

        Assert.That(observations.Messages, Is.EqualTo([message]));
    }

    [Test]
    public async Task GivenCancellationToken_HandlerReceivesCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        var handler = ResolveHandler(provider);
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.Handle(CreateMessage(), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EqualTo([tokenSource.Token]));
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

        Assert.That(observations.CancellationTokens, Is.EqualTo([tokenSource.Token]));
    }

    [Test]
    public async Task GivenNoCancellationToken_HandlerReceivesDefaultCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        var handler = ResolveHandler(provider);

        _ = await handler.Handle(CreateMessage(), CancellationToken.None);

        Assert.That(observations.CancellationTokens, Is.EqualTo([CancellationToken.None]));
    }

    [Test]
    public async Task GivenNoCancellationToken_HandlerWithoutResponseReceivesDefaultCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
            .AddSingleton(observations)
            .BuildServiceProvider();

        var handler = ResolveHandlerWithoutResponse(provider);

        await handler.Handle(CreateMessageWithoutResponse(), CancellationToken.None);

        Assert.That(observations.CancellationTokens, Is.EqualTo([CancellationToken.None]));
    }

    [Test]
    public async Task GivenMessage_HandlerReturnsResponse()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var message = CreateMessage();

        var response = await handler.Handle(message, CancellationToken.None);

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

        var thrownException = Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(CreateMessage(), CancellationToken.None)
        );

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

        var thrownException = Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(CreateMessageWithoutResponse(), CancellationToken.None)
        );

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenHandler_HandlerIsResolvedFromResolutionScope()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();

        var handler1 = ResolveHandler(scope1.ServiceProvider);
        var handler2 = ResolveHandler(scope2.ServiceProvider);

        _ = await handler1.Handle(CreateMessage(), CancellationToken.None);
        _ = await handler1.Handle(CreateMessage(), CancellationToken.None);
        _ = await handler2.Handle(CreateMessage(), CancellationToken.None);

        Assert.That(observations.ServiceProviders, Has.Count.EqualTo(expected: 3));
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

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();

        var handler1 = ResolveHandlerWithoutResponse(scope1.ServiceProvider);
        var handler2 = ResolveHandlerWithoutResponse(scope2.ServiceProvider);

        await handler1.Handle(CreateMessageWithoutResponse(), CancellationToken.None);
        await handler1.Handle(CreateMessageWithoutResponse(), CancellationToken.None);
        await handler2.Handle(CreateMessageWithoutResponse(), CancellationToken.None);

        Assert.That(observations.ServiceProviders, Has.Count.EqualTo(expected: 3));
        Assert.That(observations.ServiceProviders[0], Is.SameAs(observations.ServiceProviders[1]));
        Assert.That(observations.ServiceProviders[0], Is.Not.SameAs(observations.ServiceProviders[2]));
    }

    protected abstract IServiceCollection RegisterHandler(IServiceCollection services);

    protected abstract IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services);

    protected virtual TestMessage.IHandler ResolveHandler(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<IMessageSenders>().For(TestMessage.T);

    protected virtual TestMessageWithoutResponse.IHandler ResolveHandlerWithoutResponse(
        IServiceProvider serviceProvider
    ) => serviceProvider.GetRequiredService<IMessageSenders>().For(TestMessageWithoutResponse.T);

    protected static TestMessage CreateMessage() => new(Payload: 10);

    protected static TestMessageWithoutResponse CreateMessageWithoutResponse() => new(Payload: 20);

    protected static TestMessageResponse CreateExpectedResponse() => new(Payload: 11);

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

        _ = services.AddMessageHandler<DisposableMessageHandler>().AddSingleton(observation);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>().For(TestMessage.T);

        _ = await handler.Handle(CreateMessage(), CancellationToken.None);

        await provider.DisposeAsync();

        Assert.That(observation.WasDisposed, Is.True);
    }

    [Test]
    public async Task GivenHandlerForMultipleMessageTypes_WhenCalledWithMessageOfEitherType_HandlerReceivesMessage()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandler<MultiTestMessageHandler>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler1 = provider.GetRequiredService<IMessageSenders>().For(TestMessage.T);
        var handler2 = provider.GetRequiredService<IMessageSenders>().For(TestMessage2.T);

        var message1 = new TestMessage(Payload: 10);
        var message2 = new TestMessage2(Payload: 20);

        _ = await handler1.Handle(message1, CancellationToken.None);
        _ = await handler2.Handle(message2, CancellationToken.None);

        Assert.That(observations.Messages, Is.EqualTo(new object[] { message1, message2 }));
    }

    [Test]
    public async Task GivenHandlerForBaseMessageType_WhenHandlerIsCalledWithMessageSubType_HandlerIsCalledCorrectly()
    {
        var observations = new TestObservations();

        var provider = new ServiceCollection()
            .AddMessageHandler<TestMessageBaseHandler>()
            .AddSingleton(observations)
            .BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>().For(TestMessageBase.T);

        var message = new TestMessageSub(PayloadBase: 10, PayloadSub: -1);

        var response = await handler.Handle(message, CancellationToken.None);

        Assert.That(observations.Messages, Is.EqualTo([message]));
        Assert.That(response, Is.EqualTo(new TestMessageResponse(Payload: 11)));
    }

    [Test]
    [Combinatorial]
    public async Task GivenHandlerWithInProcessReceiverConfiguration_WhenHandlerIsCalledMultipleTimes_ReceiverIsConfiguredCorrectly(
        [Values(arg1: true, arg2: false)] bool isDisabled,
        [Values(arg1: true, arg2: false)] bool configurePerMessage
    )
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var configureCallCount = 0;

        _ = services
            .AddMessageHandler<TestMessageHandlerWithInProcessReceiverConfiguration>()
            .AddSingleton<Action<IInProcessMessageReceiver>>(r =>
            {
                configureCallCount += 1;

                if (configurePerMessage)
                {
                    _ = r.ConfigureOnEveryMessage();
                }

                if (isDisabled)
                {
                    r.Disable();
                }
            })
            .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>().For(TestMessage.T);

        async Task Call()
        {
            try
            {
                _ = await handler.Handle(CreateMessage(), CancellationToken.None);
            }
            catch when (isDisabled)
            {
                // nothing to do
            }
        }

        await Call();
        await Call();
        await Call();

        Assert.That(configureCallCount, Is.EqualTo(configurePerMessage ? 3 : 1));
    }

    [Test]
    [Combinatorial]
    public async Task GivenHandlerWithoutResponseWithInProcessReceiverConfiguration_WhenHandlerIsCalledMultipleTimes_ReceiverIsConfiguredCorrectly(
        [Values(arg1: true, arg2: false)] bool isDisabled,
        [Values(arg1: true, arg2: false)] bool configurePerMessage
    )
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var configureCallCount = 0;

        _ = services
            .AddMessageHandler<TestMessageWithoutResponseHandlerWithInProcessReceiverConfiguration>()
            .AddSingleton<Action<IInProcessMessageReceiver>>(r =>
            {
                configureCallCount += 1;

                if (configurePerMessage)
                {
                    _ = r.ConfigureOnEveryMessage();
                }

                if (isDisabled)
                {
                    r.Disable();
                }
            })
            .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>().For(TestMessageWithoutResponse.T);

        async Task Call()
        {
            try
            {
                await handler.Handle(CreateMessageWithoutResponse(), CancellationToken.None);
            }
            catch when (isDisabled)
            {
                // nothing to do
            }
        }

        await Call();
        await Call();
        await Call();

        Assert.That(configureCallCount, Is.EqualTo(configurePerMessage ? 3 : 1));
    }

    [Test]
    public async Task GivenHandlerWithDisabledInProcessTransport_WhenHandlerIsCalled_ExceptionIsThrown()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddMessageHandler<TestMessageHandlerWithInProcessReceiverConfiguration>()
            .AddSingleton<Action<IInProcessMessageReceiver>>(r => r.Disable())
            .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>().For(TestMessage.T);

        await Assert.ThatAsync(
            () => handler.Handle(CreateMessage(), CancellationToken.None),
            Throws.InvalidOperationException.With.Message.Contains("in-process transport is disabled for message type")
        );
    }

    [Test]
    public async Task GivenHandlerWithoutResponseWithDisabledInProcessTransport_WhenHandlerIsCalled_ExceptionIsThrown()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddMessageHandler<TestMessageWithoutResponseHandlerWithInProcessReceiverConfiguration>()
            .AddSingleton<Action<IInProcessMessageReceiver>>(r => r.Disable())
            .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>().For(TestMessageWithoutResponse.T);

        await Assert.ThatAsync(
            () => handler.Handle(CreateMessageWithoutResponse(), CancellationToken.None),
            Throws.InvalidOperationException.With.Message.Contains("in-process transport is disabled for message type")
        );
    }

    [Test]
    [Combinatorial]
    public async Task GivenSender_WhenConfiguringSender_TheLastConfigurationWins(
        [Values("sync", "async")] string firstConfigurationKind,
        [Values("sync", "async")] string secondConfigurationKind
    )
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddMessageHandler<TestMessageHandler>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageSenders>().For(TestMessage.T);

        handler = firstConfigurationKind switch
        {
            "sync" => handler.WithTransport(_ => new ThrowingMessageTransport<TestMessage, TestMessageResponse>(
                new NotSupportedException()
            )),
            "async" => handler.WithTransport(async _ =>
            {
                await Task.CompletedTask;

                return new ThrowingMessageTransport<TestMessage, TestMessageResponse>(new NotSupportedException());
            }),
            _ => throw new ArgumentOutOfRangeException(
                nameof(firstConfigurationKind),
                firstConfigurationKind,
                message: null
            ),
        };

        handler = secondConfigurationKind switch
        {
            "sync" => handler.WithTransport(b => b.UseInProcess()),
            "async" => handler.WithTransport(async b =>
            {
                await Task.CompletedTask;

                return b.UseInProcess();
            }),
            _ => throw new ArgumentOutOfRangeException(
                nameof(firstConfigurationKind),
                firstConfigurationKind,
                message: null
            ),
        };

        await Assert.ThatAsync(() => handler.Handle(CreateMessage(), CancellationToken.None), Throws.Nothing);
    }

    protected override IServiceCollection RegisterHandler(IServiceCollection services) =>
        services.AddMessageHandler<TestMessageHandler>();

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services) =>
        services.AddMessageHandler<TestMessageWithoutResponseHandler>();

    [Message<TestMessageResponse>]
    public sealed partial record TestMessage2(int Payload);

    private sealed partial class TestMessageHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exception = null
    ) : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(
            TestMessage message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            return new TestMessageResponse(message.Payload + 1);
        }
    }

    private sealed partial class TestMessageWithoutResponseHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exception = null
    ) : TestMessageWithoutResponse.IHandler
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

    private sealed partial class MultiTestMessageHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exception = null
    ) : TestMessage.IHandler, TestMessage2.IHandler
    {
        public async Task<TestMessageResponse> Handle(
            TestMessage message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            return new TestMessageResponse(message.Payload + 1);
        }

        public async Task<TestMessageResponse> Handle(
            TestMessage2 message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            return new TestMessageResponse(message.Payload + 1);
        }
    }

    private sealed partial class TestMessageHandlerWithInProcessReceiverConfiguration(TestObservations observations)
        : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(
            TestMessage message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);

            return new TestMessageResponse(message.Payload + 1);
        }

        static void IMessageHandler.ConfigureInProcessReceiver(IInProcessMessageReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IInProcessMessageReceiver>>().Invoke(receiver);
    }

    private sealed partial class TestMessageWithoutResponseHandlerWithInProcessReceiverConfiguration(
        TestObservations observations
    ) : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);
        }

        static void IMessageHandler.ConfigureInProcessReceiver(IInProcessMessageReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IInProcessMessageReceiver>>().Invoke(receiver);
    }

    private sealed partial class DisposableMessageHandler(DisposalObservation observation)
        : TestMessage.IHandler,
            IDisposable
    {
        public void Dispose() => observation.WasDisposed = true;

        public async Task<TestMessageResponse> Handle(
            TestMessage message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            return new TestMessageResponse(message.Payload);
        }
    }

    [Message<TestMessageResponse>]
    private partial record TestMessageBase(int PayloadBase);

    private sealed record TestMessageSub(int PayloadBase, int PayloadSub) : TestMessageBase(PayloadBase);

    private sealed partial class TestMessageBaseHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exception = null
    ) : TestMessageBase.IHandler
    {
        public async Task<TestMessageResponse> Handle(
            TestMessageBase message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            return new TestMessageResponse(message.PayloadBase + 1);
        }
    }

    private sealed class ThrowingMessageTransport<TMessage, TResponse>(Exception exception)
        : IMessageSender<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public string TransportTypeName => "throwing";

        public async Task<TResponse> Send(
            TMessage message,
            IServiceProvider serviceProvider,
            ConquerorContext conquerorContext,
            CancellationToken cancellationToken
        )
        {
            await Task.Yield();

            throw exception;
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
        return services.AddMessageHandlerDelegate(
            TestMessage.T,
            async (message, p, cancellationToken) =>
            {
                await Task.Yield();

                if (p.GetService<Exception>() is { } e)
                {
                    throw e;
                }

                var obs = p.GetRequiredService<TestObservations>();
                obs.Messages.Add(message);
                obs.CancellationTokens.Add(cancellationToken);
                obs.ServiceProviders.Add(p);

                return new TestMessageResponse(message.Payload + 1);
            }
        );
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return services.AddMessageHandlerDelegate(
            TestMessageWithoutResponse.T,
            async (message, p, cancellationToken) =>
            {
                await Task.Yield();

                if (p.GetService<Exception>() is { } e)
                {
                    throw e;
                }

                var obs = p.GetRequiredService<TestObservations>();
                obs.Messages.Add(message);
                obs.CancellationTokens.Add(cancellationToken);
                obs.ServiceProviders.Add(p);
            }
        );
    }
}

[TestFixture]
public sealed partial class MessageHandlerFunctionalityAssemblyScanningTests : MessageHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddMessageHandlersFromAssembly(
            typeof(MessageHandlerFunctionalityAssemblyScanningTests).Assembly
        );
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return services.AddMessageHandlersFromAssembly(
            typeof(MessageHandlerFunctionalityAssemblyScanningTests).Assembly
        );
    }

    // ReSharper disable once UnusedType.Global (accessed via reflection)
    public sealed partial class TestMessageForAssemblyScanningHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exception = null
    ) : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(
            TestMessage message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Messages.Add(message);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            return new TestMessageResponse(message.Payload + 1);
        }
    }

    // ReSharper disable once UnusedType.Global (accessed via reflection)
    public sealed partial class TestMessageWithoutResponseForAssemblyScanningHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exception = null
    ) : TestMessageWithoutResponse.IHandler
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

public abstract partial class MessageHandlerFunctionalityClientTests : MessageHandlerFunctionalityTests
{
    protected abstract bool BuildsSenderPerExecution { get; }

    [Test]
    public async Task GivenHandlerClient_WhenCallingClient_ServiceProviderInTransportBuilderIsFromResolutionScope()
    {
        var observations = new TestObservations();

        await using var provider = RegisterHandler(new ServiceCollection())
            .AddSingleton(observations)
            .BuildServiceProvider();

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();

        var handler1 = ResolveHandler(scope1.ServiceProvider);
        var handler2 = ResolveHandler(scope2.ServiceProvider);

        _ = await handler1.Handle(CreateMessage(), CancellationToken.None);
        _ = await handler1.Handle(CreateMessage(), CancellationToken.None);
        _ = await handler2.Handle(CreateMessage(), CancellationToken.None);

        if (BuildsSenderPerExecution)
        {
            Assert.That(observations.ServiceProvidersFromTransportFactory, Has.Count.EqualTo(expected: 3));
            Assert.That(
                observations.ServiceProvidersFromTransportFactory[0],
                Is.SameAs(observations.ServiceProvidersFromTransportFactory[1])
            );
            Assert.That(
                observations.ServiceProvidersFromTransportFactory[0],
                Is.Not.SameAs(observations.ServiceProvidersFromTransportFactory[2])
            );
        }
        else
        {
            Assert.That(observations.ServiceProvidersFromTransportFactory, Has.Count.EqualTo(expected: 2));
            Assert.That(
                observations.ServiceProvidersFromTransportFactory[0],
                Is.Not.SameAs(observations.ServiceProvidersFromTransportFactory[1])
            );
        }
    }

    [Test]
    public async Task GivenHandlerClientWithoutResponse_WhenCallingClient_ServiceProviderInTransportBuilderIsFromResolutionScope()
    {
        var observations = new TestObservations();

        await using var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
            .AddSingleton(observations)
            .BuildServiceProvider();

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();

        var handler1 = ResolveHandlerWithoutResponse(scope1.ServiceProvider);
        var handler2 = ResolveHandlerWithoutResponse(scope2.ServiceProvider);

        await handler1.Handle(CreateMessageWithoutResponse(), CancellationToken.None);
        await handler1.Handle(CreateMessageWithoutResponse(), CancellationToken.None);
        await handler2.Handle(CreateMessageWithoutResponse(), CancellationToken.None);

        if (BuildsSenderPerExecution)
        {
            Assert.That(observations.ServiceProvidersFromTransportFactory, Has.Count.EqualTo(expected: 3));
            Assert.That(
                observations.ServiceProvidersFromTransportFactory[0],
                Is.SameAs(observations.ServiceProvidersFromTransportFactory[1])
            );
            Assert.That(
                observations.ServiceProvidersFromTransportFactory[0],
                Is.Not.SameAs(observations.ServiceProvidersFromTransportFactory[2])
            );
        }
        else
        {
            Assert.That(observations.ServiceProvidersFromTransportFactory, Has.Count.EqualTo(expected: 2));
            Assert.That(
                observations.ServiceProvidersFromTransportFactory[0],
                Is.Not.SameAs(observations.ServiceProvidersFromTransportFactory[1])
            );
        }
    }

    [Test]
    public async Task GivenHandlerClientWithInProcessClientIfAvailable_WhenCallingClientWithInProcessAvailable_InProcessTransportIsUsed()
    {
        var observations = new TestObservations();
        var handlerWasCalled = false;

        await using var provider = RegisterHandler(new ServiceCollection())
            .AddMessageHandlerDelegate(
                TestMessage.T,
                (message, _) =>
                {
                    handlerWasCalled = true;

                    return new TestMessageResponse(message.Payload + 1);
                }
            )
            .AddSingleton(observations)
            .BuildServiceProvider();

        var handler = ConfigureWithTransport(ResolveHandler(provider), b => b.UseInProcessIfAvailable());

        _ = await handler.Handle(CreateMessage(), CancellationToken.None);

        Assert.That(observations.Messages, Has.Count.Zero);
        Assert.That(handlerWasCalled, Is.True);
    }

    [Test]
    public async Task GivenHandlerClientWithoutResponseWithInProcessClientIfAvailable_WhenCallingClientWithInProcessAvailable_InProcessTransportIsUsed()
    {
        var observations = new TestObservations();
        var handlerWasCalled = false;

        await using var provider = RegisterHandler(new ServiceCollection())
            .AddMessageHandlerDelegate(
                TestMessageWithoutResponse.T,
                (_, _) =>
                {
                    handlerWasCalled = true;
                }
            )
            .AddSingleton(observations)
            .BuildServiceProvider();

        var handler = ConfigureWithTransportWithoutResponse(
            ResolveHandlerWithoutResponse(provider),
            b => b.UseInProcessIfAvailable()
        );

        await handler.Handle(CreateMessageWithoutResponse(), CancellationToken.None);

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

        _ = await handler.Handle(CreateMessage(), CancellationToken.None);

        Assert.That(observations.Messages, Has.Count.EqualTo(expected: 1));
    }

    [Test]
    public async Task GivenHandlerClientWithoutResponseWithInProcessClientIfAvailable_WhenCallingClientWithInProcessNotAvailable_OtherTransportIsUsed()
    {
        var observations = new TestObservations();

        await using var provider = RegisterHandler(new ServiceCollection())
            .AddSingleton(observations)
            .BuildServiceProvider();

        var handler = ConfigureWithTransportWithoutResponse(
            ResolveHandlerWithoutResponse(provider),
            b => b.UseInProcessIfAvailable()
        );

        await handler.Handle(CreateMessageWithoutResponse(), CancellationToken.None);

        Assert.That(observations.Messages, Has.Count.EqualTo(expected: 1));
    }

    [Test]
    public async Task GivenHandlerClientWithInProcessClientIfAvailable_WhenCallingClientHandlerInProcessDisabled_OtherTransportIsUsed()
    {
        var observations = new TestObservations();

        await using var provider = RegisterHandler(new ServiceCollection())
            .AddMessageHandler<TestMessageHandlerWithInProcessReceiverConfiguration>()
            .AddSingleton<Action<IInProcessMessageReceiver>>(r => r.Disable())
            .AddSingleton(observations)
            .BuildServiceProvider();

        var handler = ConfigureWithTransport(ResolveHandler(provider), b => b.UseInProcessIfAvailable());

        _ = await handler.Handle(CreateMessage(), CancellationToken.None);

        Assert.That(observations.Messages, Has.Count.EqualTo(expected: 1));
    }

    [Test]
    public async Task GivenHandlerClientWithoutResponseWithInProcessClientIfAvailable_WhenCallingClientHandlerInProcessDisabled_OtherTransportIsUsed()
    {
        var observations = new TestObservations();

        await using var provider = RegisterHandler(new ServiceCollection())
            .AddMessageHandler<TestMessageWithoutResponseHandlerWithInProcessReceiverConfiguration>()
            .AddSingleton<Action<IInProcessMessageReceiver>>(r => r.Disable())
            .AddSingleton(observations)
            .BuildServiceProvider();

        var handler = ConfigureWithTransportWithoutResponse(
            ResolveHandlerWithoutResponse(provider),
            b => b.UseInProcessIfAvailable()
        );

        await handler.Handle(CreateMessageWithoutResponse(), CancellationToken.None);

        Assert.That(observations.Messages, Has.Count.EqualTo(expected: 1));
    }

    protected abstract TestMessage.IHandler ConfigureWithTransport(
        TestMessage.IHandler handler,
        Func<
            MessageSenderBuilder<TestMessage, TestMessageResponse>,
            IMessageSender<TestMessage, TestMessageResponse>?
        >? baseConfigure = null
    );

    protected abstract TestMessageWithoutResponse.IHandler ConfigureWithTransportWithoutResponse(
        TestMessageWithoutResponse.IHandler handler,
        Func<
            MessageSenderBuilder<TestMessageWithoutResponse, UnitMessageResponse>,
            IMessageSender<TestMessageWithoutResponse, UnitMessageResponse>?
        >? baseConfigure = null
    );

    protected sealed override IServiceCollection RegisterHandler(IServiceCollection services) =>
        services.AddConqueror().AddSingleton(typeof(TestMessageTransport<,>));

    protected sealed override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services) =>
        services.AddConqueror().AddSingleton(typeof(TestMessageTransport<,>));

    protected sealed override TestMessage.IHandler ResolveHandler(IServiceProvider serviceProvider) =>
        ConfigureWithTransport(base.ResolveHandler(serviceProvider));

    protected sealed override TestMessageWithoutResponse.IHandler ResolveHandlerWithoutResponse(
        IServiceProvider serviceProvider
    ) => ConfigureWithTransportWithoutResponse(base.ResolveHandlerWithoutResponse(serviceProvider));

    protected sealed class TestMessageTransport<TMessage, TResponse>(Exception? exception = null)
        : IMessageSender<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public string TransportTypeName => "test";

        public async Task<TResponse> Send(
            TMessage message,
            IServiceProvider serviceProvider,
            ConquerorContext conquerorContext,
            CancellationToken cancellationToken
        )
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

    private sealed partial class TestMessageHandlerWithInProcessReceiverConfiguration : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(
            TestMessage message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            return new TestMessageResponse(message.Payload + 1);
        }

        static void IMessageHandler.ConfigureInProcessReceiver(IInProcessMessageReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IInProcessMessageReceiver>>().Invoke(receiver);
    }

    private sealed partial class TestMessageWithoutResponseHandlerWithInProcessReceiverConfiguration
        : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default) =>
            await Task.Yield();

        static void IMessageHandler.ConfigureInProcessReceiver(IInProcessMessageReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IInProcessMessageReceiver>>().Invoke(receiver);
    }
}

[TestFixture]
public sealed class MessageHandlerFunctionalityClientWithSyncTransportFactoryTests
    : MessageHandlerFunctionalityClientTests
{
    protected override bool BuildsSenderPerExecution => false;

    protected override TestMessage.IHandler ConfigureWithTransport(
        TestMessage.IHandler handler,
        Func<
            MessageSenderBuilder<TestMessage, TestMessageResponse>,
            IMessageSender<TestMessage, TestMessageResponse>?
        >? baseConfigure = null
    )
    {
        return handler.WithTransport(b =>
        {
            b.ServiceProvider.GetRequiredService<TestObservations>()
                .ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);

            return baseConfigure?.Invoke(b)
                ?? b.ServiceProvider.GetRequiredService<TestMessageTransport<TestMessage, TestMessageResponse>>();
        });
    }

    protected override TestMessageWithoutResponse.IHandler ConfigureWithTransportWithoutResponse(
        TestMessageWithoutResponse.IHandler handler,
        Func<
            MessageSenderBuilder<TestMessageWithoutResponse, UnitMessageResponse>,
            IMessageSender<TestMessageWithoutResponse, UnitMessageResponse>?
        >? baseConfigure = null
    )
    {
        return handler.WithTransport(b =>
        {
            b.ServiceProvider.GetRequiredService<TestObservations>()
                .ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);

            return baseConfigure?.Invoke(b)
                ?? b.ServiceProvider.GetRequiredService<
                    TestMessageTransport<TestMessageWithoutResponse, UnitMessageResponse>
                >();
        });
    }
}

[TestFixture]
public sealed class MessageHandlerFunctionalityClientWithAsyncTransportFactoryTests
    : MessageHandlerFunctionalityClientTests
{
    protected override bool BuildsSenderPerExecution => true;

    protected override TestMessage.IHandler ConfigureWithTransport(
        TestMessage.IHandler handler,
        Func<
            MessageSenderBuilder<TestMessage, TestMessageResponse>,
            IMessageSender<TestMessage, TestMessageResponse>?
        >? baseConfigure = null
    )
    {
        return handler.WithTransport(async b =>
        {
            await Task.Delay(millisecondsDelay: 1);
            b.ServiceProvider.GetRequiredService<TestObservations>()
                .ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);

            return baseConfigure?.Invoke(b)
                ?? b.ServiceProvider.GetRequiredService<TestMessageTransport<TestMessage, TestMessageResponse>>();
        });
    }

    protected override TestMessageWithoutResponse.IHandler ConfigureWithTransportWithoutResponse(
        TestMessageWithoutResponse.IHandler handler,
        Func<
            MessageSenderBuilder<TestMessageWithoutResponse, UnitMessageResponse>,
            IMessageSender<TestMessageWithoutResponse, UnitMessageResponse>?
        >? baseConfigure = null
    )
    {
        return handler.WithTransport(async b =>
        {
            await Task.Delay(millisecondsDelay: 1);
            b.ServiceProvider.GetRequiredService<TestObservations>()
                .ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);

            return baseConfigure?.Invoke(b)
                ?? b.ServiceProvider.GetRequiredService<
                    TestMessageTransport<TestMessageWithoutResponse, UnitMessageResponse>
                >();
        });
    }
}
