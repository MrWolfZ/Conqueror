using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal delegate Task EventPublisherMiddlewareNext<in TEvent>(TEvent evt, CancellationToken cancellationToken);

internal sealed class DefaultEventPublisherMiddlewareContext<TEvent, TConfiguration> : EventPublisherMiddlewareContext<TEvent, TConfiguration>
    where TEvent : class
{
    private readonly EventPublisherMiddlewareNext<TEvent> next;

    public DefaultEventPublisherMiddlewareContext(TEvent evt,
                                                  EventPublisherMiddlewareNext<TEvent> next,
                                                  TConfiguration configuration,
                                                  IServiceProvider serviceProvider,
                                                  CancellationToken cancellationToken)
    {
        this.next = next;
        Event = evt;
        CancellationToken = cancellationToken;
        Configuration = configuration;
        ServiceProvider = serviceProvider;
    }

    public override TEvent Event { get; }

    public override Type PublisherType => throw new NotImplementedException();

    public override string PublisherName => throw new NotImplementedException();

    public override CancellationToken CancellationToken { get; }

    public override TConfiguration Configuration { get; }

    public override IServiceProvider ServiceProvider { get; }

    public override IConquerorContext ConquerorContext => throw new NotImplementedException();

    public override Task Next(TEvent evt, CancellationToken cancellationToken) => next(evt, cancellationToken);
}
