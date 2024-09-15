using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal delegate Task EventObserverMiddlewareNext<in TEvent>(TEvent evt, CancellationToken cancellationToken);

internal sealed class DefaultEventObserverMiddlewareContext<TEvent, TConfiguration>(
    TEvent evt,
    Type observedEventType,
    EventObserverMiddlewareNext<TEvent> next,
    TConfiguration configuration,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken)
    : EventObserverMiddlewareContext<TEvent, TConfiguration>
    where TEvent : class
{
    public override TEvent Event { get; } = evt;

    public override Type ObservedEventType { get; } = observedEventType;

    public override CancellationToken CancellationToken { get; } = cancellationToken;

    public override TConfiguration Configuration { get; } = configuration;

    public override IServiceProvider ServiceProvider { get; } = serviceProvider;

    public override IConquerorContext ConquerorContext => throw new NotImplementedException();

    public override Task Next(TEvent evt, CancellationToken cancellationToken) => next(evt, cancellationToken);
}
