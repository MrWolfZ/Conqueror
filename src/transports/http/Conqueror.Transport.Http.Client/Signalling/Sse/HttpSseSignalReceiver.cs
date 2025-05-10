using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Signalling.Sse;

namespace Conqueror.Transport.Http.Client.Signalling.Sse;

internal sealed class HttpSseSignalReceiver(IServiceProvider serviceProvider, Type handlerType) : IHttpSseSignalReceiver
{
    private readonly List<ISignalReceiverHandlerInvoker> invokers = [];
    private readonly ConcurrentDictionary<Type, List<ISignalReceiverHandlerInvoker>> invokersBySignalType = [];
    private readonly Dictionary<string, Func<string, object>> parserByEventType = [];
    private readonly Dictionary<string, Type> signalTypeByEventType = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public Type HandlerType { get; } = handlerType;

    public bool IsEnabled => Configuration is not null;

    public HttpSseSignalReceiverConfiguration? Configuration { get; private set; }

    public IReadOnlyCollection<string> EventTypes => parserByEventType.Keys;

    public void Disable() => Configuration = null;

    public HttpSseSignalReceiverConfiguration Enable(Uri address)
    {
        Configuration = new() { Address = address };

        return Configuration;
    }

    public void AddSignalType<TSignal>(ISignalReceiverHandlerInvoker invoker)
        where TSignal : class, IHttpSseSignal<TSignal>
    {
        if (!signalTypeByEventType.TryAdd(TSignal.EventType, typeof(TSignal)))
        {
            throw new InvalidOperationException(
                $"the event type '{TSignal.EventType}' is already used by signal type '{signalTypeByEventType[TSignal.EventType]}'");
        }

        invokers.Add(invoker);
        parserByEventType[TSignal.EventType] = content => TSignal.HttpSseSignalSerializer.Deserialize(ServiceProvider, content);
    }

    public HttpSseSignalEnvelope ParseItem(string eventType, ReadOnlySpan<byte> bytes)
    {
        var content = Encoding.UTF8.GetString(bytes);
        var newLineIndex = content.IndexOf('\n');

        var serializedSignal = content;
        string? contextData = null;
        if (newLineIndex >= 0)
        {
            serializedSignal = content[..newLineIndex];
            contextData = content[(newLineIndex + 1)..];
        }

        var signal = parserByEventType[eventType].Invoke(serializedSignal);

        return new(signal, contextData);
    }

    public async Task InvokeHandler(object signal, CancellationToken cancellationToken)
    {
        var relevantInvokers = invokersBySignalType.GetOrAdd(signal.GetType(), _ => invokers.Where(i => i.SignalType.IsInstanceOfType(signal)).ToList());

        // looping over the invokers handles the edge case where a handler observes a signal
        // multiple times through the signal's type hierarchy
        foreach (var invoker in relevantInvokers)
        {
            await invoker.Invoke(
                             signal,
                             ServiceProvider,
                             ConquerorTransportHttpConstants.ServersSentEventsTransportName,
                             cancellationToken)
                         .ConfigureAwait(false);
        }
    }
}
