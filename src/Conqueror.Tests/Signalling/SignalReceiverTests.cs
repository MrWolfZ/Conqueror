using System.ComponentModel;
using System.Diagnostics;
using Conqueror.Signalling;

namespace Conqueror.Tests.Signalling;

public sealed partial class SignalReceiverTests
{
    [Test]
    public async Task GivenHandlerWithReceiverConfiguration_WhenRunningReceiver_ReceiverGetsConfiguredCorrectly()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton<TestSignalTransportReceiverHost>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<TestSignalTransportReceiverHost>();

        var configurations = await receiver.Run();

        Assert.That(
            configurations,
            Is.EquivalentTo(
                new[]
                {
                    (typeof(TestSignalWithTestTransport), new() { Parameter = 10, Parameter2 = 1 }),
                    (typeof(TestSignal2WithTestTransport), new TestTransportSignalReceiverConfiguration { Parameter = 20, Parameter2 = 2 }),
                }));

        var signal = new TestSignalWithTestTransport();

        await receiver.Receive<TestSignalWithTestTransport, TestSignalWithTestTransport.IHandler, TestSignalHandler>(signal, CancellationToken.None);

        Assert.That(observations.ReceivedSignals, Is.EquivalentTo(new[] { signal }));
    }

    [Test]
    public async Task GivenHandlerWithReceiverConfigurationForMixedTransports_WhenRunningReceiver_ReceiverGetsCorrectConfigurationForTransport()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<MixedTestSignalHandler>()
                    .AddSingleton<TestSignalTransportReceiverHost>()
                    .AddSingleton<TestSignalTransport2ReceiverHost>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var receiver1 = provider.GetRequiredService<TestSignalTransportReceiverHost>();
        var receiver2 = provider.GetRequiredService<TestSignalTransport2ReceiverHost>();

        var configurations1 = await receiver1.Run();
        var configurations2 = receiver2.Run();

        Assert.That(
            configurations1,
            Is.EquivalentTo(
                new[]
                {
                    (typeof(TestSignalWithTestTransport), new() { Parameter = 10, Parameter2 = 0 }),
                    (typeof(TestSignal2WithTestTransport), new TestTransportSignalReceiverConfiguration { Parameter = 20, Parameter2 = 0 }),
                }));

        Assert.That(
            configurations2,
            Is.EquivalentTo(
                new[]
                {
                    (typeof(TestSignalWithTestTransport2), new TestTransport2SignalReceiverConfiguration { Parameter = 100 }),
                }));

        var signal1 = new TestSignalWithTestTransport();
        var signal2 = new TestSignal2WithTestTransport();
        var signal3 = new TestSignalWithTestTransport2();

        await receiver1.Receive<TestSignalWithTestTransport, TestSignalWithTestTransport.IHandler, MixedTestSignalHandler>(signal1, CancellationToken.None);
        await receiver1.Receive<TestSignal2WithTestTransport, TestSignal2WithTestTransport.IHandler, MixedTestSignalHandler>(signal2, CancellationToken.None);
        await receiver2.Receive(signal3, CancellationToken.None);

        Assert.That(observations.ReceivedSignals, Is.EquivalentTo(new object[] { signal1, signal2, signal3 }));
    }

    [Test]
    public async Task
        GivenHandlerWithReceiverConfigurationForSignalTypeWithMultipleTransports_WhenRunningReceiver_ReceiverGetsCorrectConfigurationForTransport()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalWithMultipleTransportsHandler>()
                    .AddSingleton<TestSignalTransportReceiverHost>()
                    .AddSingleton<TestSignalTransport2ReceiverHost>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var receiver1 = provider.GetRequiredService<TestSignalTransportReceiverHost>();
        var receiver2 = provider.GetRequiredService<TestSignalTransport2ReceiverHost>();

        var configurations1 = await receiver1.Run();
        var configurations2 = receiver2.Run();

        Assert.That(
            configurations1,
            Is.EquivalentTo(
                new[]
                {
                    (typeof(TestSignalWithMultipleTestTransports), new TestTransportSignalReceiverConfiguration { Parameter = 10, Parameter2 = 0 }),
                }));

        Assert.That(
            configurations2,
            Is.EquivalentTo(
                new[]
                {
                    (typeof(TestSignalWithMultipleTestTransports), new TestTransport2SignalReceiverConfiguration { Parameter = 20 }),
                }));
    }

    [Test]
    public async Task GivenHandlerWithReceiverConfigurationForSignalTypeHierarchy_WhenRunningReceiver_ReceiverGetsCorrectConfigurationForTransport()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalWithTypeHierarchyHandler>()
                    .AddSingleton<TestSignalTransportReceiverHost>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<TestSignalTransportReceiverHost>();

        var configurations = await receiver.Run();

        Assert.That(
            configurations,
            Is.EquivalentTo(
                new[]
                {
                    (typeof(TestSignalWithTestTransportBase), new TestTransportSignalReceiverConfiguration { Parameter = 10, Parameter2 = 1 }),
                    (typeof(TestSignalWithTestTransportSub), new() { Parameter = 20, Parameter2 = 2 }),
                }));
    }

    private sealed partial class TestSignalHandler(TestObservations observations) : TestSignalWithTestTransport.IHandler,
                                                                                    TestSignal2WithTestTransport.IHandler
    {
        public Task Handle(TestSignalWithTestTransport signal, CancellationToken cancellationToken = default)
        {
            observations.ReceivedSignals.Add(signal);

            return Task.CompletedTask;
        }

        public Task Handle(TestSignal2WithTestTransport signal, CancellationToken cancellationToken = default)
        {
            observations.ReceivedSignals.Add(signal);

            return Task.CompletedTask;
        }

        static Task ITestTransportSignalHandler.ConfigureTestTransportReceiver(ITestTransportSignalReceiver receiver)
        {
            var observations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            observations.ConfigureReceiverCallCount += 1;

            _ = receiver.Disable();
            _ = receiver.Enable(10).WithParameter2(1);

            if (receiver.SignalType == typeof(TestSignal2WithTestTransport))
            {
                _ = receiver.Enable(20).WithParameter2(2);
            }

            return Task.CompletedTask;
        }
    }

    private sealed partial class MixedTestSignalHandler(TestObservations observations)
        : TestSignalWithTestTransport.IHandler,
          TestSignal2WithTestTransport.IHandler,
          TestSignalWithTestTransport2.IHandler
    {
        public Task Handle(TestSignalWithTestTransport signal, CancellationToken cancellationToken = default)
        {
            observations.ReceivedSignals.Add(signal);

            return Task.CompletedTask;
        }

        public Task Handle(TestSignal2WithTestTransport signal, CancellationToken cancellationToken = default)
        {
            observations.ReceivedSignals.Add(signal);

            return Task.CompletedTask;
        }

        public Task Handle(TestSignalWithTestTransport2 signal, CancellationToken cancellationToken = default)
        {
            observations.ReceivedSignals.Add(signal);

            return Task.CompletedTask;
        }

        static Task ITestTransportSignalHandler.ConfigureTestTransportReceiver(ITestTransportSignalReceiver receiver)
        {
            var observations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            observations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(10);

            if (receiver.SignalType == typeof(TestSignal2WithTestTransport))
            {
                _ = receiver.Enable(20);
            }

            return Task.CompletedTask;
        }

        static void ITestTransport2SignalHandler.ConfigureTestTransport2Receiver(ITestTransport2SignalReceiver receiver)
        {
            var observations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            observations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(100);
        }
    }

    private sealed partial class TestSignalWithMultipleTransportsHandler(TestObservations observations) : TestSignalWithMultipleTestTransports.IHandler
    {
        public Task Handle(TestSignalWithMultipleTestTransports signal, CancellationToken cancellationToken = default)
        {
            observations.ReceivedSignals.Add(signal);

            return Task.CompletedTask;
        }

        static Task ITestTransportSignalHandler.ConfigureTestTransportReceiver(ITestTransportSignalReceiver receiver)
        {
            var observations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            observations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(10);

            return Task.CompletedTask;
        }

        static void ITestTransport2SignalHandler.ConfigureTestTransport2Receiver(ITestTransport2SignalReceiver receiver)
        {
            var observations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            observations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(20);
        }
    }

    private sealed partial class TestSignalWithTypeHierarchyHandler(TestObservations observations) : TestSignalWithTestTransportBase.IHandler,
                                                                                                     TestSignalWithTestTransportSub.IHandler
    {
        public Task Handle(TestSignalWithTestTransportBase signal, CancellationToken cancellationToken = default)
        {
            observations.ReceivedSignals.Add(signal);

            return Task.CompletedTask;
        }

        public Task Handle(TestSignalWithTestTransportSub signal, CancellationToken cancellationToken = default)
        {
            observations.ReceivedSignals.Add(signal);

            return Task.CompletedTask;
        }

        static Task ITestTransportSignalHandler.ConfigureTestTransportReceiver(ITestTransportSignalReceiver receiver)
        {
            var observations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            observations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(10).WithParameter2(1);

            if (receiver.SignalType == typeof(TestSignalWithTestTransportSub))
            {
                _ = receiver.Enable(20).WithParameter2(2);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TestObservations
    {
        public int ConfigureReceiverCallCount { get; set; }

        public List<object> ReceivedSignals { get; } = [];
    }
}

[TestTransportSignal]
public sealed partial record TestSignalWithTestTransport;

[TestTransportSignal(DefaultParameter = -2)]
public sealed partial record TestSignal2WithTestTransport;

[TestTransport2Signal]
public sealed partial record TestSignalWithTestTransport2;

[TestTransportSignal]
[TestTransport2Signal]
public sealed partial record TestSignalWithMultipleTestTransports;

[TestTransportSignal]
public partial record TestSignalWithTestTransportBase;

[TestTransportSignal]
public sealed partial record TestSignalWithTestTransportSub : TestSignalWithTestTransportBase;

file sealed class TestSignalTransportReceiverHost(IServiceProvider serviceProvider, ISignalHandlerRegistry registry)
{
    private const string TestTransportTypeName = "test-transport";

    private readonly Dictionary<(Type SignalType, Type HandlerType), ITestTransportSignalReceiver> receiverBySignalAndHandlerType = new();

    public async Task<IReadOnlyCollection<(Type SignalType, TestTransportSignalReceiverConfiguration Configuration)>> Run()
    {
        var typesInjectors = registry.GetReceiverHandlerInvokers<ITestTransportSignalHandlerTypesInjector>();
        var result = new List<(Type SignalType, TestTransportSignalReceiverConfiguration Configuration)>();

        foreach (var invoker in typesInjectors)
        {
            var receiver = await invoker.TypesInjector.Create(new Injectable(invoker, serviceProvider));

            if (receiver.Configuration is not null)
            {
                result.Add((invoker.SignalType, receiver.Configuration));
            }

            receiverBySignalAndHandlerType.Add(
                (invoker.SignalType, invoker.HandlerType ?? throw new NotSupportedException("delegates are not supported")),
                receiver);
        }

        return result;
    }

    public async Task Receive<TSignal, TIHandler, THandler>(TSignal signal, CancellationToken cancellationToken)
        where TSignal : class, ITestTransportSignal<TSignal>
        where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
        where THandler : class, TIHandler
    {
        var receiver = receiverBySignalAndHandlerType.GetValueOrDefault((typeof(TSignal), typeof(THandler)))
                       ?? throw new InvalidOperationException($"no configuration for handler type {typeof(THandler)}");
        await receiver.Invoke(signal, cancellationToken);
    }

    private sealed class Injectable(ISignalReceiverHandlerInvoker invoker, IServiceProvider serviceProvider)
        : ITestTransportSignalHandlerTypesInjectable<Task<ITestTransportSignalReceiver>>
    {
        async Task<ITestTransportSignalReceiver> ITestTransportSignalHandlerTypesInjectable<Task<ITestTransportSignalReceiver>>
            .WithInjectedTypes<TSignal, TIHandler, THandler>()
        {
            var receiverBuilder = new TestTransportSignalReceiver<TSignal>(serviceProvider, Invoke);

            // this showcases that configuration can be async and can be done per signal type if necessary
            await THandler.ConfigureTestTransportReceiver(receiverBuilder);

            return receiverBuilder;

            Task Invoke(TSignal signal, CancellationToken ct) => invoker.Invoke(
                signal,
                serviceProvider,
                TestTransportTypeName,
                ct);
        }
    }
}

file sealed class TestSignalTransport2ReceiverHost(IServiceProvider serviceProvider, ISignalHandlerRegistry registry)
{
    private readonly Dictionary<Type, TestTransport2SignalReceiver> receiverByHandlerType = new();

    public IReadOnlyCollection<(Type SignalType, TestTransport2SignalReceiverConfiguration Configuration)> Run()
    {
        var typesInjectors = registry.GetReceiverHandlerInvokers<ITestTransport2SignalHandlerTypesInjector>();
        var result = new List<(Type SignalType, TestTransport2SignalReceiverConfiguration Configuration)>();

        foreach (var invoker in typesInjectors)
        {
            var receiver = invoker.TypesInjector.Create(new Injectable(invoker, serviceProvider, receiverByHandlerType));

            if (receiver.Configuration is not null)
            {
                result.Add((invoker.SignalType, receiver.Configuration));
            }
        }

        return result;
    }

    public async Task Receive<TSignal>(TSignal signal, CancellationToken cancellationToken)
        where TSignal : class, ITestTransport2Signal<TSignal>
    {
        foreach (var receiver in receiverByHandlerType.Values)
        {
            await receiver.Invoke(signal, cancellationToken);
        }
    }

    private sealed class Injectable(
        ISignalReceiverHandlerInvoker invoker,
        IServiceProvider serviceProvider,
        Dictionary<Type, TestTransport2SignalReceiver> receiverByHandlerType)
        : ITestTransport2TypesInjectable<TestTransport2SignalReceiver>
    {
        TestTransport2SignalReceiver ITestTransport2TypesInjectable<TestTransport2SignalReceiver>
            .WithInjectedTypes<TSignal, TIHandler, THandler>()
        {
            if (!receiverByHandlerType.TryGetValue(typeof(THandler), out var receiver))
            {
                receiverByHandlerType.Add(typeof(THandler), receiver = new(serviceProvider));

                // this showcases the usual case where configuration is done once per handler type
                THandler.ConfigureTestTransport2Receiver(receiver);
            }

            receiver.AddInvoker(invoker);

            return receiver;
        }
    }
}

[SignalTransport(Prefix = "TestTransport", Namespace = "Conqueror.Tests.Signalling")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportSignalAttribute : Attribute
{
    public int DefaultParameter { get; init; }
}

public interface ITestTransportSignal<out TSignal> : ISignal<TSignal>
    where TSignal : class, ITestTransportSignal<TSignal>
{
    [SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "testing")]
    static virtual int DefaultParameter { get; } = -1;
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITestTransportSignalHandler
{
    static virtual Task ConfigureTestTransportReceiver(ITestTransportSignalReceiver receiver)
    {
        // by default, we don't configure the receiver
        return Task.CompletedTask;
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITestTransportSignalHandler<TSignal, TIHandler> : ISignalHandler<TSignal, TIHandler>,
                                                                   ITestTransportSignalHandler
    where TSignal : class, ITestTransportSignal<TSignal>
    where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static ISignalHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => TestTransportSignalHandlerTypesInjector<TSignal, TIHandler, THandler>.Default;
}

public interface ITestTransportSignalReceiver
{
    TestTransportSignalReceiverConfiguration? Configuration { get; }

    Type SignalType { get; }

    /// <summary>
    ///     Note that this is (usually) the service provider from the global scope,
    ///     and <i>not</i> the service provider from the scope of the send operation.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    internal Task Invoke<TSignal>(TSignal signal, CancellationToken cancellationToken)
        where TSignal : class, ITestTransportSignal<TSignal>;

    ITestTransportSignalReceiver Enable(int? parameter = null);

    ITestTransportSignalReceiver WithParameter2(int parameter2);
    ITestTransportSignalReceiver Disable();
}

internal interface ITestTransportSignalHandlerTypesInjector : ISignalHandlerTypesInjector
{
    TResult Create<TResult>(ITestTransportSignalHandlerTypesInjectable<TResult> injectable);
}

file sealed class TestTransportSignalHandlerTypesInjector<TSignal, TIHandler, THandler> : ITestTransportSignalHandlerTypesInjector
    where TSignal : class, ITestTransportSignal<TSignal>
    where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
    where THandler : class, TIHandler
{
    public static readonly TestTransportSignalHandlerTypesInjector<TSignal, TIHandler, THandler> Default = new();

    public Type SignalType { get; } = typeof(TSignal);

    public TResult Create<TResult>(ITestTransportSignalHandlerTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TSignal, TIHandler, THandler>();
}

public interface ITestTransportSignalHandlerTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TSignal, TIHandler, THandler>()
        where TSignal : class, ITestTransportSignal<TSignal>
        where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
        where THandler : class, TIHandler;
}

file sealed class TestTransportSignalReceiver<TSignal>(
    IServiceProvider serviceProvider,
    Func<TSignal, CancellationToken, Task> invokeFn)
    : ITestTransportSignalReceiver
    where TSignal : class, ITestTransportSignal<TSignal>
{
    public Type SignalType { get; } = typeof(TSignal);

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public TestTransportSignalReceiverConfiguration? Configuration { get; private set; }

    public ITestTransportSignalReceiver Enable(int? parameter = null)
    {
        Configuration = new() { Parameter = parameter ?? TSignal.DefaultParameter };

        return this;
    }

    public ITestTransportSignalReceiver WithParameter2(int parameter2)
    {
        if (Configuration is null)
        {
            throw new InvalidOperationException("cannot set parameter 2 on receiver that has not been configured yet");
        }

        Configuration.Parameter2 = parameter2;

        return this;
    }

    public ITestTransportSignalReceiver Disable()
    {
        Configuration = null;

        return this;
    }

    public Task Invoke<T>(T signal, CancellationToken cancellationToken)
        where T : class, ITestTransportSignal<T>
    {
        Debug.Assert(typeof(T) == typeof(TSignal), $"wrong signal type, expected '{typeof(TSignal)}', got '{typeof(T)}'");

        return invokeFn((signal as TSignal)!, cancellationToken);
    }
}

public sealed record TestTransportSignalReceiverConfiguration
{
    public required int Parameter { get; set; }

    public int Parameter2 { get; set; }
}

[SignalTransport(Prefix = "TestTransport2", Namespace = "Conqueror.Tests.Signalling")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransport2SignalAttribute : Attribute;

public interface ITestTransport2Signal<out TSignal> : ISignal<TSignal>
    where TSignal : class, ITestTransport2Signal<TSignal>;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITestTransport2SignalHandler
{
    static virtual void ConfigureTestTransport2Receiver(ITestTransport2SignalReceiver receiver)
    {
        // by default, we don't configure the receiver
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITestTransport2SignalHandler<TSignal, TIHandler> : ISignalHandler<TSignal, TIHandler>,
                                                                    ITestTransport2SignalHandler
    where TSignal : class, ITestTransport2Signal<TSignal>
    where TIHandler : class, ITestTransport2SignalHandler<TSignal, TIHandler>
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static ISignalHandlerTypesInjector CreateTestTransport2TypesInjector<THandler>()
        where THandler : class, TIHandler
        => TestTransport2SignalHandlerTypesInjector<TSignal, TIHandler, THandler>.Default;
}

public interface ITestTransport2SignalReceiver
{
    TestTransport2SignalReceiverConfiguration? Configuration { get; }

    /// <summary>
    ///     Note that this is (usually) the service provider from the global scope,
    ///     and <i>not</i> the service provider from the scope of the send operation.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    internal Task Invoke<TSignal>(TSignal signal, CancellationToken cancellationToken)
        where TSignal : class, ITestTransport2Signal<TSignal>;

    ITestTransport2SignalReceiver Enable(int parameter);
}

public interface ITestTransport2SignalHandlerTypesInjector : ISignalHandlerTypesInjector
{
    TResult Create<TResult>(ITestTransport2TypesInjectable<TResult> injectable);
}

file sealed class TestTransport2SignalHandlerTypesInjector<TSignal, TIHandler, THandler> : ITestTransport2SignalHandlerTypesInjector
    where TSignal : class, ITestTransport2Signal<TSignal>
    where TIHandler : class, ITestTransport2SignalHandler<TSignal, TIHandler>
    where THandler : class, TIHandler
{
    public static readonly TestTransport2SignalHandlerTypesInjector<TSignal, TIHandler, THandler> Default = new();

    public Type SignalType { get; } = typeof(TSignal);

    public TResult Create<TResult>(ITestTransport2TypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TSignal, TIHandler, THandler>();
}

public interface ITestTransport2TypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TSignal, TIHandler, THandler>()
        where TSignal : class, ITestTransport2Signal<TSignal>
        where TIHandler : class, ITestTransport2SignalHandler<TSignal, TIHandler>
        where THandler : class, TIHandler;
}

file sealed class TestTransport2SignalReceiver(IServiceProvider serviceProvider) : ITestTransport2SignalReceiver
{
    private const string TestTransportTypeName = "test-transport-2";

    private readonly List<ISignalReceiverHandlerInvoker> invokers = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public TestTransport2SignalReceiverConfiguration? Configuration { get; private set; }

    public void AddInvoker(ISignalReceiverHandlerInvoker invoker)
    {
        invokers.Add(invoker);
    }

    public ITestTransport2SignalReceiver Enable(int parameter)
    {
        Configuration = new() { Parameter = parameter };

        return this;
    }

    public async Task Invoke<TSignal>(TSignal signal, CancellationToken cancellationToken)
        where TSignal : class, ITestTransport2Signal<TSignal>
    {
        var relevantInvokers = invokers.Where(i => i.SignalType.IsInstanceOfType(signal))
                                       .ToList();

        foreach (var invoker in relevantInvokers)
        {
            await invoker.Invoke(signal, ServiceProvider, TestTransportTypeName, cancellationToken);
        }
    }
}

public sealed record TestTransport2SignalReceiverConfiguration
{
    public required int Parameter { get; set; }
}
