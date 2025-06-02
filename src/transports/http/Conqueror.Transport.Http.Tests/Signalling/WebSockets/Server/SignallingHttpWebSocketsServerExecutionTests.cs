using System.Text;
using Conqueror.Signalling.WebSockets;
using Conqueror.Transport.Http.Client.WebSockets;
using static Conqueror.Transport.Http.Tests.Signalling.HttpTestSignals;

namespace Conqueror.Transport.Http.Tests.Signalling.WebSockets.Server;

[TestFixture]
public sealed partial class SignallingHttpWebSocketsServerExecutionTests
{
    [Test]
    [TestCaseSource(typeof(HttpTestSignals), nameof(GenerateTestCaseData), [TransportType.WebSockets])]
    [SuppressMessage(
        "Structure",
        "NUnit1018:The number of parameters provided by the TestCaseSource does not match the number of parameters in the target method",
        Justification = "false positive, the analyzer does not yet recognize collection expressions")]
    public async Task GivenTestHttpWebSocketsSignal_WhenSubscribingToSignals_ReturnsCorrectEventStream(HttpSignalTestCase testCase)
    {
        await using var host = await HttpTransportTestHost.Create(
            services => testCase.RegisterServerServices(services.AddConquerorHttpServerAspNetCore()),
            app => app.MapSignalEndpoints());

        var targetUriBuilder = new UriBuilder(WebSocketsAddress)
        {
            Query = QueryStringBuilder.Create(testCase.QueryString)
                                      .Add(QueryParameterNames.HeartbeatInterval, "10")
                                      .Add(QueryParameterNames.HeartbeatTimeout, "60")
                                      .Build(),
        };

        if (string.IsNullOrWhiteSpace(testCase.QueryString))
        {
            await Assert.ThatAsync(
                () => host.ConnectToWebSocket(targetUriBuilder.Uri),
                Throws.InvalidOperationException.With.Message.Contains($"status code: {StatusCodes.Status400BadRequest}"));

            return;
        }

        using var webSocket = await host.ConnectToWebSocket(targetUriBuilder.Uri);

        await using var conquerorWebSocket = new ConquerorWebSocket(webSocket, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var result = new List<(string? Tag, string Data)>();

        async Task Run(ConquerorWebSocket ws, CancellationToken token)
        {
            try
            {
                await foreach (var stream in ws.Read(token))
                {
                    string? tag = null;

                    var (data, _) = await HttpWebSocketSignalProtocolV1.Read(
                        stream,
                        async (t, s, ct) =>
                        {
                            tag = t;

                            var buffer = new byte[1024];
                            var read = await s.ReadAsync(buffer, ct);

                            return Encoding.UTF8.GetString(buffer[..read]);
                        },
                        token);

                    result.Add((tag, (string)data));
                }
            }
            catch (OperationCanceledException)
            {
                // nothing to do
            }
        }

        var runTask = Run(conquerorWebSocket, cts.Token);

        await testCase.PublishSignals(host.Resolve<ISignalPublishers>());

        // give the client time to receive the signals
        await Task.Delay(TimeSpan.FromMilliseconds(host.IsRunningInGithubAction ? 10_000 : 100), host.TestTimeoutToken);

        await cts.CancelAsync();

        await runTask;

        Assert.That(result, Has.Count.EqualTo(testCase.ExpectedEventTypesOrTags.Count));
        Assert.That(result.Select(r => r.Tag), Is.EqualTo(testCase.ExpectedEventTypesOrTags));
        Assert.That(result.Select(r => r.Data.Split("\n")[0]), Is.EqualTo(testCase.ExpectedPayloads));
    }

    [Test]
    public async Task GivenTestHttpWebSocketsSignal_WhenPublishThrowsException_ReturnsExceptionToCaller()
    {
        var exception = new InvalidOperationException("test exception");

        await using var host = await HttpTransportTestHost.Create(
            services =>
                services.AddConquerorHttpServerAspNetCore()
                        .AddSingleton(exception)
                        .AddRouting(),
            app => app.MapSignalEndpoints());

        var targetUriBuilder = new UriBuilder(WebSocketsAddress)
        {
            Query = QueryStringBuilder.Of(
                (QueryParameterNames.SignalWebSocketsTag, "throwingTest"),
                (QueryParameterNames.HeartbeatInterval, "10"),
                (QueryParameterNames.HeartbeatTimeout, "60")),
        };

        using var webSocket = await host.ConnectToWebSocket(targetUriBuilder.Uri);

        await using var conquerorWebSocket = new ConquerorWebSocket(webSocket, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));

        await Assert.ThatAsync(
            () => host.Resolve<ISignalPublishers>()
                      .For(ThrowingTestSignal.T)
                      .WithTransport(b => b.UseHttpWebSockets())
                      .Handle(new(), host.TestTimeoutToken),
            Throws.Exception.SameAs(exception));
    }

    [Test]
    [Retry(3)] // fix some flakiness that has been observed due to race condition between publish and cancellation
    public async Task GivenTestHttpWebSocketsSignalWithSingleSubscriber_WhenCancellingPublish_ThrowsOperationCanceledException()
    {
        await using var host = await HttpTransportTestHost.Create(
            services =>
                services.AddConquerorHttpServerAspNetCore()
                        .AddSingleton<TestObservations>()
                        .AddRouting(),
            app => app.MapSignalEndpoints());

        var targetUriBuilder = new UriBuilder(WebSocketsAddress)
        {
            Query = QueryStringBuilder.Of(
                (QueryParameterNames.SignalWebSocketsTag, "test"),
                (QueryParameterNames.HeartbeatInterval, "10"),
                (QueryParameterNames.HeartbeatTimeout, "60")),
        };

        using var webSocket = await host.ConnectToWebSocket(targetUriBuilder.Uri);

        await using var conquerorWebSocket = new ConquerorWebSocket(webSocket, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        var token = cts.Token;

        var publishTask = host.Resolve<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPipeline(p => p.Use(async ctx =>
                              {
                                  // yield to force async execution
                                  await Task.Yield();
                                  await ctx.Next(ctx.Signal, ctx.CancellationToken);
                              }))
                              .WithTransport(b => b.UseHttpWebSockets())
                              .Handle(new() { Payload = 10 }, token);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () => publishTask,
            Throws.Exception.TypeOf<OperationCanceledException>());
    }

    [Test]
    [Retry(3)] // fix some flakiness that has been observed due to race condition between publish and cancellation
    public async Task GivenTestHttpWebSocketsSignalWithMultipleSubscribers_WhenCancellingPublish_ThrowsOperationCanceledException()
    {
        await using var host = await HttpTransportTestHost.Create(
            services =>
                services.AddConquerorHttpServerAspNetCore()
                        .AddSingleton<TestObservations>()
                        .AddRouting(),
            app => app.MapSignalEndpoints());

        var targetUriBuilder = new UriBuilder(WebSocketsAddress)
        {
            Query = QueryStringBuilder.Of(
                (QueryParameterNames.SignalWebSocketsTag, "test"),
                (QueryParameterNames.HeartbeatInterval, "10"),
                (QueryParameterNames.HeartbeatTimeout, "60")),
        };

        using var webSocket1 = await host.ConnectToWebSocket(targetUriBuilder.Uri);

        await using var conquerorWebSocket1 = new ConquerorWebSocket(webSocket1, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));

        using var webSocket2 = await host.ConnectToWebSocket(targetUriBuilder.Uri);

        await using var conquerorWebSocket2 = new ConquerorWebSocket(webSocket2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        var token = cts.Token;

        var publishTask = host.Resolve<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithPipeline(p => p.Use(async ctx =>
                              {
                                  // yield to force async execution
                                  await Task.Yield();
                                  await ctx.Next(ctx.Signal, ctx.CancellationToken);
                              }))
                              .WithTransport(b => b.UseHttpWebSockets())
                              .Handle(new() { Payload = 10 }, token);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () => publishTask,
            Throws.Exception.TypeOf<OperationCanceledException>());
    }

    [HttpWebSocketsSignal]
    private sealed partial record ThrowingTestSignal
    {
        static IHttpWebSocketsSignalSerializer<ThrowingTestSignal> IHttpWebSocketsSignal<ThrowingTestSignal>.HttpWebSocketsSignalSerializer { get; }
            = new ThrowingTestSignalSerializer();
    }

    private sealed class ThrowingTestSignalSerializer : IHttpWebSocketsSignalSerializer<ThrowingTestSignal>
    {
        public ValueTask Serialize(
            IServiceProvider serviceProvider,
            ThrowingTestSignal signal,
            Stream stream,
            CancellationToken cancellationToken)
        {
            throw serviceProvider.GetRequiredService<InvalidOperationException>();
        }

        public ValueTask<ThrowingTestSignal> Deserialize(IServiceProvider serviceProvider, Stream stream, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
