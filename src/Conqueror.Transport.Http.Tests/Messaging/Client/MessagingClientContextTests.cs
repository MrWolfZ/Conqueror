using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;

namespace Conqueror.Transport.Http.Tests.Messaging.Client;

[TestFixture]
public sealed class MessagingClientContextTests : IDisposable
{
    private static readonly Dictionary<string, string> ContextData = new()
    {
        { "key1", "value1" },
        { "key2", "value2" },
        { "keyWith,Comma", "value" },
        { "key4", "valueWith,Comma" },
        { "keyWith=Equals", "value" },
        { "key6", "valueWith=Equals" },
        { "keyWith|Pipe", "value" },
        { "key8", "valueWith|Pipe" },
        { "keyWith:Colon", "value" },
        { "key10", "valueWith:Colon" },
    };

    private static readonly Dictionary<string, string> InProcessContextData = new()
    {
        { "key11", "value1" },
        { "key12", "value2" },
    };

    private DisposableActivity? activity;
    private string? seenMessageIdOnServer;
    private string? seenTraceIdOnServer;

    [Test]
    [Combinatorial]
    public async Task GivenContextData_WhenSendingMessage_DataIsCorrectlySentAndReturned(
        [Values] bool hasUpstream,
        [Values] bool hasDownstream,
        [Values] bool hasBidirectional,
        [Values] bool hasActivity,
        [ValueSource(typeof(TestMessages), nameof(TestMessages.GenerateTestCases))]
        TestMessages.MessageTestCase testCase)
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

        var serverTestObservations = host.Resolve<TestMessages.TestObservations>();

        serverTestObservations.ShouldAddUpstreamData = hasUpstream;
        serverTestObservations.ShouldAddBidirectionalData = hasBidirectional;

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();

        if (hasActivity)
        {
            activity = CreateActivity(nameof(GivenContextData_WhenSendingMessage_DataIsCorrectlySentAndReturned));
        }

        if (hasDownstream)
        {
            foreach (var (key, value) in ContextData)
            {
                conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            }
        }

        var httpClient = host.HttpClient;

        if (testCase.RegistrationMethod
            is TestMessages.MessageTestCaseRegistrationMethod.CustomController
            or TestMessages.MessageTestCaseRegistrationMethod.CustomEndpoint)
        {
            httpClient.BaseAddress = new("http://localhost/custom/");
        }

        string? seenMessageIdOnClient = null;
        string? seenTraceIdOnClient = null;

        IMessageHandler<TMessage, TResponse> WithHttpTransport<TMessage, TResponse>(IMessageHandler<TMessage, TResponse> handler)
            where TMessage : class, IHttpMessage<TMessage, TResponse>
        {
            return handler.WithTransport(b =>
            {
                seenMessageIdOnClient = b.ConquerorContext.GetMessageId();
                seenTraceIdOnClient = b.ConquerorContext.GetTraceId();
                return b.UseHttp(new("http://localhost")).WithHttpClient(httpClient);
            });
        }

        IMessageHandler<TMessage> WithHttpTransportWithoutResponse<TMessage>(IMessageHandler<TMessage> handler)
            where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
        {
            return handler.WithTransport(b =>
            {
                seenMessageIdOnClient = b.ConquerorContext.GetMessageId();
                seenTraceIdOnClient = b.ConquerorContext.GetTraceId();
                return b.UseHttp(new("http://localhost")).WithHttpClient(httpClient);
            });
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
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessage.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithoutResponse m:
                await WithHttpTransportWithoutResponse(messageClients.For(TestMessages.TestMessageWithoutResponse.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithoutPayload m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithoutPayload.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithoutResponseWithoutPayload m:
                await WithHttpTransportWithoutResponse(messageClients.For(TestMessages.TestMessageWithoutResponseWithoutPayload.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithMethod m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithMethod.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithPathPrefix m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithPathPrefix.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithVersion m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithVersion.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithPath m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithPath.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithPathPrefixAndPathAndVersion m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithPathPrefixAndPathAndVersion.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithFullPath m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithFullPath.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithFullPathAndVersion m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithFullPathAndVersion.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithSuccessStatusCode m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithSuccessStatusCode.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithName m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithName.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithApiGroupName m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithApiGroupName.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithGet m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithGet.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithGetWithoutPayload m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithGetWithoutPayload.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithComplexGetPayload m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithComplexGetPayload.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithCustomSerializedPayloadType m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithCustomSerializedPayloadType.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithCustomSerializer m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithCustomSerializer.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithCustomJsonTypeInfo m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithCustomJsonTypeInfo.T)).Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithMiddleware m:
                _ = await WithHttpTransport(messageClients.For(TestMessages.TestMessageWithMiddleware.T))
                          .WithPipeline(p => p.Use(p.ServiceProvider.GetRequiredService<TestMessages.TestMessageMiddleware<TestMessages.TestMessageWithMiddleware, TestMessages.TestMessageResponse>>()))
                          .Handle(m, host.TestTimeoutToken);
                break;
            case TestMessages.TestMessageWithMiddlewareWithoutResponse m:
                await WithHttpTransportWithoutResponse(messageClients.For(TestMessages.TestMessageWithMiddlewareWithoutResponse.T)).Handle(m, host.TestTimeoutToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(testCase), testCase, null);
        }

        Assert.That(seenMessageIdOnServer, Is.EqualTo(seenMessageIdOnClient));
        Assert.That(seenTraceIdOnServer, Is.EqualTo(seenTraceIdOnClient));

        if (activity is not null)
        {
            Assert.That(seenTraceIdOnServer, Is.EqualTo(activity.TraceId));
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
        activity?.Dispose();
    }

    private Task<TestHost> CreateTestHost(Action<IServiceCollection> configureServices,
                                          Action<IApplicationBuilder> configure)
    {
        return TestHost.Create(
            services =>
            {
                _ = services.AddSingleton<TestMessages.FnToCallFromHandler>(p =>
                {
                    var conquerorContextAccessor = p.GetRequiredService<IConquerorContextAccessor>();
                    seenMessageIdOnServer = conquerorContextAccessor.ConquerorContext?.GetMessageId();
                    seenTraceIdOnServer = conquerorContextAccessor.ConquerorContext?.GetTraceId();
                    ObserveAndSetContextData(p.GetRequiredService<TestMessages.TestObservations>(), conquerorContextAccessor);
                    return Task.CompletedTask;
                });

                configureServices(services);
            },
            app =>
            {
                _ = app.Use(async (ctx, next) =>
                {
                    if (activity is not null)
                    {
                        _ = activity.Activity.Start();

                        try
                        {
                            await next();
                            return;
                        }
                        finally
                        {
                            activity.Activity.Stop();
                        }
                    }

                    await next();
                });

                configure(app);
            });
    }

    private static void ObserveAndSetContextData(TestMessages.TestObservations testObservations, IConquerorContextAccessor conquerorContextAccessor)
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

    private static DisposableActivity CreateActivity(string name)
    {
        var activitySource = new ActivitySource(name);

        var activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(activityListener);

        var a = activitySource.CreateActivity(name, ActivityKind.Server)!;
        return new(a, activitySource, activityListener, a);
    }

    private sealed class DisposableActivity(Activity activity, params IDisposable[] disposables) : IDisposable
    {
        private readonly IReadOnlyCollection<IDisposable> disposables = disposables;

        public Activity Activity { get; } = activity;

        public string TraceId => Activity.TraceId.ToString();

        public void Dispose()
        {
            foreach (var disposable in disposables.Reverse())
            {
                disposable.Dispose();
            }
        }
    }
}
