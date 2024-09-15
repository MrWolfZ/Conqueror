using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal delegate Task EventPublisherMiddlewareNext<in TEvent>(TEvent evt, CancellationToken cancellationToken);

internal sealed class DefaultEventPublisherMiddlewareContext<TEvent, TConfiguration>(
    TEvent evt,
    EventPublisherMiddlewareNext<TEvent> next,
    TConfiguration configuration,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken)
    : EventPublisherMiddlewareContext<TEvent, TConfiguration>
    where TEvent : class
{
    public override TEvent Event { get; } = evt;

    public override Type PublisherType => throw new NotImplementedException();

    public override string PublisherName => throw new NotImplementedException();

    public override CancellationToken CancellationToken { get; } = cancellationToken;

    public override TConfiguration Configuration { get; } = configuration;

    public override IServiceProvider ServiceProvider { get; } = serviceProvider;

    public override IConquerorContext ConquerorContext => throw new NotImplementedException();

    public override Task Next(TEvent evt, CancellationToken cancellationToken) => next(evt, cancellationToken);
}
