using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using static Conqueror.Transport.Http.Tests.Messaging.HttpTestMessages;

namespace Conqueror.Transport.Http.Tests.Messaging.Client;

[TestFixture]
[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
public sealed class MessagingClientExecutionTests
{
    private const string AuthorizationHeaderScheme = "Basic";
    private const string AuthorizationHeaderValue = "username:password";
    private const string AuthorizationHeader = $"{AuthorizationHeaderScheme} {AuthorizationHeaderValue}";
    private const string TestHeaderValue = "test-value";

    private bool callWasReceivedOnServer;
    private IHeaderDictionary? receivedHeadersOnServer;

    [Test]
    [TestCaseSource(typeof(HttpTestMessages), nameof(GenerateTestCaseData))]
    public async Task GivenTestHttpMessage_WhenExecutingMessage_ReturnsCorrectResponse<TMessage, TResponse, TIHandler, THandler>(
        MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler
    {
        await using var host = await CreateTestHost(
            services => services.RegisterMessageType<TMessage, TResponse, TIHandler, THandler>(testCase),
            app => app.MapMessageEndpoints<TMessage, TResponse, TIHandler>(testCase));

        var clientServices = new ServiceCollection().AddConqueror()
                                                    .AddSingleton<TestObservations>()
                                                    .AddTransient(typeof(TestMessageMiddleware<,>));

        if (testCase.Message is TestMessageWithCustomSerializedPayloadType)
        {
            var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            jsonSerializerOptions.Converters.Add(new TestMessageWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory());
            jsonSerializerOptions.MakeReadOnly(true);

            _ = clientServices.AddSingleton(jsonSerializerOptions);
        }

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var messageClients = clientServiceProvider.GetRequiredService<IMessageSenders>();

        var httpClient = host.HttpClient;

        IHttpMessageSender<TM, TR> ConfigureTransport<TM, TR>(IMessageSenderBuilder<TM, TR> builder)
            where TM : class, IHttpMessage<TM, TR>
            => builder.UseHttp(new("http://localhost"))
                      .WithHttpClient(httpClient)
                      .WithHeaders(h =>
                      {
                          h.Authorization = new("Basic", AuthorizationHeaderValue);
                          h.Add("test-header", TestHeaderValue);
                      });

        var responseTask = testCase.Message switch
        {
            TestMessageWithMiddleware m => messageClients.For(TestMessageWithMiddleware.T)
                                                         .WithTransport(ConfigureTransport)
                                                         .WithPipeline(p => p.Use(
                                                                           p.ServiceProvider
                                                                            .GetRequiredService<TestMessageMiddleware<TestMessageWithMiddleware,
                                                                                TestMessageResponse>>()))
                                                         .Handle(m, host.TestTimeoutToken),

            TestMessageWithMiddlewareWithoutResponse m => messageClients.For(TestMessageWithMiddlewareWithoutResponse.T)
                                                                        .WithTransport(ConfigureTransport)
                                                                        .WithPipeline(p => p.Use(
                                                                                          p.ServiceProvider
                                                                                           .GetRequiredService<TestMessageMiddleware<
                                                                                               TestMessageWithMiddlewareWithoutResponse,
                                                                                               UnitMessageResponse>>()))
                                                                        .Handle(m, host.TestTimeoutToken),

            _ => THandler.Invoke(
                messageClients.For(THandler.MessageTypes).WithTransport(ConfigureTransport),
                (TMessage)testCase.Message,
                host.TestTimeoutToken),
        };

        if (!testCase.HandlerIsEnabled)
        {
            await Assert.ThatAsync(
                () => responseTask,
                Throws.TypeOf<HttpMessageFailedOnClientException>()
                      .With.Matches<HttpMessageFailedOnClientException>(ex => ex.StatusCode == HttpStatusCode.NotFound));

            return;
        }

        await responseTask;

        Assert.That(callWasReceivedOnServer, Is.True);

        Assert.That(receivedHeadersOnServer!.Authorization.ToString(), Is.EqualTo(AuthorizationHeader));
        Assert.That(receivedHeadersOnServer, Does.ContainKey("test-header").WithValue("test-value"));

        if (testCase.Message is TestMessageWithMiddleware or TestMessageWithMiddlewareWithoutResponse)
        {
            var seenTransportTypeOnServer = host.Resolve<TestObservations>().SeenTransportTypeInMiddleware;
            Assert.That(seenTransportTypeOnServer?.IsHttp(), Is.True, $"transport type is {seenTransportTypeOnServer?.Name}");
            Assert.That(seenTransportTypeOnServer?.Role, Is.EqualTo(MessageTransportRole.Receiver));

            var seenTransportTypeOnClient = clientServiceProvider.GetRequiredService<TestObservations>().SeenTransportTypeInMiddleware;
            Assert.That(seenTransportTypeOnClient?.IsHttp(), Is.True, $"transport type is {seenTransportTypeOnClient?.Name}");
            Assert.That(seenTransportTypeOnClient?.Role, Is.EqualTo(MessageTransportRole.Sender));
        }
    }

    [Test]
    public async Task GivenTestHttpMessage_WhenHandlerReceiverIsDisabled_EndpointDoesNotGetRegistered()
    {
        await using var host = await CreateTestHost(
            services =>
            {
                _ = services.AddMessageHandler<DisabledTestMessageHandler>();
                _ = services.AddRouting().AddMessageEndpoints();
            },
            app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapMessageEndpoints()));

        await using var clientServiceProvider = new ServiceCollection().AddConqueror().BuildServiceProvider();

        var httpClient = host.HttpClient;

        await Assert.ThatAsync(
            () => clientServiceProvider.GetRequiredService<IMessageSenders>()
                                       .For(TestMessage.T)
                                       .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(httpClient))
                                       .Handle(new(), host.TestTimeoutToken),
            Throws.TypeOf<HttpMessageFailedOnClientException>()
                  .With.Matches<HttpMessageFailedOnClientException>(ex => ex.StatusCode == HttpStatusCode.NotFound));

        Assert.That(callWasReceivedOnServer, Is.False);
    }

    [Test]
    public async Task GivenTestHttpMessageWithoutResponse_WhenHandlerReceiverIsDisabled_EndpointDoesNotGetRegistered()
    {
        await using var host = await CreateTestHost(
            services =>
            {
                _ = services.AddMessageHandler<DisabledTestMessageWithoutResponseHandler>();
                _ = services.AddRouting().AddMessageEndpoints();
            },
            app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapMessageEndpoints()));

        await using var clientServiceProvider = new ServiceCollection().AddConqueror().BuildServiceProvider();

        var httpClient = host.HttpClient;

        await Assert.ThatAsync(
            () => clientServiceProvider.GetRequiredService<IMessageSenders>()
                                       .For(TestMessageWithoutResponse.T)
                                       .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(httpClient))
                                       .Handle(new(), host.TestTimeoutToken),
            Throws.TypeOf<HttpMessageFailedOnClientException>()
                  .With.Matches<HttpMessageFailedOnClientException>(ex => ex.StatusCode == HttpStatusCode.NotFound));

        Assert.That(callWasReceivedOnServer, Is.False);
    }

    [Test]
    [TestCase(MessageFailedException.WellKnownReasons.Unauthenticated, StatusCodes.Status401Unauthorized)]
    [TestCase(MessageFailedException.WellKnownReasons.Unauthorized, StatusCodes.Status403Forbidden)]
    [TestCase(MessageFailedException.WellKnownReasons.InvalidFormattedContextData, StatusCodes.Status400BadRequest)]
    public async Task GivenTestHttpMessageHandlerThatThrowsWellKnownException_WhenExecutingMessage_ReturnsCorrectStatusCode(
        string reason,
        int expectedStatusCode)
    {
        await using var host = await CreateTestHost(
            services =>
            {
                _ = services.AddMessageHandler<TestMessageHandler>()
                            .AddSingleton<FnToCallFromHandler>((msg, _) => throw new TestWellKnownException(reason)
                            {
                                MessagePayload = msg,
                                TransportType = new(ConquerorTransportHttpConstants.TransportName, MessageTransportRole.Receiver),
                            });

                _ = services.AddRouting().AddMessageEndpoints();
            },
            app => app.UseConquerorWellKnownErrorHandling().UseRouting().UseEndpoints(endpoints => endpoints.MapMessageEndpoints()));

        await using var clientServiceProvider = new ServiceCollection().AddConqueror().BuildServiceProvider();

        var httpClient = host.HttpClient;

        await Assert.ThatAsync(
            () => clientServiceProvider.GetRequiredService<IMessageSenders>()
                                       .For(TestMessage.T)
                                       .WithTransport(b => b.UseHttp(new("http://localhost")).WithHttpClient(httpClient))
                                       .Handle(new(), host.TestTimeoutToken),
            Throws.TypeOf<HttpMessageFailedOnClientException>()
                  .With.Matches<HttpMessageFailedOnClientException>(ex => (int?)ex.StatusCode == expectedStatusCode));
    }

    [Test]
    public async Task GivenApiWithBearerTokenAuthentication_WhenExecutingMessageWithBearerToken_AuthenticatedPrincipalIsAvailableInContext()
    {
        ClaimsPrincipal? seenPrincipal = null;

        const string userName = "test-user";

        await using var host = await CreateTestHost(
            services =>
            {
                _ = services.AddMessageHandler<TestMessageHandler>()
                            .AddSingleton<FnToCallFromHandler>((_, p) =>
                            {
                                seenPrincipal = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.GetCurrentPrincipalInternal();

                                return Task.CompletedTask;
                            });

                _ = services.AddRouting().AddMessageEndpoints();
            },
            app => app.UseConquerorWellKnownErrorHandling()
                      .UseAuthentication()
                      .UseRouting()
                      .UseEndpoints(endpoints => endpoints.MapMessageEndpoints()));

        await using var clientServiceProvider = new ServiceCollection().AddConqueror().BuildServiceProvider();

        var httpClient = host.HttpClient;

        _ = await clientServiceProvider.GetRequiredService<IMessageSenders>()
                                       .For(TestMessage.T)
                                       .WithTransport(b => b.UseHttp(new("http://localhost"))
                                                            .WithHttpClient(httpClient)
                                                            .WithHeaders(h => h.WithAuthenticatedPrincipal(userName)))
                                       .Handle(new(), host.TestTimeoutToken);

        Assert.That(seenPrincipal, Is.Not.Null);
        Assert.That(seenPrincipal?.Identity?.IsAuthenticated, Is.True);
        Assert.That(seenPrincipal?.Identity?.Name, Is.EqualTo(userName));
    }

    private Task<HttpTransportTestHost> CreateTestHost(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configure)
    {
        return HttpTransportTestHost.Create(
            services =>
            {
                _ = services.AddSingleton<FnToCallFromHandler>((_, _) =>
                {
                    callWasReceivedOnServer = true;

                    return Task.CompletedTask;
                });

                configureServices(services);
            },
            app =>
            {
                _ = app.Use(async (ctx, next) =>
                {
                    receivedHeadersOnServer = ctx.Request.Headers;
                    await next();
                });

                configure(app);
            });
    }

    private sealed class TestWellKnownException(string wellKnownReason) : MessageFailedException
    {
        public override string WellKnownReason { get; } = wellKnownReason;
    }
}
