using System.ComponentModel;
using System.Diagnostics;

namespace Conqueror.Tests.Eventing;

public sealed class EventNotificationReceiverTests
{
    [Test]
    public async Task GivenHandlerWithReceiverConfiguration_WhenRunningReceiver_ReceiverGetsConfiguredCorrectly()
    {
        var services = new ServiceCollection();
        var testObservations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton<TestEventNotificationTransportReceiverHost>()
                    .AddSingleton(testObservations);

        var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<TestEventNotificationTransportReceiverHost>();

        // TODO: actually invoke the invokers and assert that events are received
        var configurations = await receiver.Run(CancellationToken.None);

        Assert.That(configurations, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationWithTestTransport), new() { Parameter = 10, Parameter2 = 1 }),
            (typeof(TestEventNotification2WithTestTransport), new TestEventNotificationTransportReceiverConfiguration { Parameter = 20, Parameter2 = 2 }),
        }));
    }

    [Test]
    public async Task GivenHandlerWithReceiverConfigurationForMixedTransports_WhenRunningReceiver_ReceiverGetsCorrectConfigurationForTransport()
    {
        var services = new ServiceCollection();
        var testObservations = new TestObservations();

        _ = services.AddEventNotificationHandler<MixedTestEventNotificationHandler>()
                    .AddSingleton<TestEventNotificationTransportReceiverHost>()
                    .AddSingleton<TestEventNotificationTransport2ReceiverHost>()
                    .AddSingleton(testObservations);

        var provider = services.BuildServiceProvider();

        var receiver1 = provider.GetRequiredService<TestEventNotificationTransportReceiverHost>();
        var receiver2 = provider.GetRequiredService<TestEventNotificationTransport2ReceiverHost>();

        var configurations1 = await receiver1.Run(CancellationToken.None);
        var configurations2 = await receiver2.Run(CancellationToken.None);

        Assert.That(configurations1, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationWithTestTransport), new() { Parameter = 10, Parameter2 = 0 }),
            (typeof(TestEventNotification2WithTestTransport), new TestEventNotificationTransportReceiverConfiguration { Parameter = 20, Parameter2 = 0 }),
        }));

        Assert.That(configurations2, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationWithTestTransport2), new TestEventNotificationTransport2ReceiverConfiguration { Parameter = 100 }),
        }));
    }

    [Test]
    public async Task GivenHandlerWithReceiverConfigurationForEventNotificationTypeWithMultipleTransports_WhenRunningReceiver_ReceiverGetsCorrectConfigurationForTransport()
    {
        var services = new ServiceCollection();
        var testObservations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationWithMultipleTransportsHandler>()
                    .AddSingleton<TestEventNotificationTransportReceiverHost>()
                    .AddSingleton<TestEventNotificationTransport2ReceiverHost>()
                    .AddSingleton(testObservations);

        var provider = services.BuildServiceProvider();

        var receiver1 = provider.GetRequiredService<TestEventNotificationTransportReceiverHost>();
        var receiver2 = provider.GetRequiredService<TestEventNotificationTransport2ReceiverHost>();

        var configurations1 = await receiver1.Run(CancellationToken.None);
        var configurations2 = await receiver2.Run(CancellationToken.None);

        Assert.That(configurations1, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationWithMultipleTestTransports), new TestEventNotificationTransportReceiverConfiguration { Parameter = 10, Parameter2 = 0 }),
        }));

        Assert.That(configurations2, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationWithMultipleTestTransports), new TestEventNotificationTransport2ReceiverConfiguration { Parameter = 20 }),
        }));
    }

    private sealed class TestEventNotificationHandler : TestEventNotificationWithTestTransport.IHandler, TestEventNotification2WithTestTransport.IHandler
    {
        public Task Handle(TestEventNotificationWithTestTransport notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestEventNotification2WithTestTransport notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public static Task ConfigureTestTransportReceiver<T>(ITestTransportEventNotificationReceiver<T> receiver)
            where T : class, ITestTransportEventNotification<T>
        {
            var testObservations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            testObservations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(10).WithParameter2(1);

            if (typeof(T) == typeof(TestEventNotification2WithTestTransport))
            {
                _ = receiver.Enable(20).WithParameter2(2);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class MixedTestEventNotificationHandler
        : TestEventNotificationWithTestTransport.IHandler,
          TestEventNotification2WithTestTransport.IHandler,
          TestEventNotificationWithTestTransport2.IHandler
    {
        public Task Handle(TestEventNotificationWithTestTransport notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestEventNotification2WithTestTransport notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestEventNotificationWithTestTransport2 notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public static Task ConfigureTestTransportReceiver<T>(ITestTransportEventNotificationReceiver<T> receiver)
            where T : class, ITestTransportEventNotification<T>
        {
            var testObservations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            testObservations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(10);

            if (typeof(T) == typeof(TestEventNotification2WithTestTransport))
            {
                _ = receiver.Enable(20);
            }

            return Task.CompletedTask;
        }

        public static Task ConfigureTestTransport2Receiver<T>(ITestTransport2EventNotificationReceiver<T> receiver)
            where T : class, ITestTransport2EventNotification<T>
        {
            var testObservations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            testObservations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(100);
            return Task.CompletedTask;
        }
    }

    private sealed class TestEventNotificationWithMultipleTransportsHandler : TestEventNotificationWithMultipleTestTransports.IHandler
    {
        public Task Handle(TestEventNotificationWithMultipleTestTransports notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public static Task ConfigureTestTransportReceiver<T>(ITestTransportEventNotificationReceiver<T> receiver)
            where T : class, ITestTransportEventNotification<T>
        {
            var testObservations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            testObservations.ConfigureReceiverCallCount += 1;

            _ = receiver.Enable(10);

            return Task.CompletedTask;
        }

        public static Task ConfigureTestTransport2Receiver<T>(ITestTransport2EventNotificationReceiver<T> receiver)
            where T : class, ITestTransport2EventNotification<T>
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

[EventNotification]
public sealed partial record TestEventNotificationWithTestTransport : ITestTransportEventNotification<TestEventNotificationWithTestTransport>
{
    static ITestTransportTypesInjector ITestTransportEventNotification.TestTransportTypesInjector
        => TestTransportTypesInjector<TestEventNotificationWithTestTransport>.Default;

    public partial interface IHandler : IGeneratedTestTransportEventNotificationHandler;
}

[EventNotification]
public sealed partial record TestEventNotification2WithTestTransport : ITestTransportEventNotification<TestEventNotification2WithTestTransport>
{
    static ITestTransportTypesInjector ITestTransportEventNotification.TestTransportTypesInjector
        => TestTransportTypesInjector<TestEventNotification2WithTestTransport>.Default;

    public partial interface IHandler : IGeneratedTestTransportEventNotificationHandler;
}

[EventNotification]
public sealed partial record TestEventNotificationWithTestTransport2 : ITestTransport2EventNotification<TestEventNotificationWithTestTransport2>
{
    static ITestTransport2TypesInjector ITestTransport2EventNotification.TestTransport2TypesInjector
        => TestTransport2TypesInjector<TestEventNotificationWithTestTransport2>.Default;

    public partial interface IHandler : IGeneratedTestTransport2EventNotificationHandler;
}

[EventNotification]
public sealed partial record TestEventNotificationWithMultipleTestTransports : ITestTransportEventNotification<TestEventNotificationWithMultipleTestTransports>,
                                                                               ITestTransport2EventNotification<TestEventNotificationWithMultipleTestTransports>
{
    static ITestTransportTypesInjector ITestTransportEventNotification.TestTransportTypesInjector
        => TestTransportTypesInjector<TestEventNotificationWithMultipleTestTransports>.Default;

    static ITestTransport2TypesInjector ITestTransport2EventNotification.TestTransport2TypesInjector
        => TestTransport2TypesInjector<TestEventNotificationWithMultipleTestTransports>.Default;

    public partial interface IHandler : IGeneratedTestTransportEventNotificationHandler;

    public partial interface IHandler : IGeneratedTestTransport2EventNotificationHandler;
}

file sealed class TestEventNotificationTransportReceiverHost(IServiceProvider serviceProvider, IEventNotificationTransportRegistry registry)
{
    public async Task<IReadOnlyCollection<(Type NotificationType, TestEventNotificationTransportReceiverConfiguration Configuration)>> Run(CancellationToken cancellationToken)
    {
        var invokers = registry.GetEventNotificationInvokersForReceiver<ITestTransportTypesInjector>();
        var result = new List<(Type NotificationType, TestEventNotificationTransportReceiverConfiguration Configuration)>();

        foreach (var invoker in invokers)
        {
            var configuration = await invoker.TypesInjector.CreateForTestTransport(new Injectable(serviceProvider, cancellationToken));

            if (configuration is not null)
            {
                result.Add((invoker.EventNotificationType, configuration));
            }
        }

        return result;
    }

    private sealed class Injectable(IServiceProvider serviceProvider, CancellationToken cancellationToken) : ITestTransportTypesInjectable<Task<TestEventNotificationTransportReceiverConfiguration?>>
    {
        async Task<TestEventNotificationTransportReceiverConfiguration?> ITestTransportTypesInjectable<Task<TestEventNotificationTransportReceiverConfiguration?>>
            .WithInjectedTypes<TEventNotification, THandler>()
        {
            var receiverBuilder = new TestTransportEventNotificationReceiver<TEventNotification>(serviceProvider, cancellationToken);
            await THandler.ConfigureTestTransportReceiver(receiverBuilder);
            return receiverBuilder.Configuration;
        }
    }
}

file sealed class TestEventNotificationTransport2ReceiverHost(IServiceProvider serviceProvider, IEventNotificationTransportRegistry registry)
{
    public async Task<IReadOnlyCollection<(Type NotificationType, TestEventNotificationTransport2ReceiverConfiguration Configuration)>> Run(CancellationToken cancellationToken)
    {
        var invokers = registry.GetEventNotificationInvokersForReceiver<ITestTransport2TypesInjector>();
        var result = new List<(Type NotificationType, TestEventNotificationTransport2ReceiverConfiguration Configuration)>();

        foreach (var invoker in invokers)
        {
            var configuration = await invoker.TypesInjector.CreateForTestTransport2(new Injectable(serviceProvider, cancellationToken));

            if (configuration is not null)
            {
                result.Add((invoker.EventNotificationType, configuration));
            }
        }

        return result;
    }

    private sealed class Injectable(IServiceProvider serviceProvider, CancellationToken cancellationToken) : ITestTransport2TypesInjectable<Task<TestEventNotificationTransport2ReceiverConfiguration?>>
    {
        async Task<TestEventNotificationTransport2ReceiverConfiguration?> ITestTransport2TypesInjectable<Task<TestEventNotificationTransport2ReceiverConfiguration?>>
            .WithInjectedTypes<TEventNotification, THandler>()
        {
            var receiverBuilder = new TestTransport2EventNotificationReceiver<TEventNotification>(serviceProvider, cancellationToken);
            await THandler.ConfigureTestTransport2Receiver(receiverBuilder);
            return receiverBuilder.Configuration;
        }
    }
}

public interface ITestTransportEventNotification
{
    // must be virtual instead of abstract so that it can be used as a type parameter / constraint
    // ReSharper disable once UnusedMember.Global (used by reflection)
    static virtual ITestTransportTypesInjector TestTransportTypesInjector
        => throw new NotSupportedException("this method should always be implemented by the generic interface");
}

public interface ITestTransportEventNotification<T> : IEventNotification<T>, ITestTransportEventNotification
    where T : class, ITestTransportEventNotification<T>
{
    [SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "testing")]
    static virtual int DefaultParameter { get; } = -1;
}

public interface ITestTransportEventNotificationReceiver<TEventNotification>
    where TEventNotification : class, ITestTransportEventNotification<TEventNotification>
{
    IServiceProvider ServiceProvider { get; }

    CancellationToken CancellationToken { get; }

    ITestTransportEventNotificationReceiver<TEventNotification> Enable(int? parameter = null);

    ITestTransportEventNotificationReceiver<TEventNotification> WithParameter2(int parameter2);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedTestTransportEventNotificationHandler
{
    static virtual Task ConfigureTestTransportReceiver<T>(ITestTransportEventNotificationReceiver<T> receiver)
        where T : class, ITestTransportEventNotification<T>
    {
        // by default, we don't configure the receiver
        return Task.CompletedTask;
    }
}

public interface ITestTransportTypesInjector : IEventNotificationTypesInjector
{
    TResult CreateForTestTransport<TResult>(ITestTransportTypesInjectable<TResult> injectable);
}

file sealed class TestTransportTypesInjector<TEventNotification> : ITestTransportTypesInjector
    where TEventNotification : class, ITestTransportEventNotification<TEventNotification>
{
    public static readonly TestTransportTypesInjector<TEventNotification> Default = new();

    IEventNotificationTypesInjector IEventNotificationTypesInjector.WithHandlerType<THandler>()
    {
        Debug.Assert(typeof(THandler).IsAssignableTo(typeof(IEventNotificationHandler<TEventNotification>)), $"expected handler type '{typeof(THandler)}' to be assignable to '{typeof(IEventNotificationHandler<TEventNotification>)}'");

        return Activator.CreateInstance(typeof(WithHandlerType<>)
                                            .MakeGenericType(typeof(TEventNotification), typeof(THandler)))
                   as IEventNotificationTypesInjector
               ?? throw new InvalidOperationException("cannot create instance of WithHandlerType<THandler>");
    }

    public TResult CreateForTestTransport<TResult>(ITestTransportTypesInjectable<TResult> injectable)
        => throw new NotSupportedException($"handler type must be set via '{nameof(IEventNotificationTypesInjector.WithHandlerType)}'");

    private sealed class WithHandlerType<THandler> : ITestTransportTypesInjector
        where THandler : class, IEventNotificationHandler<TEventNotification>, IGeneratedTestTransportEventNotificationHandler
    {
        public TResult CreateForTestTransport<TResult>(ITestTransportTypesInjectable<TResult> injectable)
            => injectable.WithInjectedTypes<TEventNotification, THandler>();

        IEventNotificationTypesInjector IEventNotificationTypesInjector.WithHandlerType<T>()
            => throw new NotSupportedException("cannot set handler type multiple times for types injector");
    }
}

public interface ITestTransportTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TEventNotification, THandler>()
        where TEventNotification : class, ITestTransportEventNotification<TEventNotification>
        where THandler : class, IEventNotificationHandler<TEventNotification>, IGeneratedTestTransportEventNotificationHandler;
}

file sealed class TestTransportEventNotificationReceiver<T>(IServiceProvider serviceProvider, CancellationToken cancellationToken) : ITestTransportEventNotificationReceiver<T>
    where T : class, ITestTransportEventNotification<T>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public TestEventNotificationTransportReceiverConfiguration? Configuration { get; set; }

    public ITestTransportEventNotificationReceiver<T> Enable(int? parameter = null)
    {
        Configuration = new() { Parameter = parameter ?? T.DefaultParameter };
        return this;
    }

    public ITestTransportEventNotificationReceiver<T> WithParameter2(int parameter2)
    {
        if (Configuration is null)
        {
            throw new InvalidOperationException("cannot set parameter 2 on receiver that has not been configured yet");
        }

        Configuration.Parameter2 = parameter2;
        return this;
    }
}

public sealed record TestEventNotificationTransportReceiverConfiguration
{
    public required int Parameter { get; set; }

    public int Parameter2 { get; set; }
}

public interface ITestTransport2EventNotification
{
    // must be virtual instead of abstract so that it can be used as a type parameter / constraint
    // ReSharper disable once UnusedMember.Global (used by reflection)
    static virtual ITestTransport2TypesInjector TestTransport2TypesInjector
        => throw new NotSupportedException("this method should always be implemented by the generic interface");
}

public interface ITestTransport2EventNotification<T> : IEventNotification<T>, ITestTransport2EventNotification
    where T : class, ITestTransport2EventNotification<T>;

public interface ITestTransport2EventNotificationReceiver<TEventNotification>
    where TEventNotification : class, ITestTransport2EventNotification<TEventNotification>
{
    IServiceProvider ServiceProvider { get; }

    CancellationToken CancellationToken { get; }

    ITestTransport2EventNotificationReceiver<TEventNotification> Enable(int parameter);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedTestTransport2EventNotificationHandler
{
    static virtual Task ConfigureTestTransport2Receiver<T>(ITestTransport2EventNotificationReceiver<T> receiver)
        where T : class, ITestTransport2EventNotification<T>
    {
        // by default, we don't configure the receiver
        return Task.CompletedTask;
    }
}

public interface ITestTransport2TypesInjector : IEventNotificationTypesInjector
{
    TResult CreateForTestTransport2<TResult>(ITestTransport2TypesInjectable<TResult> injectable);
}

file sealed class TestTransport2TypesInjector<TEventNotification> : ITestTransport2TypesInjector
    where TEventNotification : class, ITestTransport2EventNotification<TEventNotification>
{
    public static readonly TestTransport2TypesInjector<TEventNotification> Default = new();

    IEventNotificationTypesInjector IEventNotificationTypesInjector.WithHandlerType<THandler>()
    {
        Debug.Assert(typeof(THandler).IsAssignableTo(typeof(IEventNotificationHandler<TEventNotification>)), $"expected handler type '{typeof(THandler)}' to be assignable to '{typeof(IEventNotificationHandler<TEventNotification>)}'");

        return Activator.CreateInstance(typeof(WithHandlerType<>)
                                            .MakeGenericType(typeof(TEventNotification), typeof(THandler)))
                   as IEventNotificationTypesInjector
               ?? throw new InvalidOperationException("cannot create instance of WithHandlerType<THandler>");
    }

    public TResult CreateForTestTransport2<TResult>(ITestTransport2TypesInjectable<TResult> injectable)
        => throw new NotSupportedException($"handler type must be set via '{nameof(IEventNotificationTypesInjector.WithHandlerType)}'");

    private sealed class WithHandlerType<THandler> : ITestTransport2TypesInjector
        where THandler : class, IEventNotificationHandler<TEventNotification>, IGeneratedTestTransport2EventNotificationHandler
    {
        public TResult CreateForTestTransport2<TResult>(ITestTransport2TypesInjectable<TResult> injectable)
            => injectable.WithInjectedTypes<TEventNotification, THandler>();

        IEventNotificationTypesInjector IEventNotificationTypesInjector.WithHandlerType<T>()
            => throw new NotSupportedException("cannot set handler type multiple times for types injector");
    }
}

public interface ITestTransport2TypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TEventNotification, THandler>()
        where TEventNotification : class, ITestTransport2EventNotification<TEventNotification>
        where THandler : class, IEventNotificationHandler<TEventNotification>, IGeneratedTestTransport2EventNotificationHandler;
}

file sealed class TestTransport2EventNotificationReceiver<T>(IServiceProvider serviceProvider, CancellationToken cancellationToken) : ITestTransport2EventNotificationReceiver<T>
    where T : class, ITestTransport2EventNotification<T>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public TestEventNotificationTransport2ReceiverConfiguration? Configuration { get; set; }

    public ITestTransport2EventNotificationReceiver<T> Enable(int parameter)
    {
        Configuration = new() { Parameter = parameter };
        return this;
    }
}

public sealed record TestEventNotificationTransport2ReceiverConfiguration
{
    public required int Parameter { get; set; }
}
