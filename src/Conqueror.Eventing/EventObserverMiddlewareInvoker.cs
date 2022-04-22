using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing
{
    internal sealed class EventObserverMiddlewareInvoker<TConfiguration> : IEventObserverMiddlewareInvoker
        where TConfiguration : EventObserverMiddlewareConfigurationAttribute
    {
        public async Task Invoke<TEvent>(TEvent evt,
                                         EventObserverMiddlewareNext<TEvent> next,
                                         EventObserverMetadata metadata,
                                         EventObserverMiddlewareConfigurationAttribute middlewareConfigurationAttribute,
                                         IServiceProvider serviceProvider,
                                         CancellationToken cancellationToken)
            where TEvent : class
        {
            var configurationAttribute = (TConfiguration)middlewareConfigurationAttribute;

            var ctx = new DefaultEventObserverMiddlewareContext<TEvent, TConfiguration>(evt, next, configurationAttribute, cancellationToken);

            var middleware = serviceProvider.GetRequiredService<EventObserverMiddlewareRegistry>().GetMiddleware<TConfiguration>(serviceProvider);

            await middleware.Execute(ctx);
        }
    }
}
