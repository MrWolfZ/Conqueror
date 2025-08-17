namespace Conqueror.Transport.Http.Server.AspNetCore.Signalling.Sse;

internal sealed partial class HttpSseSignalBroker(IServiceProvider serviceProvider, ILogger<HttpSseSignalBroker> logger)
{
    private readonly ConcurrentDictionary<
        string,
        ImmutableList<ChannelWriter<ChannelMessage>>
    > channelWritersByEventType = [];

    public IAsyncEnumerable<SseItem<string>> Subscribe(IEnumerable<string> eventTypes)
    {
        var eventTypesList = eventTypes.ToList();

        LogSubscribe(logger, eventTypesList);

        var channel = Channel.CreateUnbounded<ChannelMessage>();

        // it is important that this happens here and NOT in SubscribeInternal, since
        // the body of IAsyncEnumerable methods is evaluated lazily, which would delay
        // adding the channel writer to the dictionary until the first item is requested;
        // that in turn would cause a race condition where a client receives the headers
        // before the subscription is fully established
        foreach (var eventType in eventTypesList)
        {
            _ = channelWritersByEventType.AddOrUpdate(
                eventType,
                static (_, channel) => [channel.Writer],
                static (_, list, channel) => list.Add(channel.Writer),
                channel
            );
        }

        return SubscribeInternal(eventTypesList, channel, CancellationToken.None);
    }

    public async Task Publish<TSignal>(
        TSignal signal,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    )
        where TSignal : class, IHttpSseSignal<TSignal>
    {
        try
        {
            LogPublishStart(logger);

            if (!channelWritersByEventType.TryGetValue(TSignal.EventType, out var writers) || writers.Count is 0)
            {
                return;
            }

            var content = await TSignal
                .HttpSseSignalSerializer.SerializeSignal(serviceProvider, signal)
                .ConfigureAwait(false);

            if (conquerorContext.EncodeDownstreamContextData(conquerorContext.TraceId) is { } s)
            {
                content += "\n" + s;
            }

            var item = new SseItem<string>(content, TSignal.EventType) { EventId = conquerorContext.SignalId };

            cancellationToken.ThrowIfCancellationRequested();
            await Task.WhenAll(writers.Select(WriteToChannel)).ConfigureAwait(false);

            async Task WriteToChannel(ChannelWriter<ChannelMessage> writer)
            {
                try
                {
                    // run continuation async to ensure we are not blocking the reader when it notifies us of the completion
                    var taskCompletionSource = new TaskCompletionSource(
                        TaskCreationOptions.RunContinuationsAsynchronously
                    );
                    var channelMessage = new ChannelMessage(item, taskCompletionSource);

                    LogWriteToChannel(logger, TSignal.EventType);

                    await writer.WriteAsync(channelMessage, cancellationToken).ConfigureAwait(false);

                    await using var d = cancellationToken
                        .Register(
                            static tcs => ((TaskCompletionSource)tcs!).TrySetCanceled(CancellationToken.None),
                            taskCompletionSource
                        )
                        .ConfigureAwait(false);

                    LogWroteToChannel(logger, TSignal.EventType);

                    await taskCompletionSource.Task.ConfigureAwait(false);

                    LogGotChannelWriteCompletion(logger, TSignal.EventType);
                }
                catch (ChannelClosedException)
                {
                    // if a client disconnects right when we want to publish, we simply skip that client
                }
                catch (Exception ex) when (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(
                        $"publish of {nameof(signal)} of type '{signal.GetType()}' was cancelled",
                        ex,
                        cancellationToken
                    );
                }
            }
        }
        catch (Exception ex) when (ex is not HttpSseSignalFailedOnPublisherException)
        {
            throw new HttpSseSignalFailedOnPublisherException(
                $"server-sent events {nameof(signal)} of type '{typeof(TSignal)}' failed",
                ex
            )
            {
                SignalPayload = signal,
                TransportType = new SignalTransportType(ServersSentEventsTransportName, SignalTransportRole.Publisher),
            };
        }
    }

    private async IAsyncEnumerable<SseItem<string>> SubscribeInternal(
        IReadOnlyCollection<string> eventTypes,
        Channel<ChannelMessage> channel,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        TaskCompletionSource? latestTaskCompletionSource = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                LogWaitForChannel(logger);

                (var item, latestTaskCompletionSource) = await channel
                    .Reader.ReadAsync(cancellationToken)
                    .ConfigureAwait(false);

                LogGotMessageFromChannel(logger, item.EventType);

                yield return item;

                LogYieldedItem(logger, item.EventType);

                // this code will be hit when the next item is requested from the enumerable
                latestTaskCompletionSource.SetResult();
                latestTaskCompletionSource = null;
            }
        }
        finally
        {
            LogSubscriptionClosed(logger);

            channel.Writer.Complete();

            _ = latestTaskCompletionSource?.TrySetResult();

            // notify any pending publish operations
            while (channel.Reader.TryRead(out var m))
            {
                _ = m.TaskCompletionSource.TrySetResult();
            }

            foreach (var eventType in eventTypes)
            {
                _ = channelWritersByEventType.AddOrUpdate(
                    eventType,
                    static (_, _) => [],
                    static (_, list, channel) => list.Remove(channel.Writer),
                    channel
                );
            }
        }
    }

    [LoggerMessage(LogLevel.Trace, "starting publish")]
    private static partial void LogPublishStart(ILogger logger);

    [LoggerMessage(LogLevel.Trace, "writing signal with event type '{EventType}' to channel")]
    private static partial void LogWriteToChannel(ILogger logger, string eventType);

    [LoggerMessage(LogLevel.Trace, "wrote signal with event type '{EventType}' to channel")]
    private static partial void LogWroteToChannel(ILogger logger, string eventType);

    [LoggerMessage(LogLevel.Trace, "got channel write completion for event type '{EventType}'")]
    private static partial void LogGotChannelWriteCompletion(ILogger logger, string eventType);

    [LoggerMessage(LogLevel.Trace, "subscribing to events with types [{eventTypes}]")]
    private static partial void LogSubscribe(ILogger logger, IEnumerable<string> eventTypes);

    [LoggerMessage(LogLevel.Trace, "closed subscription")]
    private static partial void LogSubscriptionClosed(ILogger logger);

    [LoggerMessage(LogLevel.Trace, "waiting for read on channel")]
    private static partial void LogWaitForChannel(ILogger logger);

    [LoggerMessage(LogLevel.Trace, "got message with event type '{EventType}' from channel")]
    private static partial void LogGotMessageFromChannel(ILogger logger, string eventType);

    [LoggerMessage(LogLevel.Trace, "yielded item with event type '{EventType}'")]
    private static partial void LogYieldedItem(ILogger logger, string eventType);

    private sealed record ChannelMessage(SseItem<string> Item, TaskCompletionSource TaskCompletionSource);
}
