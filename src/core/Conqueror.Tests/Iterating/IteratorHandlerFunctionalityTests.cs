namespace Conqueror.Tests.Iterating;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[SuppressMessage(
    "Style",
    "MA0040:Use an overload with a CancellationToken",
    Justification = "Tests are intentionally validating token flow"
)]
public abstract partial class IteratorHandlerFunctionalityTests
{
    [Test]
    public async Task GivenIterator_WhenHandlerIsCalled_HandlerReceivesIterator()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var iterator = CreateIterator();

        _ = await ConsumeAll(handler.Handle(iterator, CancellationToken.None));

        Assert.That(observations.Iterators, Is.EqualTo([iterator]));
    }

    [Test]
    public async Task GivenCancellationToken_WhenHandlerIsCalled_HandlerReceivesCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        var handler = ResolveHandler(provider);
        using var tokenSource = new CancellationTokenSource();

        _ = await ConsumeAll(handler.Handle(CreateIterator(), tokenSource.Token));

        Assert.That(observations.CancellationTokens, Is.EqualTo([tokenSource.Token]));
    }

    [Test]
    public async Task GivenNoCancellationToken_WhenHandlerIsCalled_HandlerReceivesDefaultCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        var handler = ResolveHandler(provider);

        _ = await ConsumeAll(handler.Handle(CreateIterator(), CancellationToken.None));

        Assert.That(observations.CancellationTokens, Is.EqualTo([CancellationToken.None]));
    }

    [Test]
    public async Task GivenIterator_WhenHandlerIsCalled_HandlerReturnsItems()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var iterator = CreateIterator();

        var items = await ConsumeAll(handler.Handle(iterator, CancellationToken.None));

        Assert.That(items, Is.EqualTo(CreateExpectedItems()));
    }

    [Test]
    public void GivenExceptionInHandler_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var observations = new TestObservations();
        var exception = new Exception();

        var provider = RegisterHandler(new ServiceCollection())
            .AddSingleton(observations)
            .AddSingleton(exception)
            .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        using var cts = new CancellationTokenSource();
        var thrownException = Assert.ThrowsAsync<Exception>(async () =>
            await ConsumeAll(handler.Handle(CreateIterator(), cts.Token))
        );

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenHandler_WhenResolved_HandlerIsResolvedFromResolutionScope()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection()).AddSingleton(observations).BuildServiceProvider();

        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();

        var handler1 = ResolveHandler(scope1.ServiceProvider);
        var handler2 = ResolveHandler(scope2.ServiceProvider);

        _ = await ConsumeAll(handler1.Handle(CreateIterator(), CancellationToken.None));
        _ = await ConsumeAll(handler1.Handle(CreateIterator(), CancellationToken.None));
        _ = await ConsumeAll(handler2.Handle(CreateIterator(), CancellationToken.None));

        Assert.That(observations.ServiceProviders, Has.Count.EqualTo(expected: 3));
        Assert.That(observations.ServiceProviders[0], Is.SameAs(observations.ServiceProviders[1]));
        Assert.That(observations.ServiceProviders[0], Is.Not.SameAs(observations.ServiceProviders[2]));
    }

    protected abstract IServiceCollection RegisterHandler(IServiceCollection services);

    protected virtual TestIterator.IHandler ResolveHandler(IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<IIterators>().For(TestIterator.T);

    protected static TestIterator CreateIterator() => new(Payload: 10);

    protected static List<int> CreateExpectedItems() => [11, 12, 13];

    protected static async Task<List<T>> ConsumeAll<T>(
        IAsyncEnumerable<T> enumerable,
        CancellationToken cancellationToken = default
    )
    {
        var list = new List<T>();
        await foreach (var item in enumerable.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }

        return list;
    }

    [Iterator<int>]
    public sealed partial record TestIterator(int Payload);

    public sealed class TestObservations
    {
        public List<object> Iterators { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public List<IServiceProvider> ServiceProviders { get; } = [];

        public List<IServiceProvider> ServiceProvidersFromTransportFactory { get; } = [];
    }
}

[TestFixture]
[SuppressMessage(
    "Style",
    "MA0040:Use an overload with a CancellationToken",
    Justification = "Tests are intentionally validating token flow"
)]
public sealed partial class IteratorHandlerFunctionalityDefaultTests : IteratorHandlerFunctionalityTests
{
    [Test]
    public async Task GivenDisposableHandler_WhenServiceProviderIsDisposed_ThenHandlerIsDisposed()
    {
        var services = new ServiceCollection();
        var observation = new DisposalObservation();

        _ = services.AddIteratorHandler<DisposableIteratorHandler>().AddSingleton(observation);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        using var cts = new CancellationTokenSource();
        _ = await ConsumeAll(handler.Handle(CreateIterator(), cts.Token));

        await provider.DisposeAsync();

        Assert.That(observation.WasDisposed, Is.True);
    }

    [Test]
    public async Task GivenHandlerForMultipleIteratorTypes_WhenCalledWithIteratorOfEitherType_HandlerReceivesIterator()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddIteratorHandler<MultiTestIteratorHandler>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler1 = provider.GetRequiredService<IIterators>().For(TestIterator.T);
        var handler2 = provider.GetRequiredService<IIterators>().For(TestIterator2.T);

        var iterator1 = new TestIterator(Payload: 10);
        var iterator2 = new TestIterator2(Payload: 20);

        _ = await ConsumeAll(handler1.Handle(iterator1, CancellationToken.None));
        _ = await ConsumeAll(handler2.Handle(iterator2, CancellationToken.None));

        Assert.That(observations.Iterators, Is.EqualTo(new object[] { iterator1, iterator2 }));
    }

    [Test]
    public async Task GivenHandlerForBaseIteratorType_WhenHandlerIsCalledWithIteratorSubType_HandlerIsCalledCorrectly()
    {
        var observations = new TestObservations();

        var provider = new ServiceCollection()
            .AddIteratorHandler<TestIteratorBaseHandler>()
            .AddSingleton(observations)
            .BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIteratorBase.T);

        var iterator = new TestIteratorSub(PayloadBase: 10, PayloadSub: -1);

        var items = await ConsumeAll(handler.Handle(iterator, CancellationToken.None));

        Assert.That(observations.Iterators, Is.EqualTo([iterator]));
        Assert.That(items, Is.EqualTo([11, 12, 13]));
    }

    [Test]
    [Combinatorial]
    public async Task GivenHandlerWithInProcessServerConfiguration_WhenHandlerIsCalledMultipleTimes_ServerIsConfiguredCorrectly(
        [Values(arg1: true, arg2: false)] bool isDisabled,
        [Values(arg1: true, arg2: false)] bool configurePerIterator
    )
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var configureCallCount = 0;

        _ = services
            .AddIteratorHandler<TestIteratorHandlerWithInProcessServerConfiguration>()
            .AddSingleton<Action<IInProcessIteratorServer>>(r =>
            {
                configureCallCount += 1;

                if (configurePerIterator)
                {
                    _ = r.ConfigureOnEveryIteration();
                }

                if (isDisabled)
                {
                    r.Disable();
                }
            })
            .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        async Task Call()
        {
            try
            {
                _ = await ConsumeAll(handler.Handle(CreateIterator(), CancellationToken.None));
            }
            catch when (isDisabled)
            {
                // nothing to do
            }
        }

        await Call();
        await Call();
        await Call();

        Assert.That(configureCallCount, Is.EqualTo(configurePerIterator ? 3 : 1));
    }

    [Test]
    public async Task GivenHandlerWithDisabledInProcessTransport_WhenHandlerIsCalled_ExceptionIsThrown()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddIteratorHandler<TestIteratorHandlerWithInProcessServerConfiguration>()
            .AddSingleton<Action<IInProcessIteratorServer>>(r => r.Disable())
            .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        using var cts = new CancellationTokenSource();
        await Assert.ThatAsync(
            () => ConsumeAll(handler.Handle(CreateIterator(), cts.Token)),
            Throws.InvalidOperationException.With.Message.Contains("in-process transport is disabled for iterator type")
        );
    }

    [Test]
    [Combinatorial]
    public async Task GivenClient_WhenConfiguringClient_TheLastConfigurationWins(
        [Values("sync", "async")] string firstConfigurationKind,
        [Values("sync", "async")] string secondConfigurationKind
    )
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddIteratorHandler<TestIteratorHandler>().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IIterators>().For(TestIterator.T);

        handler = firstConfigurationKind switch
        {
            "sync" => handler.WithTransport(_ => new ThrowingIteratorTransport<TestIterator, int>(
                new NotSupportedException()
            )),
            "async" => handler.WithTransport(_ =>
            {
                return ValueTask.FromResult<IIteratorClient<TestIterator, int>>(
                    new ThrowingIteratorTransport<TestIterator, int>(new NotSupportedException())
                );
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
            "async" => handler.WithTransport(b =>
            {
                return ValueTask.FromResult(b.UseInProcess());
            }),
            _ => throw new ArgumentOutOfRangeException(
                nameof(firstConfigurationKind),
                firstConfigurationKind,
                message: null
            ),
        };

        using var cts = new CancellationTokenSource();
        await Assert.ThatAsync(() => ConsumeAll(handler.Handle(CreateIterator(), cts.Token)), Throws.Nothing);
    }

    protected override IServiceCollection RegisterHandler(IServiceCollection services) =>
        services.AddIteratorHandler<TestIteratorHandler>();

    [Iterator<int>]
    public sealed partial record TestIterator2(int Payload);

    private sealed partial class TestIteratorHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exception = null
    ) : TestIterator.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Iterators.Add(iterator);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            yield return iterator.Payload + 1;
            yield return iterator.Payload + 2;
            yield return iterator.Payload + 3;
        }
    }

    private sealed partial class MultiTestIteratorHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exception = null
    ) : TestIterator.IHandler, TestIterator2.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Iterators.Add(iterator);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            yield return iterator.Payload + 1;
            yield return iterator.Payload + 2;
            yield return iterator.Payload + 3;
        }

        public async IAsyncEnumerable<int> Handle(
            TestIterator2 iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Iterators.Add(iterator);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            yield return iterator.Payload + 1;
            yield return iterator.Payload + 2;
            yield return iterator.Payload + 3;
        }
    }

    private sealed partial class TestIteratorHandlerWithInProcessServerConfiguration(TestObservations observations)
        : TestIterator.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            observations.Iterators.Add(iterator);
            observations.CancellationTokens.Add(cancellationToken);

            yield return iterator.Payload + 1;
            yield return iterator.Payload + 2;
            yield return iterator.Payload + 3;
        }

        static void IIteratorHandler.ConfigureInProcessServer(IInProcessIteratorServer server) =>
            server.ServiceProvider.GetRequiredService<Action<IInProcessIteratorServer>>().Invoke(server);
    }

    private sealed partial class DisposableIteratorHandler(DisposalObservation observation)
        : TestIterator.IHandler,
            IDisposable
    {
        public void Dispose() => observation.WasDisposed = true;

        public async IAsyncEnumerable<int> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            yield return iterator.Payload;
        }
    }

    [Iterator<int>]
    private partial record TestIteratorBase(int PayloadBase);

    private sealed record TestIteratorSub(int PayloadBase, int PayloadSub) : TestIteratorBase(PayloadBase);

    private sealed partial class TestIteratorBaseHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exception = null
    ) : TestIteratorBase.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIteratorBase iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Iterators.Add(iterator);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            yield return iterator.PayloadBase + 1;
            yield return iterator.PayloadBase + 2;
            yield return iterator.PayloadBase + 3;
        }
    }

    private sealed class ThrowingIteratorTransport<TIterator, TItem>(Exception exception)
        : IIteratorClient<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public string TransportTypeName => "throwing";

        public async IAsyncEnumerable<TItem> Execute(
            TIterator iterator,
            IServiceProvider serviceProvider,
            ConquerorContext conquerorContext,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.Yield();

            throw exception;

#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }
    }

    private sealed class DisposalObservation
    {
        public bool WasDisposed { get; set; }
    }
}

[TestFixture]
[SuppressMessage(
    "Style",
    "MA0040:Use an overload with a CancellationToken",
    Justification = "Tests are intentionally validating token flow"
)]
public sealed class IteratorHandlerFunctionalityDelegateTests : IteratorHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddIteratorHandlerDelegate(TestIterator.T, HandleIterator);

        static async IAsyncEnumerable<int> HandleIterator(
            TestIterator iterator,
            IServiceProvider p,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.Yield();

            if (p.GetService<Exception>() is { } e)
            {
                throw e;
            }

            var obs = p.GetRequiredService<TestObservations>();
            obs.Iterators.Add(iterator);
            obs.CancellationTokens.Add(cancellationToken);
            obs.ServiceProviders.Add(p);

            yield return iterator.Payload + 1;
            yield return iterator.Payload + 2;
            yield return iterator.Payload + 3;
        }
    }
}

[TestFixture]
[SuppressMessage(
    "Style",
    "MA0040:Use an overload with a CancellationToken",
    Justification = "Tests are intentionally validating token flow"
)]
public sealed partial class IteratorHandlerFunctionalityAssemblyScanningTests : IteratorHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddIteratorHandlersFromAssembly(
            typeof(IteratorHandlerFunctionalityAssemblyScanningTests).Assembly
        );
    }

    public sealed partial class TestIteratorForAssemblyScanningHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exception = null
    ) : TestIterator.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Iterators.Add(iterator);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            yield return iterator.Payload + 1;
            yield return iterator.Payload + 2;
            yield return iterator.Payload + 3;
        }
    }
}

[SuppressMessage(
    "Style",
    "MA0040:Use an overload with a CancellationToken",
    Justification = "Tests are intentionally validating token flow"
)]
public abstract partial class IteratorHandlerFunctionalityClientTests : IteratorHandlerFunctionalityTests
{
    protected abstract bool BuildsClientPerExecution { get; }

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

        _ = await ConsumeAll(handler1.Handle(CreateIterator(), CancellationToken.None));
        _ = await ConsumeAll(handler1.Handle(CreateIterator(), CancellationToken.None));
        _ = await ConsumeAll(handler2.Handle(CreateIterator(), CancellationToken.None));

        if (BuildsClientPerExecution)
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
            .AddIteratorHandlerDelegate(TestIterator.T, HandleIterator)
            .AddSingleton(observations)
            .BuildServiceProvider();

        async IAsyncEnumerable<int> HandleIterator(
            TestIterator iterator,
            IServiceProvider _,
            [EnumeratorCancellation] CancellationToken _1
        )
        {
            handlerWasCalled = true;
            await Task.Yield();
            yield return iterator.Payload + 1;
            yield return iterator.Payload + 2;
            yield return iterator.Payload + 3;
        }

        var handler = ConfigureWithTransport(ResolveHandler(provider), b => b.UseInProcessIfAvailable());

        _ = await ConsumeAll(handler.Handle(CreateIterator(), CancellationToken.None));

        Assert.That(observations.Iterators, Has.Count.Zero);
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

        _ = await ConsumeAll(handler.Handle(CreateIterator(), CancellationToken.None));

        Assert.That(observations.Iterators, Has.Count.EqualTo(expected: 1));
    }

    [Test]
    public async Task GivenHandlerClientWithInProcessClientIfAvailable_WhenCallingClientHandlerInProcessDisabled_OtherTransportIsUsed()
    {
        var observations = new TestObservations();

        await using var provider = RegisterHandler(new ServiceCollection())
            .AddIteratorHandler<TestIteratorHandlerWithInProcessServerConfiguration>()
            .AddSingleton<Action<IInProcessIteratorServer>>(r => r.Disable())
            .AddSingleton(observations)
            .BuildServiceProvider();

        var handler = ConfigureWithTransport(ResolveHandler(provider), b => b.UseInProcessIfAvailable());

        _ = await ConsumeAll(handler.Handle(CreateIterator(), CancellationToken.None));

        Assert.That(observations.Iterators, Has.Count.EqualTo(expected: 1));
    }

    protected abstract TestIterator.IHandler ConfigureWithTransport(
        TestIterator.IHandler handler,
        Func<IteratorClientBuilder<TestIterator, int>, IIteratorClient<TestIterator, int>?>? baseConfigure = null
    );

    protected sealed override IServiceCollection RegisterHandler(IServiceCollection services) =>
        services.AddConqueror().AddSingleton(typeof(TestIteratorTransport<,>));

    protected sealed override TestIterator.IHandler ResolveHandler(IServiceProvider serviceProvider) =>
        ConfigureWithTransport(base.ResolveHandler(serviceProvider));

    protected sealed class TestIteratorTransport<TIterator, TItem>(Exception? exception = null)
        : IIteratorClient<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public string TransportTypeName => "test";

        public async IAsyncEnumerable<TItem> Execute(
            TIterator iterator,
            IServiceProvider serviceProvider,
            ConquerorContext conquerorContext,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            var observations = serviceProvider.GetRequiredService<TestObservations>();
            observations.Iterators.Add(iterator);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            var cmd = (TestIterator)(object)iterator;

            yield return (TItem)(object)(cmd.Payload + 1);
            yield return (TItem)(object)(cmd.Payload + 2);
            yield return (TItem)(object)(cmd.Payload + 3);
        }
    }

    private sealed partial class TestIteratorHandlerWithInProcessServerConfiguration : TestIterator.IHandler
    {
        public async IAsyncEnumerable<int> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            yield return iterator.Payload + 1;
            yield return iterator.Payload + 2;
            yield return iterator.Payload + 3;
        }

        static void IIteratorHandler.ConfigureInProcessServer(IInProcessIteratorServer server) =>
            server.ServiceProvider.GetRequiredService<Action<IInProcessIteratorServer>>().Invoke(server);
    }
}

[TestFixture]
[SuppressMessage(
    "Style",
    "MA0040:Use an overload with a CancellationToken",
    Justification = "Tests are intentionally validating token flow"
)]
public sealed class IteratorHandlerFunctionalityClientWithSyncTransportFactoryTests
    : IteratorHandlerFunctionalityClientTests
{
    protected override bool BuildsClientPerExecution => false;

    protected override TestIterator.IHandler ConfigureWithTransport(
        TestIterator.IHandler handler,
        Func<IteratorClientBuilder<TestIterator, int>, IIteratorClient<TestIterator, int>?>? baseConfigure = null
    )
    {
        return handler.WithTransport(b =>
        {
            b.ServiceProvider.GetRequiredService<TestObservations>()
                .ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);

            return baseConfigure?.Invoke(b)
                ?? b.ServiceProvider.GetRequiredService<TestIteratorTransport<TestIterator, int>>();
        });
    }
}

[TestFixture]
[SuppressMessage(
    "Style",
    "MA0040:Use an overload with a CancellationToken",
    Justification = "Tests are intentionally validating token flow"
)]
public sealed class IteratorHandlerFunctionalityClientWithAsyncTransportFactoryTests
    : IteratorHandlerFunctionalityClientTests
{
    protected override bool BuildsClientPerExecution => true;

    protected override TestIterator.IHandler ConfigureWithTransport(
        TestIterator.IHandler handler,
        Func<IteratorClientBuilder<TestIterator, int>, IIteratorClient<TestIterator, int>?>? baseConfigure = null
    )
    {
        return handler.WithTransport(async b =>
        {
            await Task.Delay(millisecondsDelay: 1);
            b.ServiceProvider.GetRequiredService<TestObservations>()
                .ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);

            return baseConfigure?.Invoke(b)
                ?? b.ServiceProvider.GetRequiredService<TestIteratorTransport<TestIterator, int>>();
        });
    }
}
