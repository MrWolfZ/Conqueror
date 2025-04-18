using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

// #pragma warning disable CA1000 // For this particular API it makes sense to have static methods on generic types
// #pragma warning disable CA1034 // we want to explicitly nest types to hide them from intellisense

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IEventNotificationHandler<in TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    internal static virtual IDefaultEventNotificationTypesInjector DefaultTypeInjector
        => TEventNotification.DefaultTypeInjector;

    Task Handle(TEventNotification notification, CancellationToken cancellationToken = default);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedEventNotificationHandler
{
    static virtual void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
        where T : class, IEventNotification<T>
    {
        // by default, we use an empty pipeline
    }

    static virtual Task ConfigureReceiver(IEventNotificationReceiver receiver)
    {
        // by default, we don't configure the receiver
        return Task.CompletedTask;
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IGeneratedEventNotificationHandler<in TEventNotification>
    : IEventNotificationHandler<TEventNotification>, IGeneratedEventNotificationHandler
    where TEventNotification : class, IEventNotification<TEventNotification>;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class GeneratedEventNotificationHandlerAdapter<TEventNotification> : IConfigurableEventNotificationHandler<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    internal IEventNotificationHandler<TEventNotification> Wrapped { get; init; } = null!;

    public Task Handle(TEventNotification notification, CancellationToken cancellationToken = default)
        => Wrapped.Handle(notification, cancellationToken);

    public IEventNotificationHandler<TEventNotification> WithPipeline(Action<IEventNotificationPipeline<TEventNotification>> configurePipeline)
        => Wrapped.WithPipeline(configurePipeline);

    public IEventNotificationHandler<TEventNotification> WithPublisher(ConfigureEventNotificationPublisher<TEventNotification> configurePublisher)
        => Wrapped.WithPublisher(configurePublisher);

    public IEventNotificationHandler<TEventNotification> WithPublisher(ConfigureEventNotificationPublisherAsync<TEventNotification> configurePublisher)
        => Wrapped.WithPublisher(configurePublisher);
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IConfigurableEventNotificationHandler<TEventNotification> : IEventNotificationHandler<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    IEventNotificationHandler<TEventNotification> WithPipeline(Action<IEventNotificationPipeline<TEventNotification>> configurePipeline);

    IEventNotificationHandler<TEventNotification> WithPublisher(ConfigureEventNotificationPublisher<TEventNotification> configurePublisher);

    IEventNotificationHandler<TEventNotification> WithPublisher(ConfigureEventNotificationPublisherAsync<TEventNotification> configurePublisher);
}
