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

    public IAsyncEnumerable<SseItem<string>> Subscribe(IEnumerable<string> eventTypes) => SubscribeInternal(eventTypes);

    public Task Publish<TSignal>(
        TSignal signal,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken)
        where TSignal : class, IHttpSseSignal<TSignal>
    {
        if (!channelWritersByEventType.TryGetValue(TSignal.EventType, out var writers))
        {
            return Task.CompletedTask;
        }

        var content = TSignal.HttpSseSignalSerializer.Serialize(serviceProvider, signal);

        var signalId = conquerorContext.RemoveSignalId();

        if (conquerorContext.EncodeDownstreamContextData() is { } s)
        {
            content += "\n" + s;
        }

        var item = new SseItem<string>(content, TSignal.EventType)
        {
            EventId = signalId,
        };

        cancellationToken.ThrowIfCancellationRequested();
        return Task.WhenAll(writers.Select(WriteToChannel));

        async Task WriteToChannel(ChannelWriter<ChannelMessage> writer)
        {
            try
            {
                // run continuation async to ensure we are not blocking the reader when it notifies us of the completion
                var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var channelMessage = new ChannelMessage(item, taskCompletionSource);

                await writer.WriteAsync(channelMessage, cancellationToken).ConfigureAwait(false);

                await using var d = cancellationToken.Register(() => taskCompletionSource.TrySetCanceled()).ConfigureAwait(false);

                await taskCompletionSource.Task.ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                // if a client disconnects right when we want to publish, we simply skip that client
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException($"publish of signal of type '{signal.GetType()}' was cancelled", ex, cancellationToken);
            }
        }
    }

    private async IAsyncEnumerable<SseItem<string>> SubscribeInternal(
        IEnumerable<string> eventTypes,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var eventTypesList = eventTypes.ToList();

        var channel = Channel.CreateBounded<ChannelMessage>(new BoundedChannelOptions(8));

        TaskCompletionSource? latestTaskCompletionSource = null;

        try
        {
            foreach (var eventType in eventTypesList)
            {
                _ = channelWritersByEventType.AddOrUpdate(eventType, _ => [channel.Writer], (_, list) => list.Add(channel.Writer));
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                (var item, latestTaskCompletionSource) = await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                yield return item;

                // this code will be hit when the next item is requested from the enumerable
                latestTaskCompletionSource.SetResult();
                latestTaskCompletionSource = null;
            }
        }
        finally
        {
            channel.Writer.Complete();

            _ = latestTaskCompletionSource?.TrySetResult();

            // notify any pending publish operations
            while (channel.Reader.TryRead(out var m))
            {
                _ = m.TaskCompletionSource.TrySetResult();
            }

            foreach (var eventType in eventTypesList)
            {
                _ = channelWritersByEventType.AddOrUpdate(
                    eventType,
                    _ => [],
                    (_, list) => list.Remove(channel.Writer));
            }
        }
    }

    private sealed record ChannelMessage(
        SseItem<string> Item,
        TaskCompletionSource TaskCompletionSource);
}
