using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
    [TestCaseSource(typeof(TestMessages), nameof(TestMessages.GenerateTestCaseData))]
    public async Task GivenTestHttpMessage_WhenExecutingMessage_ReturnsCorrectResponse<TMessage, TResponse, THandler>(TestMessages.MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where THandler : class, IGeneratedMessageHandler
    {
        await using var host = await CreateTestHost(
            services => services.RegisterMessageType<TMessage, TResponse, THandler>(testCase),
            app => app.MapMessageEndpoints<TMessage, TResponse>(testCase));

        var clientServices = new ServiceCollection().AddConqueror()
                                                    .AddSingleton<TestMessages.TestObservations>()
                                                    .AddTransient(typeof(TestMessages.TestMessageMiddleware<,>));

        if (testCase.Message is TestMessages.TestMessageWithCustomSerializedPayloadType)
        {
            var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            jsonSerializerOptions.Converters.Add(new TestMessages.TestMessageWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory());
            jsonSerializerOptions.MakeReadOnly(true);

            _ = clientServices.AddSingleton(jsonSerializerOptions);
        }

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var messageClients = clientServiceProvider.GetRequiredService<IMessageClients>();

        var httpClient = host.HttpClient;

        if (testCase.RegistrationMethod
            is TestMessages.MessageTestCaseRegistrationMethod.CustomController
            or TestMessages.MessageTestCaseRegistrationMethod.CustomEndpoint)
        {
            httpClient.BaseAddress = new("http://localhost/custom/");
        }

        // the source generator does not yet support serialization of complex GET messages, and in controller mode the
        // model validation will fail (while the endpoint mode will just silently generate an empty message due to lack
        // of out-of-the-box validation
        if (testCase is
            {
                Message: TestMessages.TestMessageWithComplexGetPayload,
                RegistrationMethod: TestMessages.MessageTestCaseRegistrationMethod.Controllers
                or TestMessages.MessageTestCaseRegistrationMethod.ExplicitController,
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
            case TestMessages.TestMessageWithMiddleware m:
                _ = await messageClients.For(TestMessages.TestMessageWithMiddleware.T)
                                        .WithTransport(ConfigureTransport)
                                        .WithPipeline(p => p.Use(p.ServiceProvider.GetRequiredService<TestMessages.TestMessageMiddleware<TestMessages.TestMessageWithMiddleware, TestMessages.TestMessageResponse>>()))
                                        .Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithMiddlewareWithoutResponse m:
                await messageClients.For(TestMessages.TestMessageWithMiddlewareWithoutResponse.T)
                                    .WithTransport(ConfigureTransport)
                                    .WithPipeline(p => p.Use(p.ServiceProvider.GetRequiredService<TestMessages.TestMessageMiddleware<TestMessages.TestMessageWithMiddlewareWithoutResponse, UnitMessageResponse>>()))
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

        if (testCase.Message is TestMessages.TestMessageWithMiddleware or TestMessages.TestMessageWithMiddlewareWithoutResponse)
        {
            var seenTransportTypeOnServer = host.Resolve<TestMessages.TestObservations>().SeenTransportTypeInMiddleware;
            Assert.That(seenTransportTypeOnServer?.IsHttp(), Is.True, $"transport type is {seenTransportTypeOnServer?.Name}");
            Assert.That(seenTransportTypeOnServer?.Role, Is.EqualTo(MessageTransportRole.Server));

            var seenTransportTypeOnClient = clientServiceProvider.GetRequiredService<TestMessages.TestObservations>().SeenTransportTypeInMiddleware;
            Assert.That(seenTransportTypeOnClient?.IsHttp(), Is.True, $"transport type is {seenTransportTypeOnClient?.Name}");
            Assert.That(seenTransportTypeOnClient?.Role, Is.EqualTo(MessageTransportRole.Client));
        }
    }

    private Task<TestHost> CreateTestHost(Action<IServiceCollection> configureServices,
                                          Action<IApplicationBuilder> configure)
    {
        return TestHost.Create(
            services =>
            {
                _ = services.AddSingleton<TestMessages.FnToCallFromHandler>(_ =>
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
