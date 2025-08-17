namespace Conqueror;

public interface IHttpMessageSenderFactory
{
    IHttpMessageSender<TMessage, TResponse> Create<TMessage, TResponse>(Uri baseAddress)
        where TMessage : class, IHttpMessage<TMessage, TResponse>;
}
