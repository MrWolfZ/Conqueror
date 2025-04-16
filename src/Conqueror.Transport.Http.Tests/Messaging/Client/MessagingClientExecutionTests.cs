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
    public async Task GivenTestHttpMessage_WhenExecutingMessage_ReturnsCorrectResponse<TMessage, TResponse, THandler>(MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where THandler : class, IGeneratedMessageHandler
    {
        await using var host = await CreateTestHost(
            services => services.RegisterMessageType<TMessage, TResponse, THandler>(testCase),
            app => app.MapMessageEndpoints<TMessage, TResponse>(testCase));

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

        var messageClients = clientServiceProvider.GetRequiredService<IMessageClients>();

        var httpClient = host.HttpClient;

        if (testCase.RegistrationMethod
            is MessageTestCaseRegistrationMethod.CustomController
            or MessageTestCaseRegistrationMethod.CustomEndpoint)
        {
            httpClient.BaseAddress = new("http://localhost/custom/");
        }

        // the source generator does not yet support serialization of complex GET messages, and in controller mode the
        // model validation will fail (while the endpoint mode will just silently generate an empty message due to lack
        // of out-of-the-box validation
        if (testCase is
            {
                Message: TestMessageWithComplexGetPayload,
                RegistrationMethod: MessageTestCaseRegistrationMethod.Controllers
                or MessageTestCaseRegistrationMethod.ExplicitController,
            })
        {
            return;
        }

        IHttpMessageTransportClient<TM, TR> ConfigureTransport<TM, TR>(IMessageTransportClientBuilder<TM, TR> builder)
            where TM : class, IHttpMessage<TM, TR>
            => builder.UseHttp(new("http://localhost"))
                      .WithHttpClient(httpClient)
                      .WithHeaders(h =>
                      {
                          h.Authorization = new("Basic", AuthorizationHeaderValue);
                          h.Add("test-header", TestHeaderValue);
                      });

        switch (testCase.Message)
        {
            case TestMessageWithMiddleware m:
                _ = await messageClients.For(TestMessageWithMiddleware.T)
                                        .WithTransport(ConfigureTransport)
                                        .WithPipeline(p => p.Use(p.ServiceProvider.GetRequiredService<TestMessageMiddleware<TestMessageWithMiddleware, TestMessageResponse>>()))
                                        .Handle(m, host.TestTimeoutToken);
                break;
            case TestMessageWithMiddlewareWithoutResponse m:
                await messageClients.For(TestMessageWithMiddlewareWithoutResponse.T)
                                    .WithTransport(ConfigureTransport)
                                    .WithPipeline(p => p.Use(p.ServiceProvider.GetRequiredService<TestMessageMiddleware<TestMessageWithMiddlewareWithoutResponse, UnitMessageResponse>>()))
                                    .Handle(m, host.TestTimeoutToken);
                break;
            default:
                _ = await messageClients.For<TMessage, TResponse>()
                                        .WithTransport(ConfigureTransport)
                                        .Handle((TMessage)testCase.Message, host.TestTimeoutToken);
                break;
        }

        Assert.That(callWasReceivedOnServer, Is.True);

        Assert.That(receivedHeadersOnServer!.Authorization.ToString(), Is.EqualTo(AuthorizationHeader));
        Assert.That(receivedHeadersOnServer, Does.ContainKey("test-header").WithValue("test-value"));

        if (testCase.Message is TestMessageWithMiddleware or TestMessageWithMiddlewareWithoutResponse)
        {
            var seenTransportTypeOnServer = host.Resolve<TestObservations>().SeenTransportTypeInMiddleware;
            Assert.That(seenTransportTypeOnServer?.IsHttp(), Is.True, $"transport type is {seenTransportTypeOnServer?.Name}");
            Assert.That(seenTransportTypeOnServer?.Role, Is.EqualTo(MessageTransportRole.Server));

            var seenTransportTypeOnClient = clientServiceProvider.GetRequiredService<TestObservations>().SeenTransportTypeInMiddleware;
            Assert.That(seenTransportTypeOnClient?.IsHttp(), Is.True, $"transport type is {seenTransportTypeOnClient?.Name}");
            Assert.That(seenTransportTypeOnClient?.Role, Is.EqualTo(MessageTransportRole.Client));
        }
    }

    private Task<HttpTransportTestHost> CreateTestHost(Action<IServiceCollection> configureServices,
                                          Action<IApplicationBuilder> configure)
    {
        return HttpTransportTestHost.Create(
            services =>
            {
                _ = services.AddSingleton<FnToCallFromHandler>(_ =>
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
}
