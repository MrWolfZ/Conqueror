namespace Conqueror.Transport.Http.Client.Signalling.Sse;

internal sealed class HttpSseSignalReceiver(
    IServiceProvider serviceProvider,
    IReadOnlyCollection<Type> signalTypes,
    Type? handlerType
) : IHttpSseSignalReceiver
{
    private readonly List<ISignalReceiverHandlerInvoker> invokers = [];
    private readonly ConcurrentDictionary<Type, List<ISignalReceiverHandlerInvoker>> invokersBySignalType = [];
    private readonly Dictionary<string, Func<string, Task<object>>> parserByEventType = [];
    private readonly Dictionary<string, Type> signalTypeByEventType = [];

    public HttpSseSignalReceiverConfiguration? Configuration { get; private set; }

    public IReadOnlyCollection<string> EventTypes => parserByEventType.Keys;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public IReadOnlyCollection<Type> SignalTypes { get; } = signalTypes;

    public Type? HandlerType { get; } = handlerType;

    public bool IsEnabled => Configuration is not null;

    public void Disable() => Configuration = null;

    public HttpSseSignalReceiverConfiguration Enable(Uri address)
    {
        Configuration = new HttpSseSignalReceiverConfiguration { Address = address };

        return Configuration;
    }

    public void AddSignalType<TSignal>(ISignalReceiverHandlerInvoker invoker)
        where TSignal : class, IHttpSseSignal<TSignal>
    {
        if (!signalTypeByEventType.TryAdd(TSignal.EventType, typeof(TSignal)))
        {
            throw new InvalidOperationException(
                $"the event type '{TSignal.EventType}' is already used by signal type '{signalTypeByEventType[TSignal.EventType]}'"
            );
        }

        invokers.Add(invoker);
        parserByEventType[TSignal.EventType] = async content =>
            await TSignal.HttpSseSignalSerializer.DeserializeSignal(ServiceProvider, content).ConfigureAwait(false);
    }

    public Task<HttpSseSignalEnvelope> ParseItem(string eventType, ReadOnlySpan<byte> bytes)
    {
        var content = Encoding.UTF8.GetString(bytes);

        return Parse();

        async Task<HttpSseSignalEnvelope> Parse()
        {
            var newLineIndex = content.IndexOf('\n', StringComparison.Ordinal);

            var serializedSignal = content;
            string? contextData = null;
            if (newLineIndex >= 0)
            {
                serializedSignal = content[..newLineIndex];
                contextData = content[(newLineIndex + 1)..];
            }

            var signal = await parserByEventType[eventType].Invoke(serializedSignal).ConfigureAwait(false);

            return new HttpSseSignalEnvelope(signal, contextData);
        }
    }

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
                .Invoke(signal, ServiceProvider, ServersSentEventsTransportName, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
