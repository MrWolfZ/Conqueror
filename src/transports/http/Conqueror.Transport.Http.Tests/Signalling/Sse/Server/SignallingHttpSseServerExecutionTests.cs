using System.Net.ServerSentEvents;
using static Conqueror.Transport.Http.Tests.Signalling.HttpTestSignals;

namespace Conqueror.Transport.Http.Tests.Signalling.Sse.Server;

[TestFixture]
public sealed partial class SignallingHttpSseServerExecutionTests
{
    [Test]
    [Retry(2)] // fix some flakiness due to time-based cancellation
    [TestCaseSource(typeof(HttpTestSignals), nameof(GenerateTestCaseData))]
    public async Task GivenTestHttpSseSignal_WhenSubscribingToSignals_ReturnsCorrectEventStream(HttpSignalTestCase testCase)
    {
        await using var host = await HttpTransportTestHost.Create(
            testCase.RegisterServerServices,
            app => app.MapSignalEndpoints());

        var targetUriBuilder = new UriBuilder(SseAddress)
        {
            Query = testCase.QueryString,
        };

        using var request = new HttpRequestMessage(new("GET"), targetUriBuilder.Uri);

        using var response = await host.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, host.TestTimeoutToken);

        if (string.IsNullOrWhiteSpace(testCase.QueryString))
        {
            await response.AssertStatusCode(StatusCodes.Status400BadRequest);

            return;
        }

        await response.AssertSuccessStatusCode();
        var contentType = response.Content.Headers.TryGetValues("Content-Type", out var ct) && ct.FirstOrDefault() is { } cs ? cs : null;

        Assert.That(contentType, Is.EqualTo("text/event-stream"));

        await testCase.PublishSignals(host.Resolve<ISignalPublishers>());

        var responseStream = await response.Content.ReadAsStreamAsync(host.TestTimeoutToken);

        var parser = SseParser.Create(responseStream);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(Environment.GetEnvironmentVariable("GITHUB_ACTION") is null ? 20 : 10_000));

        var result = new List<(string EventType, string Data)>();

        try
        {
            await foreach (var item in parser.EnumerateAsync(cts.Token))
            {
                result.Add((item.EventType, item.Data));
            }
        }
        catch (OperationCanceledException)
        {
            // nothing to do
        }

        Assert.That(result, Has.Count.EqualTo(testCase.ExpectedEventTypes.Count));
        Assert.That(result.Select(r => r.EventType), Is.EqualTo(testCase.ExpectedEventTypes));
        Assert.That(result.Select(r => r.Data.Split("\n")[0]), Is.EqualTo(testCase.ExpectedPayloads));
    }

    [Test]
    public async Task GivenTestHttpSseSignal_WhenPublishThrowsException_ReturnsExceptionToCaller()
    {
        var exception = new InvalidOperationException("test exception");

        await using var host = await HttpTransportTestHost.Create(
            services =>
                services.AddConqueror()
                        .AddSingleton(exception)
                        .AddRouting(),
            app => app.MapSignalEndpoints());

        var targetUriBuilder = new UriBuilder(SseAddress) { Query = "?signalTypes=throwingTest" };
        using var request = new HttpRequestMessage(new("GET"), targetUriBuilder.Uri);

        using var response = await host.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, host.TestTimeoutToken);
        await response.AssertSuccessStatusCode();

        await Assert.ThatAsync(
            () => host.Resolve<ISignalPublishers>()
                      .For(ThrowingTestSignal.T)
                      .WithTransport(b => b.UseHttpServerSentEvents())
                      .Handle(new(), host.TestTimeoutToken),
            Throws.Exception.SameAs(exception));
    }

    [Test]
    public async Task GivenTestHttpSseSignalWithSingleSubscriber_WhenCancellingPublish_ThrowsOperationCanceledException()
    {
        await using var host = await HttpTransportTestHost.Create(
            services =>
                services.AddConqueror()
                        .AddSingleton<TestObservations>()
                        .AddRouting(),
            app => app.MapSignalEndpoints());

        var targetUriBuilder = new UriBuilder(SseAddress) { Query = "?signalTypes=test" };
        using var request = new HttpRequestMessage(new("GET"), targetUriBuilder.Uri);

        using var response = await host.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, host.TestTimeoutToken);
        await response.AssertSuccessStatusCode();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        var token = cts.Token;

        var publishTask = host.Resolve<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithTransport(b => b.UseHttpServerSentEvents())
                              .Handle(new() { Payload = 10 }, token);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () => publishTask,
            Throws.Exception.TypeOf<OperationCanceledException>());
    }

    [Test]
    public async Task GivenTestHttpSseSignalWithMultipleSubscribers_WhenCancellingPublish_ThrowsOperationCanceledException()
    {
        await using var host = await HttpTransportTestHost.Create(
            services =>
                services.AddConqueror()
                        .AddSingleton<TestObservations>()
                        .AddRouting(),
            app => app.MapSignalEndpoints());

        var targetUriBuilder = new UriBuilder(SseAddress) { Query = "?signalTypes=test" };
        using var request1 = new HttpRequestMessage(new("GET"), targetUriBuilder.Uri);
        using var request2 = new HttpRequestMessage(new("GET"), targetUriBuilder.Uri);

        using var response1 = await host.HttpClient.SendAsync(request1, HttpCompletionOption.ResponseHeadersRead, host.TestTimeoutToken);
        await response1.AssertSuccessStatusCode();

        using var response2 = await host.HttpClient.SendAsync(request2, HttpCompletionOption.ResponseHeadersRead, host.TestTimeoutToken);
        await response2.AssertSuccessStatusCode();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        var token = cts.Token;

        var publishTask = host.Resolve<ISignalPublishers>()
                              .For(TestSignal.T)
                              .WithTransport(b => b.UseHttpServerSentEvents())
                              .Handle(new() { Payload = 10 }, token);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () => publishTask,
            Throws.Exception.TypeOf<OperationCanceledException>());
    }

    [HttpSseSignal]
    private sealed partial record ThrowingTestSignal
    {
        static IHttpSseSignalSerializer<ThrowingTestSignal> IHttpSseSignal<ThrowingTestSignal>.HttpSseSignalSerializer { get; }
            = new ThrowingTestSignalSerializer();
    }

    private sealed class ThrowingTestSignalSerializer : IHttpSseSignalSerializer<ThrowingTestSignal>
    {
        public string Serialize(IServiceProvider serviceProvider, ThrowingTestSignal signal)
        {
            throw serviceProvider.GetRequiredService<InvalidOperationException>();
        }

        public ThrowingTestSignal Deserialize(IServiceProvider serviceProvider, string serializedSignal)
        {
            throw new NotSupportedException();
        }
    }
}
