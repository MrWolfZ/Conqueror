using System.Diagnostics;
using System.Net.ServerSentEvents;
using static Conqueror.Transport.Http.Tests.HttpTestContextData;
using static Conqueror.Transport.Http.Tests.Signalling.HttpTestSignals;

namespace Conqueror.Transport.Http.Tests.Signalling.Sse.Server;

[TestFixture]
public sealed class SignallingHttpSseServerContextTests
{
    [Test]
    [Retry(3)] // fix some flakiness has been observed in GitHub Actions due to delays in subscribing to signals
    [TestCaseSource(nameof(GenerateContextDataTestCases))]
    public async Task GivenContextData_WhenPublishingHttpSseSignal_DataIsCorrectlySent(
        bool hasDownstream,
        bool hasBidirectional,
        bool hasActivity)
    {
        await using var host = await HttpTransportTestHost.Create(
            services => services.AddConqueror()
                                .AddSingleton<TestObservations>()
                                .AddTransient(typeof(TestSignalMiddleware<>))
                                .AddRouting(),
            app => app.MapSignalEndpoints());

        var targetUriBuilder = new UriBuilder(SseAddress) { Query = "?signalTypes=test" };

        using var request = new HttpRequestMessage(new("GET"), targetUriBuilder.Uri);

        using var response = await host.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, host.TestTimeoutToken);

        await response.AssertSuccessStatusCode();

        DisposableActivity? activity = null;

        var traceId = ActivityTraceId.CreateRandom().ToString();

        if (hasActivity)
        {
            activity = DisposableActivity.Create(nameof(GivenContextData_WhenPublishingHttpSseSignal_DataIsCorrectlySent));
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

                              return b.UseHttpServerSentEvents();
                          });

        await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);
        await handler.Handle(new() { Payload = 20 }, host.TestTimeoutToken);

        var responseStream = await response.Content.ReadAsStreamAsync(host.TestTimeoutToken);

        var parser = SseParser.Create(responseStream);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(Environment.GetEnvironmentVariable("GITHUB_ACTION") is null ? 20 : 10_000));

        var result = new List<SseItem<string>>();

        try
        {
            await foreach (var item in parser.EnumerateAsync(cts.Token))
            {
                result.Add(item);
            }
        }
        catch (OperationCanceledException)
        {
            // nothing to do
        }

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(i => i.EventId), Is.EqualTo(seenSignalIdsOnPublisher));

        foreach (var item in result)
        {
            using var ctx = host.Resolve<IConquerorContextAccessor>().CloneOrCreate();
            ctx.DecodeContextData(item.Data.Split("\n")[1]);

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
