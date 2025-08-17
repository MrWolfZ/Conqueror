namespace Conqueror.Streaming.Transport.Http.Common;

using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

internal sealed class TextWebSocket(WebSocket socket) : IDisposable
{
    public WebSocketState State => socket.State;

    public void Dispose() => socket.Dispose();

    public async IAsyncEnumerable<string> Read([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // we receive messages in a separate task so that close messages are processed without
        // the client needing to continue enumerating the messages; we expect that we will only
        // ever need to read one message ahead, so we create a bounded channel with capacity 1
        var channel = Channel.CreateBounded<string>(capacity: 1);

        RunReceiveLoop();

        // run foreach loop instead of returning the channel enumerable directly to ensure
        // the linked cancellation token source has the correct lifetime
        await foreach (var msg in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return msg;
        }

        [SuppressMessage(
            "Design",
            "MA0155:Do not use async void methods",
            Justification = "we are handling exceptions properly"
        )]
        async void RunReceiveLoop()
        {
            try
            {
                var buffer = new byte[1024 * 4];

                while (true)
                {
                    var receiveResult = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                    if (receiveResult.CloseStatus.HasValue)
                    {
                        if (socket.State is WebSocketState.CloseReceived)
                        {
                            await socket
                                .CloseOutputAsync(
                                    receiveResult.CloseStatus.Value,
                                    receiveResult.CloseStatusDescription,
                                    cancellationToken
                                )
                                .ConfigureAwait(false);
                        }

                        channel.Writer.Complete();

                        return;
                    }

                    if (receiveResult.MessageType is not WebSocketMessageType.Text)
                    {
                        throw new InvalidOperationException(
                            $"expected websocket message type '{nameof(WebSocketMessageType.Text)}', got '{receiveResult.MessageType}'"
                        );
                    }

                    if (receiveResult.EndOfMessage)
                    {
                        await channel
                            .Writer.WriteAsync(
                                Encoding.UTF8.GetString(buffer, index: 0, receiveResult.Count),
                                cancellationToken
                            )
                            .ConfigureAwait(false);

                        continue;
                    }

                    var allBytes = new List<byte>();

                    allBytes.AddRange(new ArraySegment<byte>(buffer, offset: 0, receiveResult.Count));

                    while (!receiveResult.EndOfMessage)
                    {
                        receiveResult = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                        allBytes.AddRange(new ArraySegment<byte>(buffer, offset: 0, receiveResult.Count));
                    }

                    await channel
                        .Writer.WriteAsync(Encoding.UTF8.GetString(allBytes.ToArray()), cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                channel.Writer.Complete(e);
            }
        }
    }

    public async Task<bool> Send(string message, CancellationToken cancellationToken)
    {
        if (socket.State is not WebSocketState.Open and not WebSocketState.CloseReceived)
        {
            return false;
        }

        var messageBytes = Encoding.UTF8.GetBytes(message);

        await socket
            .SendAsync(
                new(messageBytes, offset: 0, messageBytes.Length),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken
            )
            .ConfigureAwait(false);

        return true;
    }

    public async Task Close(CancellationToken cancellationToken)
    {
        if (
            socket.State
            is not WebSocketState.Open
                and not WebSocketState.CloseReceived
                and not WebSocketState.CloseSent
        )
        {
            return;
        }

        try
        {
            await socket
                .CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    nameof(WebSocketCloseStatus.NormalClosure),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
            when (ex is ObjectDisposedException or IOException { InnerException: ObjectDisposedException })
        {
            // if closing the connection fails due to a disposed object, we consider
            // the closing successful; this can for example happen with the ASP Core
            // `TestWebSocket` class which can throw this error when server and client
            // close the connection at the same time
        }
    }
}
