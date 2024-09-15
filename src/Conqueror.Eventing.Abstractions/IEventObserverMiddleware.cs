using System.Threading.Tasks;

namespace Conqueror;

public interface IEventObserverMiddleware : IEventObserverMiddlewareMarker
{
    Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
        where TEvent : class;
}

public interface IEventObserverMiddleware<TConfiguration> : IEventObserverMiddlewareMarker
{
    Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TConfiguration> ctx)
        where TEvent : class;
}

public interface IEventObserverMiddlewareMarker;
