namespace Conqueror.Transport.Http.Server.AspNetCore.Signalling.WebSockets;

internal sealed partial class HttpWebSocketsSignalBroker(
    IServiceProvider serviceProvider,
    ILogger<HttpWebSocketsSignalBroker> logger
)
{
    private readonly ConcurrentDictionary<string, ImmutableList<HttpWebSocketsSignalBrokerStream>> streamsBySignalTag =
        [];

    public IDisposable Subscribe(
        HttpWebSocketsSignalBrokerStream stream,
        IEnumerable<string> signalTags,
        CancellationToken cancellationToken
    )
    {
        var signalTagsList = signalTags.ToList();

        LogSubscribe(logger, signalTagsList);

        foreach (var tag in signalTagsList)
        {
            _ = streamsBySignalTag.AddOrUpdate(
                tag,
                static (_, stream) => [stream],
                static (_, list, stream) => list.Add(stream),
                stream
            );
        }

        return cancellationToken.Register(() =>
        {
            LogSubscriptionClosed(logger);

            foreach (var signalTag in signalTagsList)
            {
                _ = streamsBySignalTag.AddOrUpdate(
                    signalTag,
                    static (_, _) => [],
                    static (_, list, stream) => list.Remove(stream),
                    stream
                );
            }
        });
    }

    [SuppressMessage(
        "Reliability",
        "CA2007:Consider calling ConfigureAwait on the awaited task",
        Justification = "false positive"
    )]
    public async Task Publish<TSignal>(
        TSignal signal,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    )
        where TSignal : class, IHttpWebSocketsSignal<TSignal>
    {
        try
        {
            LogPublishStart(logger);

            if (!streamsBySignalTag.TryGetValue(TSignal.Tag, out var streams) || streams.Count is 0)
            {
                return;
            }

            Stream stream = streams.Count is 1 ? streams[0] : new MultiplexWriteStream(streams);

            await using var d = stream is MultiplexWriteStream ? stream : null;

            LogWriteStream(logger, streams.Count);

            await HttpWebSocketsSignalProtocolV1
                .Write(
                    stream,
                    TSignal.Tag,
                    conquerorContext.EncodeDownstreamContextData(
                        conquerorContext.TraceId,
                        signalId: conquerorContext.SignalId
                    ),
                    (s, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();

                        return new(
                            TSignal.HttpWebSocketsSignalSerializer.SerializeSignal(serviceProvider, signal, s, ct)
                        );
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not HttpSseSignalFailedOnPublisherException)
        {
            throw new HttpSseSignalFailedOnPublisherException(
                $"web sockets {nameof(signal)} of type '{typeof(TSignal)}' failed",
                ex
            )
            {
                SignalPayload = signal,
                TransportType = new SignalTransportType(WebSocketsTransportName, SignalTransportRole.Publisher),
            };
        }
    }

    [LoggerMessage(LogLevel.Trace, "starting publish")]
    private static partial void LogPublishStart(ILogger logger);

    [LoggerMessage(LogLevel.Trace, "writing to {Count} streams")]
    private static partial void LogWriteStream(ILogger logger, int count);

    [LoggerMessage(LogLevel.Trace, "subscribing to events with types [{signalTags}]")]
    private static partial void LogSubscribe(ILogger logger, IEnumerable<string> signalTags);

    [LoggerMessage(LogLevel.Trace, "closed subscription")]
    private static partial void LogSubscriptionClosed(ILogger logger);
}
