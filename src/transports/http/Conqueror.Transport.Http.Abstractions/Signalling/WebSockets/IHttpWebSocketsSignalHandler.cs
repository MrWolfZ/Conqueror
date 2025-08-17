namespace Conqueror;

public interface IHttpWebSocketsSignalHandler
{
    static abstract void ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver);
}

public interface IHttpWebSocketsSignalHandler<TSignal, TIHandler> : ISignalHandler<TSignal, TIHandler>
    where TSignal : class, IHttpWebSocketsSignal<TSignal>
    where TIHandler : class, IHttpWebSocketsSignalHandler<TSignal, TIHandler>
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static ISignalHandlerTypesInjector CreateHttpWebSocketsTypesInjector<THandler>()
        where THandler : class, TIHandler, IHttpWebSocketsSignalHandler =>
        new HttpWebSocketsSignalHandlerTypesInjector<TSignal, TIHandler>(THandler.ConfigureHttpWebSocketsReceiver);
}
