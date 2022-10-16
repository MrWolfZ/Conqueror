using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Conqueror.Streaming.Interactive.Extensions.AspNetCore.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// it makes sense for these types to be in the same file
#pragma warning disable SA1402

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Server
{
    [ApiController]
    public abstract class ConquerorInteractiveStreamingWithRequestPayloadWebsocketTransportControllerBase<TRequest, TItem> : ConquerorInteractiveStreamingWebsocketTransportControllerBase<TItem>
        where TRequest : class
        where TItem : notnull
    {
        protected async Task ExecuteRequest(TRequest request, CancellationToken cancellationToken)
        {
            var handler = Request.HttpContext.RequestServices.GetRequiredService<IInteractiveStreamingHandler<TRequest, TItem>>();
            await HandleWebSocketConnection(handler.ExecuteRequest(request, cancellationToken));
        }
    }

    [ApiController]
    public abstract class ConquerorInteractiveStreamingWithoutRequestPayloadWebsocketTransportControllerBase<TRequest, TItem> : ConquerorInteractiveStreamingWebsocketTransportControllerBase<TItem>
        where TRequest : class, new()
        where TItem : notnull
    {
        protected async Task ExecuteRequest(CancellationToken cancellationToken)
        {
            var handler = Request.HttpContext.RequestServices.GetRequiredService<IInteractiveStreamingHandler<TRequest, TItem>>();
            await HandleWebSocketConnection(handler.ExecuteRequest(new(), cancellationToken));
        }
    }

    public abstract class ConquerorInteractiveStreamingWebsocketTransportControllerBase<T> : ControllerBase
        where T : notnull
    {
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "all sockets are disposed in a chain when the server socket is disposed")]
        protected async Task HandleWebSocketConnection(IAsyncEnumerable<T> enumerable)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<ConquerorInteractiveStreamingWebsocketTransportControllerBase<T>>>();
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                var jsonSerializerOptions = HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
                var textWebSocket = new TextWebSocketWithHeartbeat(new(webSocket), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
                var jsonWebSocket = new JsonWebSocket(textWebSocket, jsonSerializerOptions);
                using var streamingServerWebsocket = new StreamingServerWebSocket<T>(jsonWebSocket);
                await HandleWebSocketConnection(streamingServerWebsocket, enumerable, logger, HttpContext.RequestAborted);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private static async Task HandleWebSocketConnection(StreamingServerWebSocket<T> webSocket, IAsyncEnumerable<T> enumerable, ILogger logger, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = cts.Token;

            // ReSharper disable once AccessToDisposedClosure
            webSocket.SocketClosed += () => cts.Cancel();

            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);

            var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(128));

            _ = await Task.WhenAny(ReadFromSocket(), ReadFromEnumerable());

            await webSocket.Close(cancellationToken);

            async Task ReadFromSocket()
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var message = await webSocket.Receive(token);

                        switch (message)
                        {
                            case RequestNextItemMessage _:
                                await channel.Writer.WriteAsync(1, token);
                                break;
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
            }

            async Task ReadFromEnumerable()
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        _ = await channel.Reader.ReadAsync(token);

                        if (!await enumerator.MoveNextAsync())
                        {
                            return;
                        }

                        await webSocket.SendMessage(enumerator.Current, token);
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
    }
}
