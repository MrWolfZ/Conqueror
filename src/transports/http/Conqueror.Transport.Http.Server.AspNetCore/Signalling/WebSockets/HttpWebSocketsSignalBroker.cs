using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Signalling.WebSockets;
using Microsoft.Extensions.Logging;

namespace Conqueror.Transport.Http.Server.AspNetCore.Signalling.WebSockets;

internal sealed partial class HttpWebSocketsSignalBroker(
    IServiceProvider serviceProvider,
    ILogger<HttpWebSocketsSignalBroker> logger)
{
    private readonly ConcurrentDictionary<string, ImmutableList<HttpWebSocketsSignalBrokerStream>> streamsBySignalTag = new();

    public IDisposable Subscribe(HttpWebSocketsSignalBrokerStream stream, IEnumerable<string> signalTags)
    {
        var signalTagsList = signalTags.ToList();

        LogSubscribe(logger, signalTagsList);

        foreach (var tag in signalTagsList)
        {
            _ = streamsBySignalTag.AddOrUpdate(tag, _ => [stream], (_, list) => list.Add(stream));
        }

        return new AnonymousDisposable(() =>
        {
            LogSubscriptionClosed(logger);

            foreach (var signalTag in signalTagsList)
            {
                _ = streamsBySignalTag.AddOrUpdate(
                    signalTag,
                    _ => [],
                    (_, list) => list.Remove(stream));
            }
        });
    }

    [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "false positive")]
    public async Task Publish<TSignal>(
        TSignal signal,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken)
        where TSignal : class, IHttpWebSocketsSignal<TSignal>
    {
        LogPublishStart(logger);

        if (!streamsBySignalTag.TryGetValue(TSignal.Tag, out var streams) || streams.Count == 0)
        {
            return;
        }

        Stream stream = streams.Count == 1 ? streams[0] : new MultiplexWriteStream(streams);

        await using var d = stream is MultiplexWriteStream ? stream : null;

        LogWriteStream(logger, streams.Count);

        await HttpWebSocketSignalProtocolV1.Write(
                                               stream,
                                               TSignal.Tag,
                                               conquerorContext.EncodeDownstreamContextData(),
                                               (s, ct) =>
                                               {
                                                   ct.ThrowIfCancellationRequested();

                                                   return TSignal.HttpWebSocketsSignalSerializer.Serialize(
                                                       serviceProvider,
                                                       signal,
                                                       s,
                                                       ct);
                                               },
                                               cancellationToken)
                                           .ConfigureAwait(false);
    }

    [LoggerMessage(LogLevel.Trace, "starting publish")]
    private static partial void LogPublishStart(ILogger logger);

    [LoggerMessage(LogLevel.Trace, "writing to {Count} streams")]
    private static partial void LogWriteStream(ILogger logger, int count);

    [LoggerMessage(LogLevel.Trace, "subscribing to events with types [{signalTags}]")]
    private static partial void LogSubscribe(ILogger logger, IEnumerable<string> signalTags);

    [LoggerMessage(LogLevel.Trace, "closed subscription")]
    private static partial void LogSubscriptionClosed(ILogger logger);

    private sealed class AnonymousDisposable(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
