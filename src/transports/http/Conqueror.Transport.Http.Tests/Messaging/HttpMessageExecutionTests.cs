using System.Net.Mime;
using System.Security.Claims;
using static Conqueror.Transport.Http.Tests.Messaging.HttpMessageTestCases;

namespace Conqueror.Transport.Http.Tests.Messaging;

[TestFixture]
public sealed class HttpMessageExecutionTests
{
    [Test]
    public async Task GivenTestHttpMessage_WhenHandlerReceiverIsDisabled_EndpointDoesNotGetRegistered()
    {
        using var testTimeouts = HttpTransportTestTimeouts.Create();

        await using var host = await HttpTransportTestWebHost.Create(
            services =>
            {
                _ = services.AddMessageHandler<TestMessageHandler>()
                            .AddSingleton<Action<IHttpMessageReceiver>>(r => r.Disable());

                _ = services.AddSingleton<ILogger>(p => p.GetRequiredService<ILogger<HttpMessageExecutionTests>>());

                _ = services.AddRouting().AddConquerorHttpServerAspNetCore();
            },
            app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapMessageEndpoints()));

        await using var clientServiceProvider = new ServiceCollection().AddConquerorHttpClient().BuildServiceProvider();

        var httpClient = host.HttpClient;

        await Assert.ThatAsync(
            () => clientServiceProvider.GetRequiredService<IMessageSenders>()
                                       .For(TestMessage.T)
                                       .WithTransport(b => b.UseHttp(new("http://conqueror.test")).WithHttpClient(httpClient))
                                       .Handle(new() { Payload = 10 }, testTimeouts.TestTimeoutToken),
            Throws.TypeOf<HttpMessageFailedOnClientException>()
                  .With.Matches<HttpMessageFailedOnClientException>(ex => ex.StatusCode == HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GivenTestHttpMessageWithoutResponse_WhenHandlerReceiverIsDisabled_EndpointDoesNotGetRegistered()
    {
        using var testTimeouts = HttpTransportTestTimeouts.Create();

        await using var host = await HttpTransportTestWebHost.Create(
            services =>
            {
                _ = services.AddMessageHandler<TestMessageWithoutResponseHandler>()
                            .AddSingleton<Action<IHttpMessageReceiver>>(r => r.Disable());

                _ = services.AddSingleton<ILogger>(p => p.GetRequiredService<ILogger<HttpMessageExecutionTests>>());

                _ = services.AddRouting().AddConquerorHttpServerAspNetCore();
            },
            app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapMessageEndpoints()));

        await using var clientServiceProvider = new ServiceCollection().AddConquerorHttpClient().BuildServiceProvider();

        var httpClient = host.HttpClient;

        await Assert.ThatAsync(
            () => clientServiceProvider.GetRequiredService<IMessageSenders>()
                                       .For(TestMessageWithoutResponse.T)
                                       .WithTransport(b => b.UseHttp(new("http://conqueror.test")).WithHttpClient(httpClient))
                                       .Handle(new() { Payload = 10 }, testTimeouts.TestTimeoutToken),
            Throws.TypeOf<HttpMessageFailedOnClientException>()
                  .With.Matches<HttpMessageFailedOnClientException>(ex => ex.StatusCode == HttpStatusCode.NotFound));
    }

    [Test]
    [TestCase(MessageFailedException.WellKnownReasons.Unauthenticated, StatusCodes.Status401Unauthorized)]
    [TestCase(MessageFailedException.WellKnownReasons.Unauthorized, StatusCodes.Status403Forbidden)]
    [TestCase(MessageFailedException.WellKnownReasons.InvalidFormattedContextData, StatusCodes.Status400BadRequest)]
    public async Task GivenTestHttpMessageHandlerThatThrowsWellKnownException_WhenExecutingMessage_ReturnsCorrectStatusCode(
        string reason,
        int expectedStatusCode)
    {
        using var testTimeouts = HttpTransportTestTimeouts.Create();

        await using var host = await HttpTransportTestWebHost.Create(
            services =>
            {
                _ = services.AddMessageHandler<TestMessageHandler>()
                            .AddSingleton<FnToCallFromHandler>((msg, _) => throw new TestWellKnownException(reason)
                            {
                                MessagePayload = msg,
                                TransportType = new(TransportName, MessageTransportRole.Receiver),
                            });

                _ = services.AddRouting()
                            .AddSingleton(ILogger (p) => p.GetRequiredService<ILogger<HttpMessageExecutionTests>>())
                            .AddConquerorHttpServerAspNetCore();
            },
            app => app.UseConquerorWellKnownErrorHandling().UseRouting().UseEndpoints(endpoints => endpoints.MapMessageEndpoints()));

        await using var clientServiceProvider = new ServiceCollection().AddConquerorHttpClient().BuildServiceProvider();

        var httpClient = host.HttpClient;

        await Assert.ThatAsync(
            () => clientServiceProvider.GetRequiredService<IMessageSenders>()
                                       .For(TestMessage.T)
                                       .WithTransport(b => b.UseHttp(new("http://conqueror.test")).WithHttpClient(httpClient))
                                       .Handle(new() { Payload = 10 }, testTimeouts.TestTimeoutToken),
            Throws.TypeOf<HttpMessageFailedOnClientException>()
                  .With.Matches<HttpMessageFailedOnClientException>(ex => (int?)ex.StatusCode == expectedStatusCode));
    }

    [Test]
    public async Task GivenApiWithBearerTokenAuthentication_WhenExecutingMessageWithBearerToken_AuthenticatedPrincipalIsAvailableInContext()
    {
        ClaimsPrincipal? seenPrincipal = null;

        const string userName = "test-user";

        using var testTimeouts = HttpTransportTestTimeouts.Create();

        await using var host = await HttpTransportTestWebHost.Create(
            services =>
            {
                _ = services.AddMessageHandler(p => new TestMessageHandler((_, _) =>
                {
                    seenPrincipal = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.GetCurrentPrincipalInternal();

                    return Task.CompletedTask;
                }));

                _ = services.AddRouting()
                            .AddSingleton(ILogger (p) => p.GetRequiredService<ILogger<HttpMessageExecutionTests>>())
                            .AddConquerorHttpServerAspNetCore();
            },
            app => app.UseConquerorWellKnownErrorHandling()
                      .UseAuthentication()
                      .UseRouting()
                      .UseEndpoints(endpoints => endpoints.MapMessageEndpoints()));

        await using var clientServiceProvider = new ServiceCollection().AddConquerorHttpClient().BuildServiceProvider();

        var httpClient = host.HttpClient;

        _ = await clientServiceProvider.GetRequiredService<IMessageSenders>()
                                       .For(TestMessage.T)
                                       .WithTransport(b => b.UseHttp(new("http://conqueror.test"))
                                                            .WithHttpClient(httpClient)
                                                            .WithHeaders(h => h.WithAuthenticatedPrincipal(userName)))
                                       .Handle(new() { Payload = 10 }, testTimeouts.TestTimeoutToken);

        Assert.That(seenPrincipal, Is.Not.Null);
        Assert.That(seenPrincipal?.Identity?.IsAuthenticated, Is.True);
        Assert.That(seenPrincipal?.Identity?.Name, Is.EqualTo(userName));
    }

    [Test]
    [TestCaseSource(nameof(CreateServerTestCases))]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive")]
    public async Task GivenTestHttpMessage_WhenCallingHttpEndpointDirectly_ReturnsCorrectResponse(
        HttpMessageConformityExecutionSuccessTestCase testCase)
    {
        await using var host = testCase.CreateTestHost();

        await using var receiverHost = await host.CreateReceiverTestHost(host.TestTimeoutToken);

        var targetUriBuilder = new UriBuilder
        {
            Host = "localhost",
            Path = testCase.FullPath,
        };

        for (var i = 0; i < testCase.ExpectedReceivedMessages.Count; i += 1)
        {
            var queryString = testCase.QueryStrings.ElementAt(i);
            var payload = testCase.MessagePayloads.ElementAt(i);
            var responsePayload = testCase.ResponsePayloads.Skip(i).FirstOrDefault() ?? string.Empty;

            if (queryString is not null)
            {
                targetUriBuilder.Query = queryString;
            }

            using var request = new HttpRequestMessage(new(testCase.HttpMethod), targetUriBuilder.Uri);

            using StringContent? content = payload is not null
                ? new(payload, new MediaTypeHeaderValue(testCase.MessageContentType ?? MediaTypeNames.Application.Json))
                : null;

            if (testCase.HttpMethod != MethodNames.Get)
            {
                request.Content = content;
            }

            var response = await receiverHost.HttpClient.SendAsync(request);

            if (!testCase.HandlerIsEnabled)
            {
                await response.AssertStatusCode(StatusCodes.Status404NotFound);

                return;
            }

            await response.AssertStatusCode(testCase.SuccessStatusCode);
            var resultString = await response.Content.ReadAsStringAsync();

            Assert.That(resultString, Is.EqualTo(responsePayload));
        }
    }

    [Test]
    [TestCaseSource(nameof(CreateServerTestCases))]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive")]
    public async Task GivenTestHttpMessage_WhenCallingHttpEndpointDirectlyWithWrongContentType_ReturnsError(
        HttpMessageConformityExecutionSuccessTestCase testCase)
    {
        await using var host = testCase.CreateTestHost();

        await using var receiverHost = await host.CreateReceiverTestHost(host.TestTimeoutToken);

        var targetUriBuilder = new UriBuilder
        {
            Host = "localhost",
            Path = testCase.FullPath,
        };

        for (var i = 0; i < testCase.ExpectedReceivedMessages.Count; i += 1)
        {
            using var request = new HttpRequestMessage(new(testCase.HttpMethod), targetUriBuilder.Uri);

            using StringContent content = new("wrong", new MediaTypeHeaderValue("application/wrong"));
            request.Content = content;

            var response = await receiverHost.HttpClient.SendAsync(request);

            if (!testCase.HandlerIsEnabled)
            {
                await response.AssertStatusCode(StatusCodes.Status404NotFound);

                return;
            }

            await response.AssertStatusCode(StatusCodes.Status415UnsupportedMediaType);
        }
    }

    [Test]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "false positive")]
    public async Task GivenTestHttpMessage_WhenCallingHttpEndpointDirectlyWithDifferentEncoding_ReturnsResponse()
    {
        var testCase = CreateSuccessTestCases()
            .First(tc => tc is
            {
                NumOfReceivers: 1,
                HandlerIsEnabled: true,
                ExpectedReceivedMessages.Count: > 0,
                HttpMethod: MethodNames.Post,
                MessageContentType: MediaTypeNames.Application.Json,
                ResponseContentType: MediaTypeNames.Application.Json,
            });

        await using var host = testCase.CreateTestHost();

        await using var receiverHost = await host.CreateReceiverTestHost(host.TestTimeoutToken);

        var targetUriBuilder = new UriBuilder
        {
            Host = "localhost",
            Path = testCase.FullPath,
        };

        for (var i = 0; i < testCase.ExpectedReceivedMessages.Count; i += 1)
        {
            var payload = testCase.MessagePayloads.ElementAt(i);
            var responsePayload = testCase.ResponsePayloads.Skip(i).FirstOrDefault() ?? string.Empty;

            using var request = new HttpRequestMessage(new(testCase.HttpMethod), targetUriBuilder.Uri);

            using StringContent content = new(payload!, Encoding.Unicode, new MediaTypeHeaderValue($"{MediaTypeNames.Application.Json}", "utf-16"));
            request.Content = content;

            var response = await receiverHost.HttpClient.SendAsync(request);

            await response.AssertStatusCode(testCase.SuccessStatusCode);
            var resultString = await response.Content.ReadAsStringAsync();

            Assert.That(resultString, Is.EqualTo(responsePayload));
        }
    }

    private static IEnumerable<TestCaseData> CreateServerTestCases()
        => CreateSuccessTestCases()
           .Where(tc => tc is { NumOfReceivers: 1, SingleMessageType: not null })
           .Select(tc => new TestCaseData(tc).SetName(tc.Name));

    private sealed class TestWellKnownException(string wellKnownReason) : MessageFailedException
    {
        public override string WellKnownReason { get; } = wellKnownReason;
    }
}
