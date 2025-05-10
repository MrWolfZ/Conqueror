using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using static Conqueror.Transport.Http.Tests.HttpTestContextData;
using static Conqueror.Transport.Http.Tests.Messaging.HttpTestMessages;

namespace Conqueror.Transport.Http.Tests.Messaging.Client;

[TestFixture]
public sealed class MessagingClientContextTests : IDisposable
{
    private DisposableActivity? clientActivity;
    private string? seenMessageIdOnServer;
    private string? seenTraceIdOnServer;

    [Test]
    [TestCaseSource(nameof(GenerateContextDataTestCases))]
    public async Task GivenContextData_WhenSendingMessage_DataIsCorrectlySentAndReturned<TMessage, TResponse, TIHandler, THandler>(
        bool hasUpstream,
        bool hasDownstream,
        bool hasBidirectional,
        bool hasActivity,
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

        var serverTestObservations = host.Resolve<TestObservations>();

        serverTestObservations.ShouldAddUpstreamData = hasUpstream;
        serverTestObservations.ShouldAddBidirectionalData = hasBidirectional;

        if (hasActivity)
        {
            clientActivity = DisposableActivity.Create(nameof(GivenContextData_WhenSendingMessage_DataIsCorrectlySentAndReturned));
            _ = clientActivity.Activity.Start();
        }

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();

        if (hasDownstream)
        {
            foreach (var (key, value) in ContextData)
            {
                conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            }
        }

        var httpClient = host.HttpClient;

        string? seenMessageIdOnClient = null;
        string? seenTraceIdOnClient = null;

        var handler = messageClients.For(TIHandler.MessageTypes)
                                    .WithTransport(b =>
                                    {
                                        seenMessageIdOnClient = b.ConquerorContext.GetMessageId();
                                        seenTraceIdOnClient = b.ConquerorContext.GetTraceId();
                                        return b.UseHttp(new("http://localhost")).WithHttpClient(httpClient);
                                    });

        if (!testCase.HandlerIsEnabled)
        {
            await Assert.ThatAsync(() => THandler.Invoke(handler, (TMessage)testCase.Message, host.TestTimeoutToken), Throws.TypeOf<HttpMessageFailedOnClientException>());

            Assert.That(seenMessageIdOnServer, Is.Null);
            Assert.That(seenTraceIdOnServer, Is.Null);
            return;
        }

        _ = await THandler.Invoke(handler, (TMessage)testCase.Message, host.TestTimeoutToken);

        Assert.That(seenMessageIdOnServer, Is.EqualTo(seenMessageIdOnClient));
        Assert.That(seenTraceIdOnServer, Is.EqualTo(seenTraceIdOnClient));

        if (clientActivity is not null)
        {
            Assert.That(seenTraceIdOnServer, Is.EqualTo(clientActivity.TraceId));
        }

        Assert.That(conquerorContext.UpstreamContextData.WhereScopeIsAcrossTransports(), hasUpstream ? Is.EquivalentTo(ContextData) : Is.Empty);
        Assert.That(conquerorContext.ContextData.WhereScopeIsAcrossTransports(), hasBidirectional ? Is.EquivalentTo(ContextData) : Is.Empty);

        var receivedContextData = serverTestObservations.ReceivedDownstreamContextData;
        var receivedBidirectionalContextData = serverTestObservations.ReceivedBidirectionalContextData;

        Assert.That(receivedContextData, Is.Not.Null.And.Not.Empty); // always non-empty due to message ID, trace ID, etc.
        Assert.That(receivedBidirectionalContextData, hasBidirectional ? Is.Not.Empty : Is.Empty);

        if (hasDownstream)
        {
            Assert.That(ContextData, Is.SubsetOf(receivedContextData.AsKeyValuePairs<string>()));
        }
        else
        {
            Assert.That(receivedContextData.WhereScopeIsAcrossTransports().Intersect(ContextData), Is.Empty);
        }

        if (hasBidirectional)
        {
            Assert.That(ContextData, Is.SubsetOf(receivedBidirectionalContextData.AsKeyValuePairs<string>()));
        }
        else
        {
            Assert.That(receivedBidirectionalContextData.WhereScopeIsAcrossTransports().Intersect(ContextData), Is.Empty);
        }
    }

    public void Dispose()
    {
        clientActivity?.Dispose();
    }

    private static IEnumerable<TestCaseData> GenerateContextDataTestCases()
    {
        return from testCaseData in GenerateTestCaseData()
               from hasUpstream in new[] { true, false }
               from hasDownstream in new[] { true, false }
               from hasBidirectional in new[] { true, false }
               from hasActivity in new[] { true, false }
               select new TestCaseData(hasUpstream, hasDownstream, hasBidirectional, hasActivity, testCaseData.Arguments[0])
               {
                   TypeArgs = testCaseData.TypeArgs,
               };
    }

    private Task<HttpTransportTestHost> CreateTestHost(Action<IServiceCollection> configureServices,
                                                       Action<IApplicationBuilder> configure)
    {
        return HttpTransportTestHost.Create(
            services =>
            {
                _ = services.AddSingleton<FnToCallFromHandler>((_, p) =>
                {
                    var conquerorContextAccessor = p.GetRequiredService<IConquerorContextAccessor>();
                    seenMessageIdOnServer = conquerorContextAccessor.ConquerorContext?.GetMessageId();
                    seenTraceIdOnServer = conquerorContextAccessor.ConquerorContext?.GetTraceId();
                    ObserveAndSetContextData(p.GetRequiredService<TestObservations>(), conquerorContextAccessor);
                    return Task.CompletedTask;
                });

                configureServices(services);

                // we generate a lot of test cases, which would produce a lot of meaningless logs that cause
                // a lot of churn on the disk as well as the CI infra, so we increase the minimal log level
                _ = services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Information));
            },
            configure);
    }

    private static void ObserveAndSetContextData(TestObservations testObservations, IConquerorContextAccessor conquerorContextAccessor)
    {
        testObservations.ReceivedMessageIds.Add(conquerorContextAccessor.ConquerorContext?.GetMessageId());
        testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.GetTraceId());
        testObservations.ReceivedDownstreamContextData = conquerorContextAccessor.ConquerorContext?.DownstreamContextData;
        testObservations.ReceivedBidirectionalContextData = conquerorContextAccessor.ConquerorContext?.ContextData;

        if (testObservations.ShouldAddUpstreamData)
        {
            foreach (var item in ContextData)
            {
                conquerorContextAccessor.ConquerorContext?.UpstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
            }

            foreach (var item in InProcessContextData)
            {
                conquerorContextAccessor.ConquerorContext?.UpstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
            }
        }

        if (testObservations.ShouldAddBidirectionalData)
        {
            foreach (var item in ContextData)
            {
                conquerorContextAccessor.ConquerorContext?.ContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
            }

            foreach (var item in InProcessContextData)
            {
                conquerorContextAccessor.ConquerorContext?.ContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
            }
        }
    }
}
