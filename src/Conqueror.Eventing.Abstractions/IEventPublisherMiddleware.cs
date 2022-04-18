using System.Threading.Tasks;

namespace Conqueror.Eventing
{
    public interface IEventPublisherMiddleware
    {
        Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class;
    }
}
