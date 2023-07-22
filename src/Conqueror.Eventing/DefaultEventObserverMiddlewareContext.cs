using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal delegate Task EventObserverMiddlewareNext<in TEvent>(TEvent evt, CancellationToken cancellationToken);

internal sealed class DefaultEventObserverMiddlewareContext<TEvent, TConfiguration> : EventObserverMiddlewareContext<TEvent, TConfiguration>
    where TEvent : class
{
    private readonly EventObserverMiddlewareNext<TEvent> next;

    public DefaultEventObserverMiddlewareContext(TEvent evt,
                                                 Type observedEventType,
                                                 EventObserverMiddlewareNext<TEvent> next,
                                                 TConfiguration configuration,
                                                 IServiceProvider serviceProvider,
                                                 CancellationToken cancellationToken)
    {
        this.next = next;
        Event = evt;
        ObservedEventType = observedEventType;
        CancellationToken = cancellationToken;
        ServiceProvider = serviceProvider;
        Configuration = configuration;
    }

    public override TEvent Event { get; }

    public override Type ObservedEventType { get; }

    public override CancellationToken CancellationToken { get; }

    public override TConfiguration Configuration { get; }

    public override IServiceProvider ServiceProvider { get; }

    public override IConquerorContext ConquerorContext => throw new NotImplementedException();

    public override Task Next(TEvent evt, CancellationToken cancellationToken) => next(evt, cancellationToken);
}
