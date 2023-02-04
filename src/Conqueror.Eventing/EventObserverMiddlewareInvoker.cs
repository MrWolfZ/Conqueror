using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing
{
    internal sealed class EventObserverMiddlewareInvoker<TConfiguration> : IEventObserverMiddlewareInvoker
    {
        public async Task Invoke<TEvent>(TEvent evt,
                                         EventObserverMiddlewareNext<TEvent> next,
                                         Type middlewareType,
                                         object? middlewareConfiguration,
                                         IServiceProvider serviceProvider,
                                         CancellationToken cancellationToken)
            where TEvent : class
        {
            if (middlewareConfiguration is null)
            {
                throw new ArgumentNullException(nameof(middlewareConfiguration));
            }

            var configuration = (TConfiguration)middlewareConfiguration;

            var ctx = new DefaultEventObserverMiddlewareContext<TEvent, TConfiguration>(evt, next, configuration, cancellationToken);

            var middleware = (IEventObserverMiddleware<TConfiguration>)serviceProvider.GetRequiredService(middlewareType);

            await middleware.Execute(ctx).ConfigureAwait(false);
        }
    }

    internal sealed class EventObserverMiddlewareInvoker : IEventObserverMiddlewareInvoker
    {
        public async Task Invoke<TEvent>(TEvent evt,
                                         EventObserverMiddlewareNext<TEvent> next,
                                         Type middlewareType,
                                         object? middlewareConfiguration,
                                         IServiceProvider serviceProvider,
                                         CancellationToken cancellationToken)
            where TEvent : class
        {
            var ctx = new DefaultEventObserverMiddlewareContext<TEvent>(evt, next, cancellationToken);

            var middleware = (IEventObserverMiddleware)serviceProvider.GetRequiredService(middlewareType);

            await middleware.Execute(ctx).ConfigureAwait(false);
        }
    }
}
