using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Common.Transport.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore;

internal sealed class WebSocketsConnectionHandler
{
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "all sockets are disposed in a chain when the server socket is disposed")]
    public static async Task HandleWebSocketConnection(HttpContext httpContext, IReadOnlyCollection<string> eventTypeIds, CancellationToken cancellationToken)
    {
        if (httpContext.WebSockets.IsWebSocketRequest)
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<WebSocketsConnectionHandler>>();
            var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            var jsonSerializerOptions = httpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
            var textWebSocket = new TextWebSocketWithHeartbeat(new(webSocket), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            var jsonWebSocket = new JsonWebSocket(textWebSocket, jsonSerializerOptions);
            var socket = new EventingServerWebSocket(jsonWebSocket);
            var publisher = httpContext.RequestServices.GetRequiredService<WebSocketsTransportPublisher>();
            await HandleWebSocketConnection(socket, publisher, logger, eventTypeIds, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "disposal works correctly")]
    private static async Task HandleWebSocketConnection(EventingServerWebSocket socket,
                                                        WebSocketsTransportPublisher publisher,
                                                        ILogger logger,
                                                        IReadOnlyCollection<string> eventTypeIds,
                                                        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _ = await Task.WhenAny(ReadFromSocket(), ReadFromEnumerable()).ConfigureAwait(false);
        }
        finally
        {
            await cts.CancelAsync().ConfigureAwait(false);

            await socket.Close(CancellationToken.None).ConfigureAwait(false);
        }

        async Task ReadFromSocket()
        {
            try
            {
                await foreach (var msg in socket.Read(cts.Token).ConfigureAwait(false))
                {
                    // we don't expect any messages from the socket, we only read to recognize when the connection is closed
                    _ = msg;
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
        }

        async Task ReadFromEnumerable()
        {
            try
            {
                await foreach (var evt in publisher.GetEvents(eventTypeIds, cts.Token).ConfigureAwait(false))
                {
                    _ = await socket.SendMessage(evt, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // nothing to do
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "an error occurred while reading from enumerable");
            }
        }
    }
}
