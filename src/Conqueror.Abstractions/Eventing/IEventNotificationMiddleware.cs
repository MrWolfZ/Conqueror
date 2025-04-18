using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IEventNotificationMiddleware<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    Task Execute(EventNotificationMiddlewareContext<TEventNotification> ctx);
}
