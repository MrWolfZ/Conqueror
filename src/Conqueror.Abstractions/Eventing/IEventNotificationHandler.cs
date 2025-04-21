using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

// #pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types
// #pragma warning disable CA1034 // we want to explicitly nest types to hide them from intellisense

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IEventNotificationHandler<in TEventNotification, THandler>
    where TEventNotification : class, IEventNotification<TEventNotification>
    where THandler : class, IEventNotificationHandler<TEventNotification, THandler>;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedEventNotificationHandler
{
    static virtual void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
        where T : class, IEventNotification<T>
    {
        // by default, we use an empty pipeline
    }

    static virtual void ConfigureInProcessReceiver<T>(IInProcessEventNotificationReceiver<T> receiver)
        where T : class, IEventNotification<T>
    {
        // by default, we enable the in-process receiver for all event notification types
        receiver.Enable();
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedEventNotificationHandler<in TEventNotification, THandler>
    : IEventNotificationHandler<TEventNotification, THandler>, IGeneratedEventNotificationHandler
    where TEventNotification : class, IEventNotification<TEventNotification>
    where THandler : class, IEventNotificationHandler<TEventNotification, THandler>
{
    static abstract Task Invoke(THandler handler, TEventNotification notification, CancellationToken cancellationToken);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class GeneratedEventNotificationHandlerAdapter<TEventNotification, THandler, TAdapter> : IConfigurableEventNotificationHandler<TEventNotification, THandler>
    where TEventNotification : class, IEventNotification<TEventNotification>
    where THandler : class, IEventNotificationHandler<TEventNotification, THandler>
    where TAdapter : GeneratedEventNotificationHandlerAdapter<TEventNotification, THandler, TAdapter>, THandler, new()
{
    internal IEventNotificationDispatcher<TEventNotification> Dispatcher { get; init; } = null!;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Task Handle(TEventNotification notification, CancellationToken cancellationToken = default)
        => Dispatcher.Dispatch(notification, cancellationToken);

    public THandler WithPipeline(Action<IEventNotificationPipeline<TEventNotification>> configurePipeline)
        => new TAdapter { Dispatcher = Dispatcher.WithPipeline(configurePipeline) };

    public THandler WithPublisher(ConfigureEventNotificationPublisher<TEventNotification> configurePublisher)
        => new TAdapter { Dispatcher = Dispatcher.WithPublisher(configurePublisher) };

    public THandler WithPublisher(ConfigureEventNotificationPublisherAsync<TEventNotification> configurePublisher)
        => new TAdapter { Dispatcher = Dispatcher.WithPublisher(configurePublisher) };
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IConfigurableEventNotificationHandler<TEventNotification, THandler> : IEventNotificationHandler<TEventNotification, THandler>
    where TEventNotification : class, IEventNotification<TEventNotification>
    where THandler : class, IEventNotificationHandler<TEventNotification, THandler>
{
    THandler WithPipeline(Action<IEventNotificationPipeline<TEventNotification>> configurePipeline);

    THandler WithPublisher(ConfigureEventNotificationPublisher<TEventNotification> configurePublisher);

    THandler WithPublisher(ConfigureEventNotificationPublisherAsync<TEventNotification> configurePublisher);
}
