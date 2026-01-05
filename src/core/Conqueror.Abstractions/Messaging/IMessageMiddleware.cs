namespace Conqueror;

public interface IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx);
}
