using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Transport.Http.Client.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Conqueror.Transport.Http.Server.AspNetCore.WebSockets;

internal static class WebSocketEndpoint
{
    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "not necessary for cancellation exceptions")]
    public static async Task Run(
        HttpContext context,
        ILogger logger,
        TimeSpan heartbeatInterval,
        TimeSpan heartbeatTimeout,
        Action<WebSocketWriteStream> setStream,
        Func<Stream, CancellationToken, Task>? onMessage = null)
    {
        try
        {
            if (context.Features.Get<IHttpWebSocketFeature>() is null)
            {
                logger.LogWarning("web sockets feature is not available; did you forget to call UseWebSockets() first?");

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.Headers.Append(HeaderNames.ContentType, ContentTypes.TextPlain);
                await context.Response.WriteAsync("the web socket middleware is not used", context.RequestAborted).ConfigureAwait(false);

                return;
            }

            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.Headers.Append(HeaderNames.ContentType, ContentTypes.TextPlain);
                await context.Response.WriteAsync("not a web socket request", context.RequestAborted).ConfigureAwait(false);

                return;
            }

            if (context.Features.Get<IHttpResponseBodyFeature>() is { } f)
            {
                f.DisableBuffering();
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

            var conquerorWebSocket = new ConquerorWebSocket(webSocket, heartbeatInterval, heartbeatTimeout);

            await using var d = conquerorWebSocket.ConfigureAwait(false);

            setStream(conquerorWebSocket.WriteStream);

            await Read(conquerorWebSocket, onMessage, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception) when (context.RequestAborted.IsCancellationRequested)
        {
            // nothing to do, the client just disconnected, which may have thrown spurious exceptions
            logger.LogDebug("client disconnected from socket");
        }
        catch (Exception e)
        {
            // we don't handle the error here since terminating the connection should cause the client to reconnect;
            // any missed items due to this are by the nature of this transport which does not buffer anything and
            // provides no guarantees about receiving all items
            logger.LogError(e, "an error occurred during web socket connection");
        }
    }

    public static bool TryGetHeartbeatParameters(
        HttpContext context,
        out TimeSpan heartbeatInterval,
        out TimeSpan heartbeatTimeout,
        out string message)
    {
        if (!context.Request.Query.TryGetValue(QueryParameterNames.HeartbeatInterval, out var heartbeatIntervalValues)
            || !int.TryParse(heartbeatIntervalValues, out var heartbeatIntervalSeconds)
            || heartbeatIntervalSeconds is < 0 or > 600)
        {
            heartbeatInterval = Timeout.InfiniteTimeSpan;
            heartbeatTimeout = Timeout.InfiniteTimeSpan;
            message = "must provide heartbeat interval in seconds between 0 and 600";

            return false;
        }

        if (!context.Request.Query.TryGetValue(QueryParameterNames.HeartbeatTimeout, out var heartbeatTimeoutValues)
            || !int.TryParse(heartbeatTimeoutValues, out var heartbeatTimeoutSeconds)
            || heartbeatTimeoutSeconds is < 0 or > 600
            || (heartbeatIntervalSeconds > 0 && heartbeatTimeoutSeconds <= heartbeatIntervalSeconds))
        {
            heartbeatInterval = Timeout.InfiniteTimeSpan;
            heartbeatTimeout = Timeout.InfiniteTimeSpan;
            message = "must provide heartbeat timeout in seconds between 0 and 600 and it must be greater than heartbeat interval";

            return false;
        }

        heartbeatInterval = TimeSpan.FromSeconds(heartbeatIntervalSeconds);
        heartbeatTimeout = TimeSpan.FromSeconds(heartbeatTimeoutSeconds);
        message = string.Empty;

        return true;
    }

    private static async Task Read(
        ConquerorWebSocket ws,
        Func<Stream, CancellationToken, Task>? onMessage,
        CancellationToken token)
    {
        await foreach (var stream in ws.Read(token).ConfigureAwait(false))
        {
            if (onMessage is null)
            {
                throw new InvalidOperationException("did not expect any message from the client");
            }

            await onMessage(stream, token).ConfigureAwait(false);
        }
    }
}
