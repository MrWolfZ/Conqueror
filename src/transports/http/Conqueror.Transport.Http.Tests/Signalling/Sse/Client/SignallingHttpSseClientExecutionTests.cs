using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using static Conqueror.Transport.Http.Tests.Signalling.HttpTestSignals;

namespace Conqueror.Transport.Http.Tests.Signalling.Sse.Client;

[TestFixture]
[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
public sealed class SignallingHttpSseClientExecutionTests
{
    private const string AuthorizationHeaderScheme = "Basic";
    private const string AuthorizationHeaderValue = "username:password";
    private const string AuthorizationHeader = $"{AuthorizationHeaderScheme} {AuthorizationHeaderValue}";
    private const string TestHeaderValue = "test-value";

    private bool serverResponseHasBegun;
    private IHeaderDictionary? receivedHeadersOnServer;

    [Test]
    [TestCaseSource(typeof(HttpTestSignals), nameof(GenerateTestCaseData))]
    public async Task GivenTestHttpSseSignal_WhenRunningReceivers_ReceiversReceiveCorrectSignals(HttpSignalTestCase testCase)
    {
        await using var host = await CreateTestHost(
            testCase.RegisterServerServices,
            app => app.MapSignalEndpoints());

        var httpClient = host.HttpClient;

        var clientServices = testCase.RegisterClientServices(new ServiceCollection())
                                     .AddSingleton<FnToCallFromHandler>((s, p) =>
                                     {
                                         var observations = p.GetRequiredService<TestObservations>();
                                         observations.ReceivedSignals.Add(s);

                                         return Task.CompletedTask;
                                     })
                                     .AddSingleton<Action<IHttpSseSignalReceiver>>(r => r.Enable(SseAddress)
                                                                                         .WithHttpClient(httpClient)
                                                                                         .WithHeaders(h =>
                                                                                         {
                                                                                             h.Authorization = new(
                                                                                                 "Basic",
                                                                                                 AuthorizationHeaderValue);
                                                                                             h.Add("test-header", TestHeaderValue);
                                                                                         }));

        if (testCase.ExpectedReceivedSignals.FirstOrDefault() is TestSignalWithCustomSerializedPayloadType)
        {
            var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

            jsonSerializerOptions.Converters.Add(new TestSignalWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory());
            jsonSerializerOptions.MakeReadOnly(true);

            _ = clientServices.AddSingleton(jsonSerializerOptions);
        }

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        var task = signalReceivers.RunHttpSseSignalReceivers(cts.Token);

        // for test cases without any enabled signal types, the receivers should complete immediately
        if (string.IsNullOrWhiteSpace(testCase.QueryString))
        {
            Assert.That(
                () => task.IsCompletedSuccessfully,
                Is.True
                  .After(host.AssertionTimeoutInMs)
                  .MilliSeconds
                  .PollEvery(10)
                  .MilliSeconds);

            return;
        }

        Assert.That(
            () => serverResponseHasBegun,
            Is.True
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        Assert.That(receivedHeadersOnServer!.Authorization.ToString(), Is.EqualTo(AuthorizationHeader));
        Assert.That(receivedHeadersOnServer, Does.ContainKey("test-header").WithValue("test-value"));

        await testCase.PublishSignals(host.Resolve<ISignalPublishers>());

        var observations = clientServiceProvider.GetRequiredService<TestObservations>();

        Assert.That(
            () => observations.ReceivedSignals,
            Is.EquivalentTo(testCase.ExpectedReceivedSignals)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        if (testCase.ExpectedReceivedSignals.FirstOrDefault() is TestSignalWithMiddleware)
        {
            var seenTransportTypeOnServer = host.Resolve<TestObservations>().SeenTransportTypeInMiddleware;
            Assert.That(seenTransportTypeOnServer?.IsHttpServerSentEvents(), Is.True, $"transport type is {seenTransportTypeOnServer?.Name}");
            Assert.That(seenTransportTypeOnServer?.Role, Is.EqualTo(SignalTransportRole.Publisher));

            var seenTransportTypeOnClient = clientServiceProvider.GetRequiredService<TestObservations>().SeenTransportTypeInMiddleware;
            Assert.That(seenTransportTypeOnClient?.IsHttpServerSentEvents(), Is.True, $"transport type is {seenTransportTypeOnClient?.Name}");
            Assert.That(seenTransportTypeOnClient?.Role, Is.EqualTo(SignalTransportRole.Receiver));
        }

        await cts.CancelAsync();
        await task;
    }

    private Task<HttpTransportTestHost> CreateTestHost(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configure)
    {
        return HttpTransportTestHost.Create(
            configureServices,
            app =>
            {
                _ = app.Use(async (ctx, next) =>
                {
                    receivedHeadersOnServer = ctx.Request.Headers;
                    ctx.Response.OnStarting(() =>
                    {
                        serverResponseHasBegun = true;

                        return Task.CompletedTask;
                    });

                    await next();
                });

                configure(app);
            });
    }
}
