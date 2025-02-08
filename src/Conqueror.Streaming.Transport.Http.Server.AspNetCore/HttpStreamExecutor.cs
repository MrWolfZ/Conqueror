using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Conqueror.Streaming.Transport.Http.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

internal static class HttpStreamExecutor
{
    public static async Task ExecuteStreamingRequest<TRequest, TItem>(HttpContext httpContext, CancellationToken cancellationToken)
        where TRequest : class
    {
        var producer = httpContext.RequestServices.GetRequiredService<IStreamProducer<TRequest, TItem>>();
        await HandleWebSocketConnection(httpContext, producer, cancellationToken).ConfigureAwait(false);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "all sockets are disposed in a chain when the server socket is disposed")]
    private static async Task HandleWebSocketConnection<TRequest, TItem>(HttpContext httpContext,
                                                                         IStreamProducer<TRequest, TItem> producer,
                                                                         CancellationToken cancellationToken)
        where TRequest : class
    {
        if (httpContext.WebSockets.IsWebSocketRequest)
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(HttpStreamExecutor));
            var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            var jsonSerializerOptions = httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
            var textWebSocket = new TextWebSocketWithHeartbeat(new(webSocket), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            var jsonWebSocket = new JsonWebSocket(textWebSocket, jsonSerializerOptions);
            using var streamingServerWebsocket = new StreamingServerWebSocket<TRequest, TItem>(jsonWebSocket);
            await HandleWebSocketConnection(streamingServerWebsocket, producer, logger, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "disposal works correctly")]
    private static async Task HandleWebSocketConnection<TRequest, TItem>(StreamingServerWebSocket<TRequest, TItem> socket,
                                                                         IStreamProducer<TRequest, TItem> producer,
                                                                         ILogger logger,
                                                                         CancellationToken cancellationToken)
        where TRequest : class
    {
        var channel = Channel.CreateBounded<object?>(new BoundedChannelOptions(8));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _ = await Task.WhenAny(ReadFromSocket(), ReadFromEnumerable()).ConfigureAwait(false);
        }
        finally
        {
            await socket.Close(CancellationToken.None).ConfigureAwait(false);

            await cts.CancelAsync().ConfigureAwait(false);
        }

        async Task ReadFromSocket()
        {
            try
            {
                await foreach (var msg in socket.Read(cts.Token).ConfigureAwait(false))
                {
                    await channel.Writer.WriteAsync(msg, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // nothing to do
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "an error occurred while reading from socket");
            }
            finally
            {
                channel.Writer.Complete();
            }
        }

        async Task ReadFromEnumerable()
        {
            try
            {
                IAsyncEnumerator<TItem>? sourceEnumerator = null;

                while (await channel.Reader.WaitToReadAsync(cts.Token).ConfigureAwait(false))
                {
                    while (channel.Reader.TryRead(out var msg))
                    {
                        if (msg is InitialRequestMessage<TRequest> requestMessage)
                        {
                            sourceEnumerator = producer.ExecuteRequest(requestMessage.Payload, cts.Token).GetAsyncEnumerator(cts.Token);
                        }
                        else if (sourceEnumerator == null)
                        {
                            throw new InvalidOperationException("received request for next item before initial request");
                        }

                        if (!await sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            return;
                        }

                        _ = await socket.SendMessage(sourceEnumerator.Current, cts.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // nothing to do
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "an error occurred while reading from enumerable");
                _ = await socket.SendError(ex.Message, cts.Token).ConfigureAwait(false);
            }
        }
    }
}
