using System.Net.ServerSentEvents;
using Microsoft.AspNetCore.Http;
using static Conqueror.Transport.Http.Tests.Signalling.HttpTestSignals;

namespace Conqueror.Transport.Http.Tests.Signalling.Sse.Server;

[TestFixture]
public sealed class SignallingHttpSseServerExecutionTests
{
    [Test]
    [Retry(2)] // fix some flakiness due to time-based cancellation
    [TestCaseSource(typeof(HttpTestSignals), nameof(GenerateTestCaseData))]
    public async Task GivenTestHttpSseSignal_WhenExecutingSignal_ReturnsCorrectEventStream(HttpSignalTestCase testCase)
    {
        await using var host = await HttpTransportTestHost.Create(
            testCase.RegisterServerServices,
            app => app.MapSignalEndpoints());

        var targetUriBuilder = new UriBuilder(SseAddress)
        {
            Query = testCase.QueryString,
        };

        using var request = new HttpRequestMessage(new("GET"), targetUriBuilder.Uri);

        var response = await host.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, host.TestTimeoutToken);

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
        cts.CancelAfter(TimeSpan.FromMilliseconds(Environment.GetEnvironmentVariable("GITHUB_ACTION") is null ? 20 : 2_000));

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
        Assert.That(result.Select(r => r.Data.Split("\n")[^1]), Is.EqualTo(testCase.ExpectedPayloads));
    }
}
