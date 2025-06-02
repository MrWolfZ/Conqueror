using System.Net.WebSockets;
using System.Text;
using Conqueror.Transport.Http.Client.WebSockets;
using Conqueror.Transport.Http.Server.AspNetCore.WebSockets;

namespace Conqueror.Transport.Http.Tests.WebSockets;

[TestFixture]
public sealed class WebSocketTests
{
    private const string Content1 = "{\"test\":\"value\"}";
    private const string Content2 = "{\"test\":\"value\"}";

    [Test]
    public async Task GivenSocketEndpoint_WhenWritingToAndReadingFromSocket_EverythingWorks()
    {
        await using var host = await CreateHost(async (s, logger, ct) =>
        {
            await using var d = ct.Register(() => logger.LogTrace("canceled"));

            logger.LogTrace("writing message 1");

            await s.WriteAsync(Encoding.UTF8.GetBytes(Content1), ct);
            await s.FlushAsync(ct);

            logger.LogTrace("writing message 2");

            await s.WriteAsync(Encoding.UTF8.GetBytes(Content2), ct);
            await s.FlushAsync(ct);

            logger.LogTrace("sleeping");
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        });

        var query = QueryStringBuilder.Of((QueryParameterNames.HeartbeatInterval, "10"), (QueryParameterNames.HeartbeatTimeout, "60"));
        using var webSocket = await host.ConnectToWebSocket(new($"ws://localhost{query}"));

        await using var conquerorWebSocket = new ConquerorWebSocket(webSocket, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));

        var logger = host.Resolve<ILogger<WebSocketTests>>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var result = new List<string>();

        async Task Run(ConquerorWebSocket ws, CancellationToken token)
        {
            try
            {
                await foreach (var stream in ws.Read(token))
                {
                    logger.LogTrace("reading message");

                    using var streamReader = new StreamReader(stream, Encoding.UTF8);

                    var res = await streamReader.ReadToEndAsync(token);
                    result.Add(res);
                }
            }
            catch (OperationCanceledException)
            {
                // nothing to do
            }
            catch (ObjectDisposedException)
            {
                // nothing to do
            }
        }

        var runTask = Run(conquerorWebSocket, cts.Token);

        // give the client time to receive the signals
        await Task.Delay(TimeSpan.FromMilliseconds(host.IsRunningInGithubAction ? 10_000 : 100), host.TestTimeoutToken);

        await cts.CancelAsync();

        await runTask;

        Assert.That(result, Is.EqualTo(new[] { Content1, Content2 }));
    }

    [Test]
    [TestCase(null, "60")]
    [TestCase("60", null)]
    [TestCase("", "60")]
    [TestCase("60", "")]
    [TestCase("foo", "60")]
    [TestCase("60", "foo")]
    [TestCase("-1", "60")]
    [TestCase("60", "-1")]
    [TestCase("601", "60")]
    [TestCase("60", "601")]
    [TestCase("10.5", "60")]
    [TestCase("60", "60.5")]
    [TestCase("10", "5")]
    public async Task GivenSocketEndpoint_WhenSendingInvalidHeartbeatParameters_ReturnsBadRequest(
        string? heartbeatInterval,
        string? heartbeatTimeout)
    {
        await using var host = await CreateHost(async (_, _, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        });

        var qb = QueryStringBuilder.Create();

        if (heartbeatInterval is not null)
        {
            qb = qb.Add(QueryParameterNames.HeartbeatInterval, heartbeatInterval);
        }

        if (heartbeatTimeout is not null)
        {
            qb = qb.Add(QueryParameterNames.HeartbeatTimeout, heartbeatTimeout);
        }

        await Assert.ThatAsync(
            () => host.ConnectToWebSocket(new($"ws://localhost{qb.Build()}")),
            Throws.InvalidOperationException.With.Message.Contains($"status code: {StatusCodes.Status400BadRequest}"));
    }

    [Test]
    public async Task GivenSocketEndpoint_WhenSendingRequestWithHeartbeatInterval_SocketsSendHeartbeats()
    {
        await using var host = await CreateHost(
            async (_, _, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            },
            (TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(50)));

        using var webSocket = await host.ConnectToWebSocket(new("ws://localhost"));

        await using var conquerorWebSocket = new ConquerorWebSocket(webSocket, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(50));

        var logger = host.Resolve<ILogger<WebSocketTests>>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        async Task Run(ConquerorWebSocket ws, CancellationToken token)
        {
            try
            {
                await foreach (var unused in ws.Read(token))
                {
                    logger.LogTrace("reading message");
                }
            }
            catch (OperationCanceledException)
            {
                // nothing to do
            }
            catch (ObjectDisposedException)
            {
                // nothing to do
            }
        }

        var runTask = Run(conquerorWebSocket, cts.Token);

        // give the client time to receive the heartbeats
        await Task.Delay(TimeSpan.FromMilliseconds(100), host.TestTimeoutToken);

        Assert.That(conquerorWebSocket.State, Is.EqualTo(WebSocketState.Open));

        await cts.CancelAsync();

        await runTask;
    }

    [Test]
    public async Task GivenSocketEndpoint_WhenServerDoesNotSendHeartbeat_ClientClosesSocket()
    {
        await using var host = await CreateHost(
            async (_, _, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            },
            (TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60)));

        using var webSocket = await host.ConnectToWebSocket(new("ws://localhost"));

        await using var conquerorWebSocket = new ConquerorWebSocket(webSocket, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(50));

        var logger = host.Resolve<ILogger<WebSocketTests>>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        async Task Run(ConquerorWebSocket ws, CancellationToken token)
        {
            try
            {
                await foreach (var unused in ws.Read(token))
                {
                    logger.LogTrace("reading message");
                }
            }
            catch (OperationCanceledException)
            {
                // nothing to do
            }
            catch (ObjectDisposedException)
            {
                // nothing to do
            }
        }

        var runTask = Run(conquerorWebSocket, cts.Token);

        // give the client time to time out the heartbeat
        await Task.Delay(TimeSpan.FromMilliseconds(100), host.TestTimeoutToken);

        Assert.That(conquerorWebSocket.State, Is.EqualTo(WebSocketState.Closed));

        await cts.CancelAsync();

        await runTask;
    }

    [Test]
    public async Task GivenSocketEndpoint_WhenClientDoesNotSendHeartbeat_ServerClosesSocket()
    {
        await using var host = await CreateHost(
            async (_, _, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            },
            (TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(50)));

        using var webSocket = await host.ConnectToWebSocket(new("ws://localhost"));

        await using var conquerorWebSocket = new ConquerorWebSocket(webSocket, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60));

        var logger = host.Resolve<ILogger<WebSocketTests>>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        async Task Run(ConquerorWebSocket ws, CancellationToken token)
        {
            try
            {
                await foreach (var unused in ws.Read(token))
                {
                    logger.LogTrace("reading message");
                }
            }
            catch (OperationCanceledException)
            {
                // nothing to do
            }
            catch (ObjectDisposedException)
            {
                // nothing to do
            }
        }

        var runTask = Run(conquerorWebSocket, cts.Token);

        // give the server time to time out the heartbeat
        await Task.Delay(TimeSpan.FromMilliseconds(100), host.TestTimeoutToken);

        Assert.That(conquerorWebSocket.State, Is.EqualTo(WebSocketState.Closed));

        await cts.CancelAsync();

        await runTask;
    }

    private static async Task<HttpTransportTestHost> CreateHost(
        Func<Stream, ILogger, CancellationToken, Task> handler,
        (TimeSpan HeartbeatInterval, TimeSpan HeartbeatTimeout)? heartbeat = null)
    {
        var host = await HttpTransportTestHost.Create(
            services =>
            {
                _ = services.AddRouting();
            },
            app =>
            {
                _ = app.UseRouting()
                       .UseWebSockets();

                _ = app.UseEndpoints(endpoints =>
                {
                    _ = endpoints.MapGet(
                        "/",
                        async context =>
                        {
                            var logger = context.RequestServices.GetRequiredService<ILogger<WebSocketTests>>();

                            var heartbeatInterval = heartbeat?.HeartbeatInterval ?? Timeout.InfiniteTimeSpan;
                            var heartbeatTimeout = heartbeat?.HeartbeatTimeout ?? Timeout.InfiniteTimeSpan;

                            if (heartbeat is null
                                && !WebSocketEndpoint.TryGetHeartbeatParameters(
                                    context,
                                    out heartbeatInterval,
                                    out heartbeatTimeout,
                                    out _))
                            {
                                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                                return;
                            }

                            var tcs = new TaskCompletionSource<WebSocketWriteStream>();

                            async Task RunHandler()
                            {
                                var s = await tcs.Task;
                                await handler(s, logger, context.RequestAborted);
                            }

                            _ = await Task.WhenAny(
                                WebSocketEndpoint.Run(
                                    context,
                                    logger,
                                    heartbeatInterval,
                                    heartbeatTimeout,
                                    s => tcs.TrySetResult(s)),
                                RunHandler());

                            _ = tcs.TrySetCanceled();
                        });
                });
            });

        return host;
    }
}
