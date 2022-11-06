using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Conqueror.Streaming.Interactive.Transport.Http.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// it makes sense for these types to be in the same file
#pragma warning disable SA1402

// it makes sense for the file to be named differently
#pragma warning disable SA1649

namespace Conqueror.Streaming.Interactive.Transport.Http.Server.AspNetCore
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
                using var streamingServerWebsocket = new InteractiveStreamingServerWebSocket<T>(jsonWebSocket);
                await HandleWebSocketConnection(streamingServerWebsocket, enumerable, logger);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "disposal works correctly")]
        private static async Task HandleWebSocketConnection(InteractiveStreamingServerWebSocket<T> socket, IAsyncEnumerable<T> enumerable, ILogger logger)
        {
            var channel = Channel.CreateBounded<object?>(new BoundedChannelOptions(8));

            using var cts = new CancellationTokenSource();

            var sourceEnumerator = enumerable.GetAsyncEnumerator(cts.Token);

            try
            {
                _ = await Task.WhenAny(ReadFromSocket(), ReadFromEnumerable());
            }
            finally
            {
                cts.Cancel();

                await socket.Close(CancellationToken.None);
            }

            async Task ReadFromSocket()
            {
                try
                {
                    await foreach (var msg in socket.Read(cts.Token))
                    {
                        await channel.Writer.WriteAsync(msg, cts.Token);
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
                    while (await channel.Reader.WaitToReadAsync(cts.Token))
                    {
                        while (channel.Reader.TryRead(out _))
                        {
                            if (!await sourceEnumerator.MoveNextAsync())
                            {
                                return;
                            }

                            _ = await socket.SendMessage(sourceEnumerator.Current, cts.Token);
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
                    _ = await socket.SendError(ex.Message, cts.Token);
                }
            }
        }
    }
}
