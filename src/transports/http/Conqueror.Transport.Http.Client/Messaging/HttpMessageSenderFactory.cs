namespace Conqueror.Transport.Http.Client.Messaging;

internal sealed class HttpMessageSenderFactory : IHttpMessageSenderFactory
{
    public IHttpMessageSender<TMessage, TResponse> Create<TMessage, TResponse>(Uri baseAddress)
        where TMessage : class, IHttpMessage<TMessage, TResponse> =>
        new HttpMessageSender<TMessage, TResponse>(baseAddress);
}
