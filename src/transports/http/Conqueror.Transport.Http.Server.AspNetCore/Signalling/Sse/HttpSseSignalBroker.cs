using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.ServerSentEvents;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Conqueror.Signalling.Sse;

namespace Conqueror.Transport.Http.Server.AspNetCore.Signalling.Sse;

internal sealed class HttpSseSignalBroker(IServiceProvider serviceProvider)
{
    private readonly ConcurrentDictionary<string, ImmutableList<ChannelWriter<SseItem<HttpSseSignalEnvelope>>>> channelWritersByEventType = new();
    private readonly ConcurrentDictionary<Type, Action<SseItem<HttpSseSignalEnvelope>, IBufferWriter<byte>>> itemFormatterBySignalType = new();

    public (IAsyncEnumerable<SseItem<HttpSseSignalEnvelope>> Items, Action<SseItem<HttpSseSignalEnvelope>, IBufferWriter<byte>> Formatter) Subscribe(
        IEnumerable<string> eventTypes,
        CancellationToken cancellationToken)
    {
        var items = SubscribeInternal(eventTypes, cancellationToken);

        return (items, WriteItem);
    }

    public async Task Publish<TSignal>(
        TSignal signal,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken)
        where TSignal : class, IHttpSseSignal<TSignal>
    {
        _ = itemFormatterBySignalType.GetOrAdd(typeof(TSignal), CreateFormatter<TSignal>);

        var eventType = TSignal.EventType;
        var envelope = new HttpSseSignalEnvelope(signal, conquerorContext.EncodeDownstreamContextData());
        var item = new SseItem<HttpSseSignalEnvelope>(envelope, eventType);

        if (channelWritersByEventType.TryGetValue(eventType, out var writers))
        {
            foreach (var writer in writers)
            {
                try
                {
                    await writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                }
                catch (ChannelClosedException)
                {
                    // nothing to do, the channel is currently being disposed
                }
                catch (OperationCanceledException)
                {
                    // nothing to do, we simply abort the publish
                    return;
                }
            }
        }
    }

    private IAsyncEnumerable<SseItem<HttpSseSignalEnvelope>> SubscribeInternal(
        IEnumerable<string> eventTypes,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<SseItem<HttpSseSignalEnvelope>>(new BoundedChannelOptions(8));

        using var d = new AggregateDisposable();

        foreach (var eventType in eventTypes)
        {
            _ = channelWritersByEventType.AddOrUpdate(eventType, _ => [channel.Writer], (_, list) => list.Add(channel.Writer));
            var registration = cancellationToken.Register(
                static state =>
                {
                    state.channel.Writer.Complete();
                    _ = state.channelWritersByEventType.AddOrUpdate(state.eventType, _ => [], (_, list) => list.Remove(state.channel.Writer));
                },
                (eventType, channel, channelWritersByEventType));
            d.Add(registration);
        }

        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    private void WriteItem(SseItem<HttpSseSignalEnvelope> item, IBufferWriter<byte> bufferWriter)
    {
        var formatter = itemFormatterBySignalType.GetValueOrDefault(item.Data.Signal.GetType());

        Debug.Assert(formatter is not null, "formatter should always be set during a publish before this is called");

        formatter(item, bufferWriter);
    }

    private Action<SseItem<HttpSseSignalEnvelope>, IBufferWriter<byte>> CreateFormatter<TSignal>(Type signalType)
        where TSignal : class, IHttpSseSignal<TSignal>
    {
        return (item, writer) =>
        {
            // TODO: write context data

            TSignal.HttpSseSignalSerializer.Serialize(
                serviceProvider,
                (TSignal)item.Data.Signal,
                writer);
        };
    }
}
