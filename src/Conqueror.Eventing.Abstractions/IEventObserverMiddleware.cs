using System.Threading.Tasks;

namespace Conqueror
{
    public interface IEventObserverMiddleware
    {
        Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class;
    }

    public interface IEventObserverMiddleware<TConfiguration>
    {
        Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TConfiguration> ctx)
            where TEvent : class;
    }
}
