using System.ComponentModel;
using System.Diagnostics;

namespace Conqueror.Tests.Signalling;

public sealed class SignalReceiverTests
{
    [Test]
    public async Task GivenHandlerWithReceiverConfiguration_WhenRunningReceiver_ReceiverGetsConfiguredCorrectly()
    {
        var services = new ServiceCollection();
        var testObservations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton<TestSignalTransportReceiverHost>()
                    .AddSingleton(testObservations);

        var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<TestSignalTransportReceiverHost>();

        // TODO: actually invoke the invokers and assert that signals are received
        var configurations = await receiver.Run(CancellationToken.None);

        Assert.That(configurations, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalWithTestTransport), new() { Parameter = 10, Parameter2 = 1 }),
            (typeof(TestSignal2WithTestTransport), new TestSignalTransportReceiverConfiguration { Parameter = 20, Parameter2 = 2 }),
        }));
    }

    [Test]
    public async Task GivenHandlerWithReceiverConfigurationForMixedTransports_WhenRunningReceiver_ReceiverGetsCorrectConfigurationForTransport()
    {
        var services = new ServiceCollection();
        var testObservations = new TestObservations();

        _ = services.AddSignalHandler<MixedTestSignalHandler>()
                    .AddSingleton<TestSignalTransportReceiverHost>()
                    .AddSingleton<TestSignalTransport2ReceiverHost>()
                    .AddSingleton(testObservations);

        var provider = services.BuildServiceProvider();

        var receiver1 = provider.GetRequiredService<TestSignalTransportReceiverHost>();
        var receiver2 = provider.GetRequiredService<TestSignalTransport2ReceiverHost>();

        var configurations1 = await receiver1.Run(CancellationToken.None);
        var configurations2 = await receiver2.Run(CancellationToken.None);

        Assert.That(configurations1, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalWithTestTransport), new() { Parameter = 10, Parameter2 = 0 }),
            (typeof(TestSignal2WithTestTransport), new TestSignalTransportReceiverConfiguration { Parameter = 20, Parameter2 = 0 }),
        }));

        Assert.That(configurations2, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalWithTestTransport2), new TestSignalTransport2ReceiverConfiguration { Parameter = 100 }),
        }));
    }

    [Test]
    public async Task GivenHandlerWithReceiverConfigurationForSignalTypeWithMultipleTransports_WhenRunningReceiver_ReceiverGetsCorrectConfigurationForTransport()
    {
        var services = new ServiceCollection();
        var testObservations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalWithMultipleTransportsHandler>()
                    .AddSingleton<TestSignalTransportReceiverHost>()
                    .AddSingleton<TestSignalTransport2ReceiverHost>()
                    .AddSingleton(testObservations);

        var provider = services.BuildServiceProvider();

        var receiver1 = provider.GetRequiredService<TestSignalTransportReceiverHost>();
        var receiver2 = provider.GetRequiredService<TestSignalTransport2ReceiverHost>();

        var configurations1 = await receiver1.Run(CancellationToken.None);
        var configurations2 = await receiver2.Run(CancellationToken.None);

        Assert.That(configurations1, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalWithMultipleTestTransports), new TestSignalTransportReceiverConfiguration { Parameter = 10, Parameter2 = 0 }),
        }));

        Assert.That(configurations2, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalWithMultipleTestTransports), new TestSignalTransport2ReceiverConfiguration { Parameter = 20 }),
        }));
    }

    private sealed class TestSignalHandler : TestSignalWithTestTransport.IHandler, TestSignal2WithTestTransport.IHandler
    {
        public Task Handle(TestSignalWithTestTransport signal, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestSignal2WithTestTransport signal, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public static Task ConfigureTestTransportReceiver<T>(ITestTransportSignalReceiver<T> receiver)
            where T : class, ITestTransportSignal<T>
        {
            var testObservations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            testObservations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(10).WithParameter2(1);

            if (typeof(T) == typeof(TestSignal2WithTestTransport))
            {
                _ = receiver.Enable(20).WithParameter2(2);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class MixedTestSignalHandler
        : TestSignalWithTestTransport.IHandler,
          TestSignal2WithTestTransport.IHandler,
          TestSignalWithTestTransport2.IHandler
    {
        public Task Handle(TestSignalWithTestTransport signal, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestSignal2WithTestTransport signal, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestSignalWithTestTransport2 signal, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public static Task ConfigureTestTransportReceiver<T>(ITestTransportSignalReceiver<T> receiver)
            where T : class, ITestTransportSignal<T>
        {
            var testObservations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            testObservations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(10);

            if (typeof(T) == typeof(TestSignal2WithTestTransport))
            {
                _ = receiver.Enable(20);
            }

            return Task.CompletedTask;
        }

        public static Task ConfigureTestTransport2Receiver<T>(ITestTransport2SignalReceiver<T> receiver)
            where T : class, ITestTransport2Signal<T>
        {
            var testObservations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            testObservations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(100);
            return Task.CompletedTask;
        }
    }

    private sealed class TestSignalWithMultipleTransportsHandler : TestSignalWithMultipleTestTransports.IHandler
    {
        public Task Handle(TestSignalWithMultipleTestTransports signal, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public static Task ConfigureTestTransportReceiver<T>(ITestTransportSignalReceiver<T> receiver)
            where T : class, ITestTransportSignal<T>
        {
            var testObservations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            testObservations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(10);

            return Task.CompletedTask;
        }

        public static Task ConfigureTestTransport2Receiver<T>(ITestTransport2SignalReceiver<T> receiver)
            where T : class, ITestTransport2Signal<T>
        {
            var testObservations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            testObservations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(20);

            return Task.CompletedTask;
        }
    }

    private sealed class TestObservations
    {
        public int ConfigureReceiverCallCount { get; set; }
    }
}

[Signal]
public sealed partial record TestSignalWithTestTransport : ITestTransportSignal<TestSignalWithTestTransport>
{
    static ITestTransportTypesInjector ITestTransportSignal.TestTransportTypesInjector
        => TestTransportTypesInjector<TestSignalWithTestTransport, IHandler>.Default;

    public partial interface IHandler : IGeneratedTestTransportSignalHandler;
}

[Signal]
public sealed partial record TestSignal2WithTestTransport : ITestTransportSignal<TestSignal2WithTestTransport>
{
    static ITestTransportTypesInjector ITestTransportSignal.TestTransportTypesInjector
        => TestTransportTypesInjector<TestSignal2WithTestTransport, IHandler>.Default;

    public partial interface IHandler : IGeneratedTestTransportSignalHandler;
}

[Signal]
public sealed partial record TestSignalWithTestTransport2 : ITestTransport2Signal<TestSignalWithTestTransport2>
{
    static ITestTransport2TypesInjector ITestTransport2Signal.TestTransport2TypesInjector
        => TestTransport2TypesInjector<TestSignalWithTestTransport2, IHandler>.Default;

    public partial interface IHandler : IGeneratedTestTransport2SignalHandler;
}

[Signal]
public sealed partial record TestSignalWithMultipleTestTransports : ITestTransportSignal<TestSignalWithMultipleTestTransports>,
                                                                    ITestTransport2Signal<TestSignalWithMultipleTestTransports>
{
    static ITestTransportTypesInjector ITestTransportSignal.TestTransportTypesInjector
        => TestTransportTypesInjector<TestSignalWithMultipleTestTransports, IHandler>.Default;

    static ITestTransport2TypesInjector ITestTransport2Signal.TestTransport2TypesInjector
        => TestTransport2TypesInjector<TestSignalWithMultipleTestTransports, IHandler>.Default;

    public partial interface IHandler : IGeneratedTestTransportSignalHandler;

    public partial interface IHandler : IGeneratedTestTransport2SignalHandler;
}

file sealed class TestSignalTransportReceiverHost(IServiceProvider serviceProvider, ISignalTransportRegistry registry)
{
    public async Task<IReadOnlyCollection<(Type SignalType, TestSignalTransportReceiverConfiguration Configuration)>> Run(CancellationToken cancellationToken)
    {
        var invokers = registry.GetSignalInvokersForReceiver<ITestTransportTypesInjector>();
        var result = new List<(Type SignalType, TestSignalTransportReceiverConfiguration Configuration)>();

        foreach (var invoker in invokers)
        {
            var configuration = await invoker.TypesInjector.CreateForTestTransport(new Injectable(serviceProvider, cancellationToken));

            if (configuration is not null)
            {
                result.Add((invoker.SignalType, configuration));
            }
        }

        return result;
    }

    private sealed class Injectable(IServiceProvider serviceProvider, CancellationToken cancellationToken) : ITestTransportTypesInjectable<Task<TestSignalTransportReceiverConfiguration?>>
    {
        async Task<TestSignalTransportReceiverConfiguration?> ITestTransportTypesInjectable<Task<TestSignalTransportReceiverConfiguration?>>
            .WithInjectedTypes<TSignal, THandler>()
        {
            var receiverBuilder = new TestTransportSignalReceiver<TSignal>(serviceProvider, cancellationToken);
            await THandler.ConfigureTestTransportReceiver(receiverBuilder);
            return receiverBuilder.Configuration;
        }
    }
}

file sealed class TestSignalTransport2ReceiverHost(IServiceProvider serviceProvider, ISignalTransportRegistry registry)
{
    public async Task<IReadOnlyCollection<(Type SignalType, TestSignalTransport2ReceiverConfiguration Configuration)>> Run(CancellationToken cancellationToken)
    {
        var invokers = registry.GetSignalInvokersForReceiver<ITestTransport2TypesInjector>();
        var result = new List<(Type SignalType, TestSignalTransport2ReceiverConfiguration Configuration)>();

        foreach (var invoker in invokers)
        {
            var configuration = await invoker.TypesInjector.CreateForTestTransport2(new Injectable(serviceProvider, cancellationToken));

            if (configuration is not null)
            {
                result.Add((invoker.SignalType, configuration));
            }
        }

        return result;
    }

    private sealed class Injectable(IServiceProvider serviceProvider, CancellationToken cancellationToken) : ITestTransport2TypesInjectable<Task<TestSignalTransport2ReceiverConfiguration?>>
    {
        async Task<TestSignalTransport2ReceiverConfiguration?> ITestTransport2TypesInjectable<Task<TestSignalTransport2ReceiverConfiguration?>>
            .WithInjectedTypes<TSignal, THandler>()
        {
            var receiverBuilder = new TestTransport2SignalReceiver<TSignal>(serviceProvider, cancellationToken);
            await THandler.ConfigureTestTransport2Receiver(receiverBuilder);
            return receiverBuilder.Configuration;
        }
    }
}

public interface ITestTransportSignal
{
    // must be virtual instead of abstract so that it can be used as a type parameter / constraint
    // ReSharper disable once UnusedMember.Global (used by reflection)
    static virtual ITestTransportTypesInjector TestTransportTypesInjector
        => throw new NotSupportedException("this method should always be implemented by the generic interface");
}

public interface ITestTransportSignal<T> : ISignal<T>, ITestTransportSignal
    where T : class, ITestTransportSignal<T>
{
    [SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "testing")]
    static virtual int DefaultParameter { get; } = -1;
}

public interface ITestTransportSignalReceiver<TSignal>
    where TSignal : class, ITestTransportSignal<TSignal>
{
    IServiceProvider ServiceProvider { get; }

    CancellationToken CancellationToken { get; }

    ITestTransportSignalReceiver<TSignal> Enable(int? parameter = null);

    ITestTransportSignalReceiver<TSignal> WithParameter2(int parameter2);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedTestTransportSignalHandler
{
    static virtual Task ConfigureTestTransportReceiver<T>(ITestTransportSignalReceiver<T> receiver)
        where T : class, ITestTransportSignal<T>
    {
        // by default, we don't configure the receiver
        return Task.CompletedTask;
    }
}

public interface ITestTransportTypesInjector : ISignalTypesInjector
{
    TResult CreateForTestTransport<TResult>(ITestTransportTypesInjectable<TResult> injectable);
}

file sealed class TestTransportTypesInjector<TSignal, THandlerInterface> : ITestTransportTypesInjector
    where TSignal : class, ITestTransportSignal<TSignal>
    where THandlerInterface : class, ISignalHandler<TSignal, THandlerInterface>
{
    public static readonly TestTransportTypesInjector<TSignal, THandlerInterface> Default = new();

    ISignalTypesInjector ISignalTypesInjector.WithHandlerType<THandler>()
    {
        Debug.Assert(typeof(THandler).IsAssignableTo(typeof(ISignalHandler<TSignal, THandlerInterface>)),
                     $"expected handler type '{typeof(THandler)}' to be assignable to '{typeof(ISignalHandler<TSignal, THandlerInterface>)}'");

        return Activator.CreateInstance(typeof(WithHandlerType<>)
                                            .MakeGenericType(typeof(TSignal), typeof(THandlerInterface), typeof(THandler)))
                   as ISignalTypesInjector
               ?? throw new InvalidOperationException("cannot create instance of WithHandlerType<THandler>");
    }

    public TResult CreateForTestTransport<TResult>(ITestTransportTypesInjectable<TResult> injectable)
        => throw new NotSupportedException($"handler type must be set via '{nameof(ISignalTypesInjector.WithHandlerType)}'");

    private sealed class WithHandlerType<THandler> : ITestTransportTypesInjector
        where THandler : class, ISignalHandler<TSignal, THandlerInterface>, IGeneratedTestTransportSignalHandler
    {
        public TResult CreateForTestTransport<TResult>(ITestTransportTypesInjectable<TResult> injectable)
            => injectable.WithInjectedTypes<TSignal, THandler>();

        ISignalTypesInjector ISignalTypesInjector.WithHandlerType<T>()
            => throw new NotSupportedException("cannot set handler type multiple times for types injector");
    }
}

public interface ITestTransportTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TSignal, THandler>()
        where TSignal : class, ITestTransportSignal<TSignal>
        where THandler : class, IGeneratedTestTransportSignalHandler;
}

file sealed class TestTransportSignalReceiver<T>(IServiceProvider serviceProvider, CancellationToken cancellationToken) : ITestTransportSignalReceiver<T>
    where T : class, ITestTransportSignal<T>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public TestSignalTransportReceiverConfiguration? Configuration { get; set; }

    public ITestTransportSignalReceiver<T> Enable(int? parameter = null)
    {
        Configuration = new() { Parameter = parameter ?? T.DefaultParameter };
        return this;
    }

    public ITestTransportSignalReceiver<T> WithParameter2(int parameter2)
    {
        if (Configuration is null)
        {
            throw new InvalidOperationException("cannot set parameter 2 on receiver that has not been configured yet");
        }

        Configuration.Parameter2 = parameter2;
        return this;
    }
}

public sealed record TestSignalTransportReceiverConfiguration
{
    public required int Parameter { get; set; }

    public int Parameter2 { get; set; }
}

public interface ITestTransport2Signal
{
    // must be virtual instead of abstract so that it can be used as a type parameter / constraint
    // ReSharper disable once UnusedMember.Global (used by reflection)
    static virtual ITestTransport2TypesInjector TestTransport2TypesInjector
        => throw new NotSupportedException("this method should always be implemented by the generic interface");
}

public interface ITestTransport2Signal<T> : ISignal<T>, ITestTransport2Signal
    where T : class, ITestTransport2Signal<T>;

public interface ITestTransport2SignalReceiver<TSignal>
    where TSignal : class, ITestTransport2Signal<TSignal>
{
    IServiceProvider ServiceProvider { get; }

    CancellationToken CancellationToken { get; }

    ITestTransport2SignalReceiver<TSignal> Enable(int parameter);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedTestTransport2SignalHandler
{
    static virtual Task ConfigureTestTransport2Receiver<T>(ITestTransport2SignalReceiver<T> receiver)
        where T : class, ITestTransport2Signal<T>
    {
        // by default, we don't configure the receiver
        return Task.CompletedTask;
    }
}

public interface ITestTransport2TypesInjector : ISignalTypesInjector
{
    TResult CreateForTestTransport2<TResult>(ITestTransport2TypesInjectable<TResult> injectable);
}

file sealed class TestTransport2TypesInjector<TSignal, THandlerInterface> : ITestTransport2TypesInjector
    where TSignal : class, ITestTransport2Signal<TSignal>
    where THandlerInterface : class, ISignalHandler<TSignal, THandlerInterface>
{
    public static readonly TestTransport2TypesInjector<TSignal, THandlerInterface> Default = new();

    ISignalTypesInjector ISignalTypesInjector.WithHandlerType<THandler>()
    {
        Debug.Assert(typeof(THandler).IsAssignableTo(typeof(ISignalHandler<TSignal, THandlerInterface>)),
                     $"expected handler type '{typeof(THandler)}' to be assignable to '{typeof(ISignalHandler<TSignal, THandlerInterface>)}'");

        return Activator.CreateInstance(typeof(WithHandlerType<>)
                                            .MakeGenericType(typeof(TSignal), typeof(THandlerInterface), typeof(THandler)))
                   as ISignalTypesInjector
               ?? throw new InvalidOperationException("cannot create instance of WithHandlerType<THandler>");
    }

    public TResult CreateForTestTransport2<TResult>(ITestTransport2TypesInjectable<TResult> injectable)
        => throw new NotSupportedException($"handler type must be set via '{nameof(ISignalTypesInjector.WithHandlerType)}'");

    private sealed class WithHandlerType<THandler> : ITestTransport2TypesInjector
        where THandler : class, ISignalHandler<TSignal, THandlerInterface>, IGeneratedTestTransport2SignalHandler
    {
        public TResult CreateForTestTransport2<TResult>(ITestTransport2TypesInjectable<TResult> injectable)
            => injectable.WithInjectedTypes<TSignal, THandler>();

        ISignalTypesInjector ISignalTypesInjector.WithHandlerType<T>()
            => throw new NotSupportedException("cannot set handler type multiple times for types injector");
    }
}

public interface ITestTransport2TypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TSignal, THandler>()
        where TSignal : class, ITestTransport2Signal<TSignal>
        where THandler : class, IGeneratedTestTransport2SignalHandler;
}

file sealed class TestTransport2SignalReceiver<T>(IServiceProvider serviceProvider, CancellationToken cancellationToken) : ITestTransport2SignalReceiver<T>
    where T : class, ITestTransport2Signal<T>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public TestSignalTransport2ReceiverConfiguration? Configuration { get; set; }

    public ITestTransport2SignalReceiver<T> Enable(int parameter)
    {
        Configuration = new() { Parameter = parameter };
        return this;
    }
}

public sealed record TestSignalTransport2ReceiverConfiguration
{
    public required int Parameter { get; set; }
}
