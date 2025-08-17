namespace Conqueror.Transport.Http.Client.Signalling.WebSockets;

internal sealed class HttpWebSocketsSignalReceiver(
    IServiceProvider serviceProvider,
    IReadOnlyCollection<Type> signalTypes,
    Type? handlerType
) : IHttpWebSocketsSignalReceiver
{
    private readonly List<ISignalReceiverHandlerInvoker> invokers = [];
    private readonly ConcurrentDictionary<Type, List<ISignalReceiverHandlerInvoker>> invokersBySignalType = [];
    private readonly Dictionary<string, Func<Stream, CancellationToken, ValueTask<object>>> parserByTag = [];
    private readonly Dictionary<string, Type> signalTypeByEventType = [];

    public HttpWebSocketsSignalReceiverConfiguration? Configuration { get; private set; }

    public IReadOnlyCollection<string> Tags => parserByTag.Keys;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public IReadOnlyCollection<Type> SignalTypes { get; } = signalTypes;

    public Type? HandlerType { get; } = handlerType;

    public bool IsEnabled => Configuration is not null;

    public void Disable() => Configuration = null;

    public HttpWebSocketsSignalReceiverConfiguration Enable(Uri address)
    {
        Configuration = new HttpWebSocketsSignalReceiverConfiguration { Address = address };

        return Configuration;
    }

    public void AddSignalType<TSignal>(ISignalReceiverHandlerInvoker invoker)
        where TSignal : class, IHttpWebSocketsSignal<TSignal>
    {
        if (!signalTypeByEventType.TryAdd(TSignal.Tag, typeof(TSignal)))
        {
            throw new InvalidOperationException(
                $"the tag '{TSignal.Tag}' is already used by signal type '{signalTypeByEventType[TSignal.Tag]}'"
            );
        }

        invokers.Add(invoker);
        parserByTag[TSignal.Tag] = async (content, ct) =>
            await TSignal
                .HttpWebSocketsSignalSerializer.DeserializeSignal(ServiceProvider, content, ct)
                .ConfigureAwait(false);
    }

    public ValueTask<object> ReadSignal(string tag, Stream stream, CancellationToken cancellationToken) =>
        parserByTag[tag].Invoke(stream, cancellationToken);

    public async Task InvokeHandler(object signal, CancellationToken cancellationToken)
    {
        var relevantInvokers = invokersBySignalType.GetOrAdd(
            signal.GetType(),
            static (_, state) => state.invokers.Where(i => i.SignalType.IsInstanceOfType(state.signal)).ToList(),
            (invokers, signal)
        );

        // looping over the invokers handles the edge case where a handler observes a signal
        // multiple times through the signal's type hierarchy
        foreach (var invoker in relevantInvokers)
        {
            await invoker
                .Invoke(signal, ServiceProvider, WebSocketsTransportName, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
