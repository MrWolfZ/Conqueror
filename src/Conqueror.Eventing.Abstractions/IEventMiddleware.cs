using System.Threading.Tasks;

namespace Conqueror;

public interface IEventMiddleware<TEvent>
    where TEvent : class
{
    /// <summary>
    ///     Execute the middleware with a given context.
    /// </summary>
    /// <param name="ctx">The context for the event execution.</param>
    Task Execute(EventMiddlewareContext<TEvent> ctx);
}
