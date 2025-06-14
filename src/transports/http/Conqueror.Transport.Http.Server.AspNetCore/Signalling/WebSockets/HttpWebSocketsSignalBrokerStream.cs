using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Transport.Http.Client.WebSockets;

namespace Conqueror.Transport.Http.Server.AspNetCore.Signalling.WebSockets;

// helper class to deal with the fact that we want to subscribe streams to
// brokers before the web socket connection is accepted, but we can only get
// the stream once the socket has been accepted
internal sealed class HttpWebSocketsSignalBrokerStream(Task<WebSocketWriteStream> streamTask) : Stream
{
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
        => throw new NotSupportedException("This stream does not support synchronous flushing");

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        Stream? stream;

        try
        {
            stream = await streamTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // the client disconnected before the web socket was accepted
            return;
        }

        try
        {
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (WebSocketException e) when (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            // nothing to do here, the client disconnected in the middle of sending a signal
        }
        catch (ObjectDisposedException)
        {
            // nothing to do here, the client disconnected in the middle of sending a signal
        }
        catch (IOException iex) when (iex.InnerException is ObjectDisposedException)
        {
            // nothing to do here, the client disconnected in the middle of sending a signal
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("This stream does not support reading");

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException("This stream does not support seeking");

    public override void SetLength(long value)
        => throw new NotSupportedException("This stream does not support setting length");

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("This stream does not support synchronous writing");

    public override void Write(ReadOnlySpan<byte> buffer)
        => throw new NotSupportedException("This stream does not support synchronous writing");

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        Stream? stream;

        try
        {
            stream = await streamTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // the client disconnected before the web socket was accepted
            return;
        }

        try
        {
            await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (WebSocketException e) when (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            // nothing to do here, the client disconnected in the middle of sending a signal
        }
        catch (ObjectDisposedException)
        {
            // nothing to do here, the client disconnected in the middle of sending a signal
        }
        catch (IOException iex) when (iex.InnerException is ObjectDisposedException)
        {
            // nothing to do here, the client disconnected in the middle of sending a signal
        }
    }
}
