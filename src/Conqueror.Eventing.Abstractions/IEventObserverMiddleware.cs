using System.Threading.Tasks;

// empty interface used as marker interface for other operations
#pragma warning disable CA1040

namespace Conqueror.Eventing
{
    public interface IEventObserverMiddleware
    {
    }

    public interface IEventObserverMiddleware<TConfiguration> : IEventObserverMiddleware
        where TConfiguration : EventObserverMiddlewareConfigurationAttribute
    {
        Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TConfiguration> ctx)
            where TEvent : class;
    }
}
