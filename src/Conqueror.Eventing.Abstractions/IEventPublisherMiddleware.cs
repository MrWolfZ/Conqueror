using System.Threading.Tasks;

namespace Conqueror;

public interface IEventPublisherMiddleware : IEventPublisherMiddlewareMarker
{
    Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
        where TEvent : class;
}

public interface IEventPublisherMiddleware<TConfiguration> : IEventPublisherMiddlewareMarker
{
    Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent, TConfiguration> ctx)
        where TEvent : class;
}

public interface IEventPublisherMiddlewareMarker
{
}
