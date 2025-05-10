using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Conqueror.Transport.Http.Server.AspNetCore.Signalling.Sse;

internal sealed class HttpSseSignalBroker(IServiceProvider serviceProvider)
{
    private readonly ConcurrentDictionary<string, ImmutableList<ChannelWriter<ChannelMessage>>> channelWritersByEventType = new();

    public IAsyncEnumerable<SseItem<string>> Subscribe(
        IEnumerable<string> eventTypes,
        CancellationToken cancellationToken)
    {
        var items = SubscribeInternal(eventTypes, cancellationToken);

        return items;
    }

    public async Task Publish<TSignal>(
        TSignal signal,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken)
        where TSignal : class, IHttpSseSignal<TSignal>
    {
        if (!channelWritersByEventType.TryGetValue(TSignal.EventType, out var writers))
        {
            return;
        }

        var content = TSignal.HttpSseSignalSerializer.Serialize(serviceProvider, signal);

        if (conquerorContext.EncodeDownstreamContextData() is { } s)
        {
            content += "\n" + s;
        }

        var item = new SseItem<string>(content, TSignal.EventType)
        {
            EventId = conquerorContext.GetSignalId(),
        };

        cancellationToken.ThrowIfCancellationRequested();
        await Task.WhenAll(writers.Select(WriteToChannel)).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        async Task WriteToChannel(ChannelWriter<ChannelMessage> writer)
        {
            try
            {
                var taskCompletionSource = new TaskCompletionSource();
                var channelMessage = new ChannelMessage(item, taskCompletionSource);

                await writer.WriteAsync(channelMessage, cancellationToken).ConfigureAwait(false);

                await using var d = cancellationToken.Register(() => taskCompletionSource.TrySetCanceled()).ConfigureAwait(false);

                await taskCompletionSource.Task.ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                // if a client disconnects right when we want to publish, we simply skip that client
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException($"publish of signal of type '{signal.GetType()}' was cancelled", cancellationToken);
            }
        }
    }

    private async IAsyncEnumerable<SseItem<string>> SubscribeInternal(
        IEnumerable<string> eventTypes,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<ChannelMessage>(new BoundedChannelOptions(8));

        using var d = new AggregateDisposable();

        foreach (var eventType in eventTypes)
        {
            _ = channelWritersByEventType.AddOrUpdate(eventType, _ => [channel.Writer], (_, list) => list.Add(channel.Writer));
            var registration = cancellationToken.Register(
                static state =>
                {
                    _ = state.channel.Writer.TryComplete();
                    _ = state.channelWritersByEventType.AddOrUpdate(
                        state.eventType,
                        _ => [],
                        (_, list) => list.Remove(state.channel.Writer));
                },
                (eventType, channel, channelWritersByEventType));
            d.Add(registration);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var (item, taskCompletionSource) = await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            await using var d2 = cancellationToken.Register(() => taskCompletionSource.TrySetResult()).ConfigureAwait(false);

            yield return item;

            // this code will be hit when the next item is requested from the enumerable
            _ = taskCompletionSource.TrySetResult();
        }
    }

    private sealed record ChannelMessage(
        SseItem<string> Item,
        TaskCompletionSource TaskCompletionSource);
}
