using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing
{
    internal sealed class EventPublisher : IEventPublisher
    {
        private static readonly ConcurrentDictionary<Type, Func<EventPublisher, object, CancellationToken, Task>> PublishFunctions = new();

        private readonly IServiceProvider serviceProvider;

        public EventPublisher(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task PublishEvent(object evt, CancellationToken cancellationToken)
        {
            var publishFn = PublishFunctions.GetOrAdd(evt.GetType(), CreatePublishFunction);
            await publishFn(this, evt, cancellationToken).ConfigureAwait(false);
        }

        private async Task PublishEventGeneric<TEvent>(TEvent evt, CancellationToken cancellationToken)
            where TEvent : class
        {
            var observer = serviceProvider.GetService<IEventObserver<TEvent>>();

            if (observer is null)
            {
                return;
            }

            await observer.HandleEvent(evt, cancellationToken).ConfigureAwait(false);
        }

        private Func<EventPublisher, object, CancellationToken, Task> CreatePublishFunction(Type eventType)
        {
            var publisherParam = Expression.Parameter(typeof(EventPublisher));
            var eventParam = Expression.Parameter(typeof(object));
            var cancellationTokenParameterExpression = Expression.Parameter(typeof(CancellationToken));
            var castedEventParam = Expression.Convert(eventParam, eventType);
            var callExpr = Expression.Call(publisherParam, nameof(PublishEventGeneric), new[] { eventType }, castedEventParam, cancellationTokenParameterExpression);
            var lambda = Expression.Lambda(callExpr, publisherParam, eventParam, cancellationTokenParameterExpression);
            var compiled = lambda.Compile();
            return (Func<EventPublisher, object, CancellationToken, Task>)compiled;
        }
    }
}
