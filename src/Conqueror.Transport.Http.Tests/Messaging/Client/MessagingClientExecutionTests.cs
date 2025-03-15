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
    [TestCaseSource(typeof(TestMessages), nameof(TestMessages.GenerateTestCases))]
    public async Task GivenTestHttpMessage_WhenExecutingMessage_ReturnsCorrectResponse(TestMessages.MessageTestCase testCase)
    {
        await using var host = await CreateTestHost(services => services.RegisterMessageType(testCase), app => app.MapMessageEndpoints(testCase));

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

        IMessageHandler<TMessage, TResponse> WithHttpTransport<TMessage, TResponse>(IMessageHandler<TMessage, TResponse> handler)
            where TMessage : class, IMessage<TResponse>, IHttpMessage<TMessage, TResponse>, IMessageTypes<TMessage, TResponse>
        {
            return handler.WithTransport(b => b.UseHttp(new("http://localhost"))
                                               .WithHttpClient(httpClient)
                                               .WithHeaders(h =>
                                               {
                                                   h.Authorization = new("Basic", AuthorizationHeaderValue);
                                                   h.Add("test-header", TestHeaderValue);
                                               }));
        }

        IMessageHandler<TMessage> WithHttpTransportWithoutResponse<TMessage>(IMessageHandler<TMessage> handler)
            where TMessage : class, IMessage<UnitMessageResponse>, IHttpMessage<TMessage, UnitMessageResponse>, IMessageTypes<TMessage, UnitMessageResponse>
        {
            return handler.WithTransport(b => b.UseHttp(new("http://localhost"))
                                               .WithHttpClient(httpClient)
                                               .WithHeaders(h =>
                                               {
                                                   h.Authorization = new("Basic", AuthorizationHeaderValue);
                                                   h.Add("test-header", TestHeaderValue);
                                               }));
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

        switch (testCase.Message)
        {
            case TestMessages.TestMessage m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessage.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithoutResponse m:
                await WithHttpTransportWithoutResponse(messageClients.For<TestMessages.TestMessageWithoutResponse.IHandler>()).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithoutPayload m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithoutPayload.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithoutResponseWithoutPayload m:
                await WithHttpTransportWithoutResponse(messageClients.For<TestMessages.TestMessageWithoutResponseWithoutPayload.IHandler>()).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithMethod m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithMethod.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithPathPrefix m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithPathPrefix.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithVersion m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithVersion.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithPath m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithPath.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithPathPrefixAndPathAndVersion m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithPathPrefixAndPathAndVersion.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithFullPath m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithFullPath.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithFullPathAndVersion m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithFullPathAndVersion.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithSuccessStatusCode m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithSuccessStatusCode.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithName m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithName.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithApiGroupName m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithApiGroupName.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithGet m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithGet.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithGetWithoutPayload m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithGetWithoutPayload.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithComplexGetPayload m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithComplexGetPayload.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithCustomSerializedPayloadType m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithCustomSerializedPayloadType.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithCustomSerializer m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithCustomSerializer.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithCustomJsonTypeInfo m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithCustomJsonTypeInfo.IHandler>()).Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithMiddleware m:
                Assert.That(await WithHttpTransport(messageClients.For<TestMessages.TestMessageWithMiddleware.IHandler>())
                                  .WithPipeline(p => p.Use(p.ServiceProvider.GetRequiredService<TestMessages.TestMessageMiddleware<TestMessages.TestMessageWithMiddleware, TestMessages.TestMessageResponse>>()))
                                  .Handle(m, host.TestTimeoutToken),
                            Is.EqualTo(testCase.Response));
                break;
            case TestMessages.TestMessageWithMiddlewareWithoutResponse m:
                await WithHttpTransportWithoutResponse(messageClients.For<TestMessages.TestMessageWithMiddlewareWithoutResponse.IHandler>())
                      .WithPipeline(p => p.Use(p.ServiceProvider.GetRequiredService<TestMessages.TestMessageMiddleware<TestMessages.TestMessageWithMiddlewareWithoutResponse, UnitMessageResponse>>()))
                      .Handle(m, host.TestTimeoutToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(testCase), testCase, null);
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
