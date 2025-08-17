namespace Conqueror.Tests.Signalling;

public abstract partial class SignalHandlerFunctionalityTests
{
    [Test]
    public async Task GivenHandlerForSingleSignalType_WhenCalledWithSignal_HandlerReceivesSignal()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var signal = CreateSignal();

        await handler.Handle(signal, CancellationToken.None);

        Assert.That(observations.Signals, Is.EqualTo([signal]));
    }

    [Test]
    public async Task GivenHandlerForSingleSignalType_WhenCalledWithSignalOfSubType_HandlerReceivesSignal()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var signal = CreateSubSignal();

        await handler.Handle(signal, CancellationToken.None);

        Assert.That(observations.Signals, Is.EqualTo([signal]));
    }

    [Test]
    public async Task GivenMultipleHandlersForSingleSignalType_WhenCalledWithSignal_AllHandlersReceiveSignal()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler2(RegisterHandler(new ServiceCollection()))
            .AddSingleton(observations)
            .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var signal = CreateSignal();

        await handler.Handle(signal, CancellationToken.None);

        Assert.That(observations.Signals, Is.EqualTo([signal, signal]));
    }

    [Test]
    public async Task GivenHandler_WhenCalledWithCancellationToken_HandlerReceivesCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var signal = CreateSignal();

        using var cts = new CancellationTokenSource();

        await handler.Handle(signal, cts.Token);

        Assert.That(observations.CancellationTokens, Is.EqualTo([cts.Token]));
    }

    [Test]
    public async Task GivenHandler_WhenCalledWithoutCancellationToken_HandlerReceivesDefaultCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var signal = CreateSignal();

        await handler.Handle(signal, CancellationToken.None);

        Assert.That(observations.CancellationTokens, Is.EqualTo([CancellationToken.None]));
    }

    [Test]
    public void GivenHandler_WhenCallingThrowsException_InvocationThrowsSameException()
    {
        var exception = new Exception();

        var provider = RegisterHandler(new ServiceCollection())
            .AddSingleton(new TestObservations())
            .AddSingleton(exception)
            .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        Assert.That(() => handler.Handle(CreateSignal(), CancellationToken.None), Throws.Exception.SameAs(exception));
    }

    [Test]
    public async Task GivenHandler_WhenResolvingHandler_HandlerIsResolvedFromResolutionScope()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();

        var handler1 = ResolveHandler(scope1.ServiceProvider);
        var handler2 = ResolveHandler(scope2.ServiceProvider);

        var signal = CreateSignal();

        await handler1.Handle(signal, CancellationToken.None);
        await handler1.Handle(signal, CancellationToken.None);
        await handler2.Handle(signal, CancellationToken.None);

        Assert.That(observations.ServiceProviders, Has.Count.EqualTo(expected: 3));
        Assert.That(
            observations.ServiceProviders[0],
            Is.SameAs(observations.ServiceProviders[1]).And.Not.SameAs(observations.ServiceProviders[2])
        );
    }

    protected abstract IServiceCollection RegisterHandler(IServiceCollection services);

    protected abstract IServiceCollection RegisterHandler2(IServiceCollection services);

    protected virtual TestSignal.IHandler ResolveHandler(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<ISignalPublishers>().For(TestSignal.T);

    protected TestSignal CreateSignal() => new(Payload: 10);

    protected TestSignal CreateSubSignal() => new(Payload: 10);

    [Signal]
    public partial record TestSignal(int Payload);

    public sealed class TestObservations
    {
        public List<object> Signals { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public List<IServiceProvider> ServiceProviders { get; } = [];

        public List<IServiceProvider> ServiceProvidersFromTransportFactory { get; } = [];

        public int ConfigurationCount { get; set; }
    }
}

[TestFixture]
public sealed partial class SignalHandlerFunctionalityDefaultTests : SignalHandlerFunctionalityTests
{
    [Test]
    public async Task GivenSignalTypeWithoutRegisteredHandler_WhenCallingHandler_LeadsToNoop()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorSignalling().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var signal = CreateSignal();

        await handler.Handle(signal, CancellationToken.None);

        Assert.That(observations.Signals, Is.Empty);
    }

    [Test]
    public async Task GivenSignalTypeWithHandlerWithDisabledInProcessTransport_WhenCallingHandler_LeadsToNoop()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandlerWithDisabledInProcessTransport>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var signal = CreateSignal();

        await handler.Handle(signal, CancellationToken.None);

        Assert.That(observations.Signals, Is.Empty);
    }

    [Test]
    public async Task GivenHandlerForMultipleSignalTypes_WhenCalledWithSignalOfEitherType_HandlerReceivesSignal()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<MultiTestSignalHandler>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler1 = provider.GetRequiredService<ISignalPublishers>().For(TestSignal.T);
        var handler2 = provider.GetRequiredService<ISignalPublishers>().For(TestSignal2.T);

        var signal1 = new TestSignal(Payload: 10);
        var signal2 = new TestSignal2(Payload: 20);

        await handler1.Handle(signal1, CancellationToken.None);
        await handler2.Handle(signal2, CancellationToken.None);

        Assert.That(observations.Signals, Is.EqualTo(new object[] { signal1, signal2 }));
    }

    [Test]
    public async Task GivenHandlerForMultipleSignalTypes_WhenCalledWithSignalOfSubTypeOfEitherType_HandlerReceivesSignal()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<MultiTestSignalHandler>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler1 = provider.GetRequiredService<ISignalPublishers>().For(TestSignal.T);
        var handler2 = provider.GetRequiredService<ISignalPublishers>().For(TestSignal2.T);

        var signal1 = new TestSignalSub(Payload: 10);
        var signal2 = new TestSignal2Sub(Payload: 20);

        await handler1.Handle(signal1, CancellationToken.None);
        await handler2.Handle(signal2, CancellationToken.None);

        Assert.That(observations.Signals, Is.EqualTo(new object[] { signal1, signal2 }));
    }

    [Test]
    public async Task GivenHandlerForMultipleSignalTypesFromTheSameHierarchy_WhenCalledWithSignalOfSubType_HandlerReceivesSignalMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<MultiHierarchyTestSignalHandler>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>().For(TestSignal.T);

        var signal = new TestSignalSub(Payload: 10);

        await handler.Handle(signal, CancellationToken.None);

        Assert.That(observations.Signals, Is.EqualTo(new object[] { signal, signal }));
    }

    [Test]
    public async Task GivenHandlerForMultipleSignalTypesFromTheSameHierarchy_WhenCalledWithSignalOfSubSubType_HandlerReceivesSignalMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<MultiHierarchyTestSignalHandler>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>().For(TestSignal.T);

        var signal = new TestSignalSubSub(Payload: 10);

        await handler.Handle(signal, CancellationToken.None);

        Assert.That(observations.Signals, Is.EqualTo(new object[] { signal, signal }));
    }

    [Test]
    public async Task GivenHandlerWithInProcessReceiverConfiguration_WhenHandlerIsCalledMultipleTimes_TheInProcessReceiverIsOnlyConfiguredOnce()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandlerWithInProcessReceiverConfiguration>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>().For(TestSignal.T);

        await handler.Handle(new(Payload: 10), CancellationToken.None);
        await handler.Handle(new(Payload: 10), CancellationToken.None);

        Assert.That(observations.ConfigurationCount, Is.EqualTo(expected: 1));
    }

    [Test]
    public async Task GivenDisposableHandler_WhenServiceProviderIsDisposed_ThenHandlerIsDisposed()
    {
        var services = new ServiceCollection();
        var observation = new DisposalObservation();

        _ = services
            .AddSignalHandler<DisposableSignalHandler>()
            .AddSingleton(observation)
            .AddSingleton(new TestObservations());

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>().For(TestSignal.T);

        await handler.Handle(new(Payload: 10), CancellationToken.None);

        await provider.DisposeAsync();

        Assert.That(observation.WasDisposed, Is.True);
    }

    [Test]
    [Combinatorial]
    public async Task GivenPublisher_WhenConfiguringPublisher_TheLastConfigurationWins(
        [Values("sync", "async")] string firstConfigurationKind,
        [Values("sync", "async")] string secondConfigurationKind
    )
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>().For(TestSignal.T);

        handler = firstConfigurationKind switch
        {
            "sync" => handler.WithTransport(_ => new ThrowingSignalPublisher<TestSignal>(new NotSupportedException())),
            "async" => handler.WithTransport(async _ =>
            {
                await Task.CompletedTask;

                return new ThrowingSignalPublisher<TestSignal>(new NotSupportedException());
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

        await Assert.ThatAsync(() => handler.Handle(CreateSignal(), CancellationToken.None), Throws.Nothing);
    }

    protected override IServiceCollection RegisterHandler(IServiceCollection services) =>
        services.AddSignalHandler<TestSignalHandler>();

    protected override IServiceCollection RegisterHandler2(IServiceCollection services) =>
        services.AddSignalHandler<TestSignalHandler2>();

    [Signal]
    public partial record TestSignalSub(int Payload) : TestSignal(Payload);

    public sealed record TestSignalSubSub(int Payload) : TestSignalSub(Payload);

    [Signal]
    public partial record TestSignal2(int Payload);

    public sealed record TestSignal2Sub(int Payload) : TestSignal2(Payload);

    private sealed partial class TestSignalHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exceptionToThrow = null
    ) : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }

            observations.Signals.Add(signal);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }

    private sealed partial class TestSignalHandler2(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exceptionToThrow = null
    ) : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }

            observations.Signals.Add(signal);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }

    private sealed partial class MultiTestSignalHandler(TestObservations observations, IServiceProvider serviceProvider)
        : TestSignal.IHandler,
            TestSignal2.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Signals.Add(signal);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Signals.Add(signal);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }

    private sealed partial class MultiHierarchyTestSignalHandler(
        TestObservations observations,
        IServiceProvider serviceProvider
    ) : TestSignal.IHandler, TestSignalSub.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Signals.Add(signal);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }

        public async Task Handle(TestSignalSub signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Signals.Add(signal);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }

    private sealed partial class TestSignalHandlerWithDisabledInProcessTransport(TestObservations observations)
        : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Signals.Add(signal);
            observations.CancellationTokens.Add(cancellationToken);
        }

        static void ISignalHandler.ConfigureInProcessReceiver(IInProcessSignalReceiver receiver) => receiver.Disable();
    }

    private sealed partial class TestSignalHandlerWithInProcessReceiverConfiguration(TestObservations observations)
        : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Signals.Add(signal);
            observations.CancellationTokens.Add(cancellationToken);
        }

        static void ISignalHandler.ConfigureInProcessReceiver(IInProcessSignalReceiver receiver)
        {
            var observations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            observations.ConfigurationCount += 1;
        }
    }

    private sealed partial class DisposableSignalHandler(DisposalObservation observation)
        : TestSignal.IHandler,
            IDisposable
    {
        public void Dispose() => observation.WasDisposed = true;

        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default) =>
            await Task.Yield();
    }

    private sealed class ThrowingSignalPublisher<TSignal>(Exception exception) : ISignalPublisher<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public string TransportTypeName => "throwing";

        public async Task Publish(
            TSignal signal,
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
public sealed class SignalHandlerFunctionalityDelegateTests : SignalHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddSignalHandlerDelegate(
            TestSignal.T,
            async (signal, p, cancellationToken) =>
            {
                await Task.Yield();

                if (p.GetService<Exception>() is { } e)
                {
                    throw e;
                }

                var obs = p.GetRequiredService<TestObservations>();
                obs.Signals.Add(signal);
                obs.CancellationTokens.Add(cancellationToken);
                obs.ServiceProviders.Add(p);
            }
        );
    }

    protected override IServiceCollection RegisterHandler2(IServiceCollection services)
    {
        return services.AddSignalHandlerDelegate(
            TestSignal.T,
            async (signal, p, cancellationToken) =>
            {
                await Task.Yield();

                if (p.GetService<Exception>() is { } e)
                {
                    throw e;
                }

                var obs = p.GetRequiredService<TestObservations>();
                obs.Signals.Add(signal);
                obs.CancellationTokens.Add(cancellationToken);
                obs.ServiceProviders.Add(p);
            }
        );
    }
}

[TestFixture]
public sealed partial class SignalHandlerFunctionalityAssemblyScanningTests : SignalHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services) =>
        services.AddSignalHandlersFromAssembly(typeof(SignalHandlerFunctionalityAssemblyScanningTests).Assembly);

    protected override IServiceCollection RegisterHandler2(IServiceCollection services)
    {
        return services
            .AddSignalHandlersFromAssembly(typeof(SignalHandlerFunctionalityAssemblyScanningTests).Assembly)
            .AddSignalHandler<TestSignalHandler>();
    }

    [Signal]
    public sealed partial record TestSignal2(int Payload);

    // ReSharper disable once UnusedType.Global (accessed via reflection)
    public sealed partial class TestSignalForAssemblyScanningHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exception = null
    ) : TestSignal.IHandler, TestSignal2.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Signals.Add(signal);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }

        public Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed partial class TestSignalHandler(TestObservations observations) : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Signals.Add(signal);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }
}

public abstract class SignalHandlerFunctionalityPublisherTests : SignalHandlerFunctionalityTests
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

        await handler1.Handle(CreateSignal(), CancellationToken.None);
        await handler1.Handle(CreateSignal(), CancellationToken.None);
        await handler2.Handle(CreateSignal(), CancellationToken.None);

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

    protected abstract TestSignal.IHandler ConfigureWithPublisher(
        TestSignal.IHandler builder,
        Func<SignalPublisherBuilder<TestSignal>, ISignalPublisher<TestSignal>?>? baseConfigure = null
    );

    protected sealed override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        var existingOptions = services
            .Select(d => d.ImplementationInstance)
            .OfType<TestSignalPublisherOptions>()
            .FirstOrDefault();
        _ = services.Replace(
            ServiceDescriptor.Singleton(new TestSignalPublisherOptions(existingOptions?.HandlerCount + 1 ?? 1))
        );
        services.TryAddSingleton(typeof(TestSignalPublisher<>));

        return services.AddConqueror();
    }

    protected sealed override IServiceCollection RegisterHandler2(IServiceCollection services) =>
        RegisterHandler(services);

    protected sealed override TestSignal.IHandler ResolveHandler(IServiceProvider serviceProvider) =>
        ConfigureWithPublisher(base.ResolveHandler(serviceProvider));

    protected sealed class TestSignalPublisher<TSignal>(TestSignalPublisherOptions options, Exception? exception = null)
        : ISignalPublisher<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public string TransportTypeName => "test";

        public async Task Publish(
            TSignal signal,
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

            for (var i = 0; i < options.HandlerCount; i += 1)
            {
                observations.Signals.Add(signal);
                observations.CancellationTokens.Add(cancellationToken);
                observations.ServiceProviders.Add(serviceProvider);
            }
        }
    }

    protected sealed record TestSignalPublisherOptions(int HandlerCount);
}

[TestFixture]
public sealed class SignalHandlerFunctionalityPublisherWithSyncTransportFactoryTests
    : SignalHandlerFunctionalityPublisherTests
{
    protected override bool BuildsSenderPerExecution => false;

    protected override TestSignal.IHandler ConfigureWithPublisher(
        TestSignal.IHandler builder,
        Func<SignalPublisherBuilder<TestSignal>, ISignalPublisher<TestSignal>?>? baseConfigure = null
    )
    {
        return builder.WithTransport(b =>
        {
            b.ServiceProvider.GetRequiredService<TestObservations>()
                .ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);

            return baseConfigure?.Invoke(b) ?? b.ServiceProvider.GetRequiredService<TestSignalPublisher<TestSignal>>();
        });
    }
}

[TestFixture]
public sealed class SignalHandlerFunctionalityPublisherWithAsyncTransportFactoryTests
    : SignalHandlerFunctionalityPublisherTests
{
    protected override bool BuildsSenderPerExecution => true;

    protected override TestSignal.IHandler ConfigureWithPublisher(
        TestSignal.IHandler builder,
        Func<SignalPublisherBuilder<TestSignal>, ISignalPublisher<TestSignal>?>? baseConfigure = null
    )
    {
        return builder.WithTransport(async b =>
        {
            await Task.Delay(millisecondsDelay: 1);
            b.ServiceProvider.GetRequiredService<TestObservations>()
                .ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);

            return baseConfigure?.Invoke(b) ?? b.ServiceProvider.GetRequiredService<TestSignalPublisher<TestSignal>>();
        });
    }
}
