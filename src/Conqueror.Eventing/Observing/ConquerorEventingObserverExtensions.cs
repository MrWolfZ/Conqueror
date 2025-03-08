using System;
using Conqueror.Eventing.Publishing;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible without an extra import)
namespace Conqueror;

public static class ConquerorEventingObserverExtensions
{
    public static IEventObserver<TEvent> WithPipeline<TEvent>(this IEventObserver<TEvent> observer,
                                                              Action<IEventPipeline<TEvent>> configurePipeline)
        where TEvent : class
    {
        if (observer is EventObserverDispatcher<TEvent> dispatcher)
        {
            return dispatcher.WithPipeline(configurePipeline);
        }

        throw new NotSupportedException($"observer type '{observer.GetType()}' is not supported");
    }
}
