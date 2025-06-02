using System.Diagnostics;
using System.Text;
using Conqueror.Signalling.WebSockets;
using Conqueror.Transport.Http.Client.WebSockets;
using static Conqueror.Transport.Http.Tests.HttpTestContextData;
using static Conqueror.Transport.Http.Tests.Signalling.HttpTestSignals;

namespace Conqueror.Transport.Http.Tests.Signalling.WebSockets.Server;

[TestFixture]
public sealed class SignallingHttpWebSocketsServerContextTests
{
    [Test]
    [Retry(3)] // fix some flakiness has been observed in GitHub Actions due to delays in subscribing to signals
    [TestCaseSource(nameof(GenerateContextDataTestCases))]
    public async Task GivenContextData_WhenPublishingHttpWebSocketsSignal_DataIsCorrectlySent(
        bool hasDownstream,
        bool hasBidirectional,
        bool hasActivity)
    {
        await using var host = await HttpTransportTestHost.Create(
            services => services.AddConquerorHttpServerAspNetCore()
                                .AddSingleton<TestObservations>()
                                .AddTransient(typeof(TestSignalMiddleware<>))
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

        DisposableActivity? activity = null;

        var traceId = ActivityTraceId.CreateRandom().ToString();

        if (hasActivity)
        {
            activity = DisposableActivity.Create(nameof(GivenContextData_WhenPublishingHttpWebSocketsSignal_DataIsCorrectlySent));
            _ = activity.Activity.Start();
            traceId = activity.TraceId;
        }

        using var d = activity;

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();

        if (!hasActivity)
        {
            conquerorContext.SetTraceId(traceId);
        }

        if (hasDownstream)
        {
            foreach (var (key, value) in ContextData)
            {
                conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            }

            foreach (var (key, value) in InProcessContextData)
            {
                conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.InProcess);
            }
        }

        if (hasBidirectional)
        {
            foreach (var (key, value) in ContextData)
            {
                conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            }

            foreach (var (key, value) in InProcessContextData)
            {
                conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.InProcess);
            }
        }

        List<string?> seenSignalIdsOnPublisher = [];

        var handler = host.Resolve<ISignalPublishers>()
                          .For(TestSignal.T)
                          .WithTransport(b =>
                          {
                              seenSignalIdsOnPublisher.Add(b.ConquerorContext.GetSignalId());

                              return b.UseHttpWebSockets();
                          });

        await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);
        await handler.Handle(new() { Payload = 20 }, host.TestTimeoutToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(host.IsRunningInGithubAction ? 10_000 : 20));

        var result = new List<(string? Tag, string? ContextData)>();

        try
        {
            await foreach (var stream in conquerorWebSocket.Read(cts.Token))
            {
                string? tag = null;

                var (_, contextData) = await HttpWebSocketSignalProtocolV1.Read(
                    stream,
                    async (t, s, ct) =>
                    {
                        tag = t;

                        var buffer = new byte[1024];
                        var read = await s.ReadAsync(buffer, ct);

                        return Encoding.UTF8.GetString(buffer[..read]);
                    },
                    cts.Token);

                result.Add((tag, contextData));
            }
        }
        catch (OperationCanceledException)
        {
            // nothing to do
        }

        Assert.That(result, Has.Count.EqualTo(2));

        foreach (var item in result)
        {
            Assert.That(item.ContextData, Is.Not.Null);

            using var ctx = host.Resolve<IConquerorContextAccessor>().CloneOrCreate();
            ctx.DecodeContextData(item.ContextData);

            Assert.That(seenSignalIdsOnPublisher, Contains.Item(ctx.GetSignalId()));
            Assert.That(ctx.GetTraceId(), Is.EqualTo(traceId));

            if (hasDownstream)
            {
                Assert.That(ContextData, Is.SubsetOf(ctx.DownstreamContextData.AsKeyValuePairs<string>()));
            }
            else
            {
                Assert.That(ctx.DownstreamContextData.WhereScopeIsAcrossTransports().Intersect(ContextData), Is.Empty);
            }

            if (hasBidirectional)
            {
                Assert.That(ContextData, Is.SubsetOf(ctx.ContextData.AsKeyValuePairs<string>()));
            }
            else
            {
                Assert.That(ctx.ContextData.WhereScopeIsAcrossTransports().Intersect(ContextData), Is.Empty);
            }
        }
    }

    private static IEnumerable<TestCaseData> GenerateContextDataTestCases()
    {
        return from hasDownstream in new[] { true, false }
               from hasBidirectional in new[] { true, false }
               from hasActivity in new[] { true, false }
               select new TestCaseData(hasDownstream, hasBidirectional, hasActivity);
    }
}
