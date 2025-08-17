namespace Conqueror;

public interface IHttpMessageHandler
{
    static virtual void ConfigureHttpReceiver(IHttpMessageReceiver receiver)
    {
        // we don't configure the receiver (by default, it is enabled for all message types)
    }
}

public interface IHttpMessageHandler<TMessage, TResponse, TIHandler> : IMessageHandler<TMessage, TResponse, TIHandler>
    where TMessage : class, IHttpMessage<TMessage, TResponse>
    where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static IMessageHandlerTypesInjector CreateHttpTypesInjector<THandler>()
        where THandler : class, TIHandler, IHttpMessageHandler =>
        new HttpMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(THandler.ConfigureHttpReceiver);
}
