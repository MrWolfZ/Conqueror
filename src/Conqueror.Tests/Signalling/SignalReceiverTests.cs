using System.ComponentModel;
using Conqueror.Signalling;

namespace Conqueror.Tests.Signalling;

public sealed partial class SignalReceiverTests
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
            (typeof(TestSignal2WithTestTransport), new TestTransportSignalReceiverConfiguration { Parameter = 20, Parameter2 = 2 }),
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
            (typeof(TestSignal2WithTestTransport), new TestTransportSignalReceiverConfiguration { Parameter = 20, Parameter2 = 0 }),
        }));

        Assert.That(configurations2, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalWithTestTransport2), new TestTransport2SignalReceiverConfiguration { Parameter = 100 }),
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
            (typeof(TestSignalWithMultipleTestTransports), new TestTransportSignalReceiverConfiguration { Parameter = 10, Parameter2 = 0 }),
        }));

        Assert.That(configurations2, Is.EquivalentTo(new[]
        {
            (typeof(TestSignalWithMultipleTestTransports), new TestTransport2SignalReceiverConfiguration { Parameter = 20 }),
        }));
    }

    private sealed partial class TestSignalHandler : TestSignalWithTestTransport.IHandler, TestSignal2WithTestTransport.IHandler
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

    private sealed partial class MixedTestSignalHandler
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

    private sealed partial class TestSignalWithMultipleTransportsHandler : TestSignalWithMultipleTestTransports.IHandler
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

[TestTransportSignal]
public sealed partial record TestSignalWithTestTransport;

[TestTransportSignal(DefaultParameter = -2)]
public sealed partial record TestSignal2WithTestTransport;

[TestTransport2Signal]
public sealed partial record TestSignalWithTestTransport2;

[TestTransportSignal]
[TestTransport2Signal]
public sealed partial record TestSignalWithMultipleTestTransports;

file sealed class TestSignalTransportReceiverHost(IServiceProvider serviceProvider, ISignalTransportRegistry registry)
{
    public async Task<IReadOnlyCollection<(Type SignalType, TestTransportSignalReceiverConfiguration Configuration)>> Run(CancellationToken cancellationToken)
    {
        var invokers = registry.GetSignalInvokersForReceiver<ITestTransportSignalHandlerTypesInjector>();
        var result = new List<(Type SignalType, TestTransportSignalReceiverConfiguration Configuration)>();

        foreach (var invoker in invokers)
        {
            var configuration = await invoker.TypesInjector.Create(new Injectable(serviceProvider, cancellationToken));

            if (configuration is not null)
            {
                result.Add((invoker.SignalType, configuration));
            }
        }

        return result;
    }

    private sealed class Injectable(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        : ITestTransportSignalHandlerTypesInjectable<Task<TestTransportSignalReceiverConfiguration?>>
    {
        async Task<TestTransportSignalReceiverConfiguration?> ITestTransportSignalHandlerTypesInjectable<Task<TestTransportSignalReceiverConfiguration?>>
            .WithInjectedTypes<TSignal, TIHandler, THandler>()
        {
            var receiverBuilder = new TestTransportSignalReceiver<TSignal>(serviceProvider, cancellationToken);
            await THandler.ConfigureTestTransportReceiver(receiverBuilder);
            return receiverBuilder.Configuration;
        }
    }
}

file sealed class TestSignalTransport2ReceiverHost(IServiceProvider serviceProvider, ISignalTransportRegistry registry)
{
    public async Task<IReadOnlyCollection<(Type SignalType, TestTransport2SignalReceiverConfiguration Configuration)>> Run(CancellationToken cancellationToken)
    {
        var invokers = registry.GetSignalInvokersForReceiver<ITestTransport2SignalHandlerTypesInjector>();
        var result = new List<(Type SignalType, TestTransport2SignalReceiverConfiguration Configuration)>();

        foreach (var invoker in invokers)
        {
            var configuration = await invoker.TypesInjector.Create(new Injectable(serviceProvider, cancellationToken));

            if (configuration is not null)
            {
                result.Add((invoker.SignalType, configuration));
            }
        }

        return result;
    }

    private sealed class Injectable(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        : ITestTransport2TypesInjectable<Task<TestTransport2SignalReceiverConfiguration?>>
    {
        async Task<TestTransport2SignalReceiverConfiguration?> ITestTransport2TypesInjectable<Task<TestTransport2SignalReceiverConfiguration?>>
            .WithInjectedTypes<TSignal, TIHandler, THandler>()
        {
            var receiverBuilder = new TestTransport2SignalReceiver<TSignal>(serviceProvider, cancellationToken);
            await THandler.ConfigureTestTransport2Receiver(receiverBuilder);
            return receiverBuilder.Configuration;
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
public interface ITestTransportSignalHandler<TSignal, TIHandler>
    where TSignal : class, ITestTransportSignal<TSignal>
    where TIHandler : class, ITestTransportSignalHandler<TSignal, TIHandler>
{
    static virtual Task ConfigureTestTransportReceiver<T>(ITestTransportSignalReceiver<T> receiver)
        where T : class, ITestTransportSignal<T>
    {
        // by default, we don't configure the receiver
        return Task.CompletedTask;
    }

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static ISignalHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => TestTransportSignalHandlerTypesInjector<TSignal, TIHandler, THandler>.Default;
}

public interface ITestTransportSignalReceiver<TSignal>
    where TSignal : class, ITestTransportSignal<TSignal>
{
    IServiceProvider ServiceProvider { get; }

    CancellationToken CancellationToken { get; }

    ITestTransportSignalReceiver<TSignal> Enable(int? parameter = null);

    ITestTransportSignalReceiver<TSignal> WithParameter2(int parameter2);
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

file sealed class TestTransportSignalReceiver<T>(IServiceProvider serviceProvider, CancellationToken cancellationToken) : ITestTransportSignalReceiver<T>
    where T : class, ITestTransportSignal<T>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public TestTransportSignalReceiverConfiguration? Configuration { get; set; }

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
public interface ITestTransport2SignalHandler<TSignal, TIHandler>
    where TSignal : class, ITestTransport2Signal<TSignal>
    where TIHandler : class, ITestTransport2SignalHandler<TSignal, TIHandler>
{
    static virtual Task ConfigureTestTransport2Receiver<T>(ITestTransport2SignalReceiver<T> receiver)
        where T : class, ITestTransport2Signal<T>
    {
        // by default, we don't configure the receiver
        return Task.CompletedTask;
    }

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static ISignalHandlerTypesInjector CreateTestTransport2TypesInjector<THandler>()
        where THandler : class, TIHandler
        => TestTransport2SignalHandlerTypesInjector<TSignal, TIHandler, THandler>.Default;
}

public interface ITestTransport2SignalReceiver<TSignal>
    where TSignal : class, ITestTransport2Signal<TSignal>
{
    IServiceProvider ServiceProvider { get; }

    CancellationToken CancellationToken { get; }

    ITestTransport2SignalReceiver<TSignal> Enable(int parameter);
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

file sealed class TestTransport2SignalReceiver<T>(IServiceProvider serviceProvider, CancellationToken cancellationToken) : ITestTransport2SignalReceiver<T>
    where T : class, ITestTransport2Signal<T>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public TestTransport2SignalReceiverConfiguration? Configuration { get; set; }

    public ITestTransport2SignalReceiver<T> Enable(int parameter)
    {
        Configuration = new() { Parameter = parameter };
        return this;
    }
}

public sealed record TestTransport2SignalReceiverConfiguration
{
    public required int Parameter { get; set; }
}
