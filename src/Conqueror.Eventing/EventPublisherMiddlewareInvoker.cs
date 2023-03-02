using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing;

internal sealed class EventPublisherMiddlewareInvoker : IEventPublisherMiddlewareInvoker
{
    private readonly Type middlewareType;

    public EventPublisherMiddlewareInvoker(Type middlewareType)
    {
        this.middlewareType = middlewareType;
    }

    public async Task Invoke<TEvent>(TEvent evt,
                                     EventPublisherMiddlewareNext<TEvent> next,
                                     IServiceProvider serviceProvider,
                                     CancellationToken cancellationToken)
        where TEvent : class
    {
        var middleware = (IEventPublisherMiddleware)serviceProvider.GetRequiredService(middlewareType);
        var ctx = new DefaultEventPublisherMiddlewareContext<TEvent>(evt, next, cancellationToken);
        await middleware.Execute(ctx).ConfigureAwait(false);
    }
}
