using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Interactive.Transport.Http.Common;

internal sealed class TextWebSocket : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly WebSocket socket;

    public TextWebSocket(WebSocket socket)
    {
        this.socket = socket;
    }

    public WebSocketState State => socket.State;

    public void Dispose()
    {
        socket.Dispose();
        cancellationTokenSource.Dispose();
    }

    public async IAsyncEnumerable<string> Read([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);
        var linkedToken = cts.Token;

        // we receive messages in a separate task so that close messages are processed without
        // the client needing to continue enumerating the messages; we expect that we will only
        // ever need to read one message ahead, so we create a bounded channel with capacity 1
        var channel = Channel.CreateBounded<string>(1);

        RunReceiveLoop();

        // run foreach loop instead of returning the channel enumerable directly to ensure
        // the linked cancellation token source has the correct lifetime
        await foreach (var msg in channel.Reader.ReadAllAsync(linkedToken))
        {
            yield return msg;
        }

        async void RunReceiveLoop()
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (true)
                {
                    var receiveResult = await socket.ReceiveAsync(buffer, linkedToken).ConfigureAwait(false);

                    if (receiveResult.CloseStatus.HasValue)
                    {
                        await socket.CloseAsync(
                            receiveResult.CloseStatus.Value,
                            receiveResult.CloseStatusDescription,
                            cancellationToken).ConfigureAwait(false);

                        channel.Writer.Complete();
                        return;
                    }

                    if (receiveResult.MessageType != WebSocketMessageType.Text)
                    {
                        throw new InvalidOperationException($"expected websocket message type '{WebSocketMessageType.Text}', got '{receiveResult.MessageType}'");
                    }

                    if (receiveResult.EndOfMessage)
                    {
                        await channel.Writer.WriteAsync(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count), linkedToken).ConfigureAwait(false);
                        continue;
                    }

                    var allBytes = new List<byte>();

                    allBytes.AddRange(new ArraySegment<byte>(buffer, 0, receiveResult.Count));

                    while (!receiveResult.EndOfMessage)
                    {
                        receiveResult = await socket.ReceiveAsync(buffer, linkedToken).ConfigureAwait(false);
                        allBytes.AddRange(new ArraySegment<byte>(buffer, 0, receiveResult.Count));
                    }

                    await channel.Writer.WriteAsync(Encoding.UTF8.GetString(allBytes.ToArray()), linkedToken).ConfigureAwait(false);
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
        if (socket.State is not WebSocketState.Open && socket.State is not WebSocketState.CloseReceived)
        {
            return false;
        }

        var messageBytes = Encoding.UTF8.GetBytes(message);

        await socket.SendAsync(
            new(messageBytes, 0, messageBytes.Length),
            WebSocketMessageType.Text,
            true,
            cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async Task Close(CancellationToken cancellationToken)
    {
        if (socket.State is not WebSocketState.Open && socket.State is not WebSocketState.CloseReceived && socket.State is not WebSocketState.CloseSent)
        {
            return;
        }

        cancellationTokenSource.Cancel();

        try
        {
            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                nameof(WebSocketCloseStatus.NormalClosure),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or IOException { InnerException: ObjectDisposedException })
        {
            // if closing the connection fails due to a disposed object, we consider
            // the closing successful; this can for example happen with the ASP Core
            // `TestWebSocket` class which can throw this error when server and client
            // close the connection at the same time
        }
    }
}
