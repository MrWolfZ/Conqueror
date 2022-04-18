using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing
{
    internal sealed class EventObserverMiddlewareInvoker<TConfiguration> : IEventObserverMiddlewareInvoker
        where TConfiguration : EventObserverMiddlewareConfigurationAttribute, IEventObserverMiddlewareConfiguration<IEventObserverMiddleware<TConfiguration>>
    {
        private static readonly Type MiddlewareType = typeof(TConfiguration).GetInterfaces()
                                                                            .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventObserverMiddlewareConfiguration<>))
                                                                            .GetGenericArguments()
                                                                            .Single();

        public async Task Invoke<TEvent>(TEvent evt,
                                         EventObserverMiddlewareNext<TEvent> next,
                                         EventObserverMetadata metadata,
                                         IServiceProvider serviceProvider,
                                         CancellationToken cancellationToken)
            where TEvent : class
        {
            if (!metadata.TryGetMiddlewareConfiguration<TEvent, TConfiguration>(out var configurationAttribute))
            {
                await next(evt, cancellationToken);
                return;
            }

            var ctx = new DefaultEventObserverMiddlewareContext<TEvent, TConfiguration>(evt, next, configurationAttribute, cancellationToken);

            var middleware = serviceProvider.GetService(MiddlewareType);

            if (middleware is null)
            {
                throw new InvalidOperationException($"event observer middleware ${MiddlewareType.Name} is not registered, but is being used on observer ${metadata.ObserverType.Name}");
            }

            await ((IEventObserverMiddleware<TConfiguration>)middleware).Execute(ctx);
        }
    }
}
