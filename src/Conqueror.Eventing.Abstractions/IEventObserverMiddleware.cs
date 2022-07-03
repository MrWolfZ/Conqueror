using System.Threading.Tasks;

// empty interface used as marker interface for other operations
#pragma warning disable CA1040

namespace Conqueror.Eventing
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
