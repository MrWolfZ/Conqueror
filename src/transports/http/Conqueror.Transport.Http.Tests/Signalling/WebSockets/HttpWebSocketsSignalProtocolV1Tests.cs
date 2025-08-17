namespace Conqueror.Transport.Http.Tests.Signalling.WebSockets;

using Conqueror.Signalling.WebSockets;

[TestFixture]
public sealed class HttpWebSocketsSignalProtocolV1Tests
{
    private const string Tag = "testTag";

    private const string Content = "{\"test\":\"value\"}";

    private const string ContextData = "d|foo=bar||b|baz=biz";

    [Test]
    [Combinatorial]
    public async Task GivenSignal_WhenWritingToAndReadingFromStreamWithProtocol_ReturnsCorrectResult(
        [Values(Content)] string content,
        [Values(arg1: null, ContextData)] string? contextData
    )
    {
        await using var ms = new FlushResetMemoryStream();

        await HttpWebSocketsSignalProtocolV1.Write(
            ms,
            Tag,
            contextData,
            (s, ct) => s.WriteAsync(Encoding.UTF8.GetBytes(content), ct),
            CancellationToken.None
        );

        Assert.That(ms.Position, Is.EqualTo(expected: 0));

        var (signal, data) = await HttpWebSocketsSignalProtocolV1.Read(
            ms,
            async (tag, s, ct) =>
            {
                Assert.That(tag, Is.EqualTo(Tag));

                var buffer = new byte[1024];
                var read = await s.ReadAsync(buffer, ct);

                Assert.That(read, Is.EqualTo(Encoding.UTF8.GetBytes(content).Length));

                return Encoding.UTF8.GetString(buffer[..read]);
            },
            CancellationToken.None
        );

        Assert.That(signal, Is.EqualTo(Content));
        Assert.That(data, Is.EqualTo(contextData));
    }

    [Test]
    public async Task GivenStreamWithWrongVersion_WhenReadingFromStreamWithProtocol_ThrowsException()
    {
        await using var ms = new FlushResetMemoryStream();

        await ms.WriteAsync(" "u8.ToArray(), CancellationToken.None);
        await ms.FlushAsync(CancellationToken.None);

        await Assert.ThatAsync(
            () =>
                HttpWebSocketsSignalProtocolV1.Read(
                    ms,
                    (_, _, _) =>
                    {
                        Assert.Fail("should not happen");

                        return default;
                    },
                    CancellationToken.None
                ),
            Throws.InvalidOperationException.With.Message.Contains("Unsupported protocol version")
        );
    }

    [Test]
    public async Task GivenStreamWithInvalidHeaderLength_WhenReadingFromStreamWithProtocol_ThrowsException()
    {
        await using var ms = new FlushResetMemoryStream();

        await ms.WriteAsync(new byte[] { 1 << 4, 0, 0, 1 }, CancellationToken.None);
        await ms.FlushAsync(CancellationToken.None);

        await Assert.ThatAsync(
            () =>
                HttpWebSocketsSignalProtocolV1.Read(
                    ms,
                    (_, _, _) =>
                    {
                        Assert.Fail("should not happen");

                        return default;
                    },
                    CancellationToken.None
                ),
            Throws.InvalidOperationException.With.Message.Contains("Invalid header length")
        );
    }

    private sealed class FlushResetMemoryStream : MemoryStream
    {
        public override void Flush() => Position = 0;
    }
}
