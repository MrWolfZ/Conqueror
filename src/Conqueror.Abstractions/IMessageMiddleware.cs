using System.Threading.Tasks;

namespace Conqueror;

public interface IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx);
}
