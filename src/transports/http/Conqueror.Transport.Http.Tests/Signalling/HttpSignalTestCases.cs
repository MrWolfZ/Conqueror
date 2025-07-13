using Microsoft.AspNetCore.Http.Json;
using static Conqueror.Transport.Http.Tests.Signalling.HttpSignalConformityTestCase;

namespace Conqueror.Transport.Http.Tests.Signalling;

[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Members are used by ASP.NET Core via reflection")]
public static partial class HttpSignalTestCases
{
    private const string AuthorizationHeaderScheme = "Basic";
    private const string AuthorizationHeaderValue = "username:password";
    private const string AuthorizationHeader = $"{AuthorizationHeaderScheme} {AuthorizationHeaderValue}";
    private const string TestHeaderName = "test-value";
    private const string TestHeaderValue = "test-value";

    public static IEnumerable<HttpSignalConformityExecutionSuccessTestCase> CreateSuccessTestCases(HttpSignalTransportType transportType)
    {
        yield return new()
        {
            Name = "single receiver",
            TransportType = transportType,
            ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "single receiver with HTTP headers",
            TransportType = transportType,
            ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalHandler>(r, transportType, ct),
            ConfigureHeaders = h =>
            {
                h.Authorization = AuthorizationHeader;
                h.Append(TestHeaderName, TestHeaderValue);
            },
            BeforePublish = host =>
            {
                Assert.That(host.PublisherHost.ServerResponseHasBegunCount, Is.EqualTo(1));

                Assert.That(host.PublisherHost.ReceivedHeadersOnServer!.Authorization.ToString(), Is.EqualTo(AuthorizationHeader));
                Assert.That(host.PublisherHost.ReceivedHeadersOnServer, Does.ContainKey(TestHeaderName).WithValue(TestHeaderValue));

                return Task.CompletedTask;
            },
        };

        yield return new()
        {
            Name = "single receiver with parallel publish",
            TransportType = transportType,
            ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await Task.WhenAll(
                    CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct),
                    CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 20 }, ct));
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalHandler>(r, transportType, ct),
            SignalsArePublishedInParallel = true,
        };

        yield return new()
        {
            Name = "single receiver multiple times",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 20 },
                new TestSignal { Payload = 20 },
            ],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => r.CombineExecutions(
            [
                RunReceiverForTransport<TestSignalHandler>(r, transportType, ct),
                RunReceiverForTransport<TestSignalHandler>(r, transportType, ct),
            ]),
            NumOfReceivers = 2,
        };

        yield return new()
        {
            Name = "multiple receivers",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>()
                                    .AddSignalHandler<MultiTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 20 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
            NumOfReceivers = 2,
        };

        yield return new()
        {
            Name = "multiple receivers with parallel publish",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>()
                                    .AddSignalHandler<MultiTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await Task.WhenAll(
                    CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct),
                    CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 20 }, ct),
                    CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct));
            },
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
            NumOfReceivers = 2,
            SignalsArePublishedInParallel = true,
        };

        yield return new()
        {
            Name = "disabled receiver",
            TransportType = transportType,
            ExpectedReceivedSignals = [],
            ShouldCompleteImmediately = true,
            RegisterHandler = s => s.AddSignalHandler<DisabledTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<DisabledTestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "receiver for multiple signal types",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 11 },
                new TestSignal { Payload = 20 },
                new TestSignal2 { Payload2 = 21 },
            ],
            RegisterHandler = s => s.AddSignalHandler<MultiTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 11 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 20 }, ct);
                await CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 21 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<MultiTestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "handler for HTTP and non-HTTP signal",
            TransportType = transportType,
            ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandler<MixedWithNonHttpTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await p.For(NonHttpTestSignal.T).Handle(new() { Payload = 11 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 20 }, ct);
                await p.For(NonHttpTestSignal.T).Handle(new() { Payload = 21 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<MixedWithNonHttpTestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "signal with custom event type or tag",
            TransportType = transportType,
            ExpectedReceivedSignals = [new TestSignalWithCustomEventTypeOrTag { Payload = 10 }, new TestSignalWithCustomEventTypeOrTag { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandler<TestSignalWithCustomEventTypeOrTagHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignalWithCustomEventTypeOrTag.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignalWithCustomEventTypeOrTag.T, transportType).Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalWithCustomEventTypeOrTagHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "signal without payload",
            TransportType = transportType,
            ExpectedReceivedSignals = [new TestSignalWithoutPayload(), new TestSignalWithoutPayload()],
            RegisterHandler = s => s.AddSignalHandler<TestSignalWithoutPayloadHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignalWithoutPayload.T, transportType).Handle(new(), ct);
                await CreatePublisher(p, TestSignalWithoutPayload.T, transportType).Handle(new(), ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalWithoutPayloadHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "signal with custom serialized payload type",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignalWithCustomSerializedPayloadType { Payload = new(10) },
                new TestSignalWithCustomSerializedPayloadType { Payload = new(20) },
            ],
            RegisterHandler = s =>
            {
                var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    Converters = { new TestSignalWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory() },
                };

                jsonSerializerOptions.MakeReadOnly(populateMissingResolver: true);

                _ = s.AddSignalHandler<TestSignalWithCustomSerializedPayloadTypeHandler>()
                     .AddSingleton(jsonSerializerOptions);
            },
            RegisterOnServer = s =>
            {
                _ = s.AddTransient<JsonSerializerOptions>(p => p.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions)
                     .PostConfigure<JsonOptions>(options =>
                     {
                         options.SerializerOptions.Converters
                                .Add(new TestSignalWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory());
                     });
            },
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignalWithCustomSerializedPayloadType.T, transportType).Handle(new() { Payload = new(10) }, ct);
                await CreatePublisher(p, TestSignalWithCustomSerializedPayloadType.T, transportType).Handle(new() { Payload = new(20) }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalWithCustomSerializedPayloadTypeHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "signal with custom serializer",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignalWithCustomSerializer { Payload = 10 },
                new TestSignalWithCustomSerializer { Payload = 20 },
            ],
            RegisterHandler = s => s.AddSignalHandler<TestSignalWithCustomSerializerHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignalWithCustomSerializer.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignalWithCustomSerializer.T, transportType).Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalWithCustomSerializerHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "signal with custom type info",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignalWithCustomJsonTypeInfo { MessagePayload = 10 },
                new TestSignalWithCustomJsonTypeInfo { MessagePayload = 20 },
            ],
            RegisterHandler = s => s.AddSignalHandler<TestSignalWithCustomJsonTypeInfoHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignalWithCustomJsonTypeInfo.T, transportType).Handle(new() { MessagePayload = 10 }, ct);
                await CreatePublisher(p, TestSignalWithCustomJsonTypeInfo.T, transportType).Handle(new() { MessagePayload = 20 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalWithCustomJsonTypeInfoHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "receiver and publisher with middleware",
            TransportType = transportType,
            ExpectedReceivedSignals = [new TestSignalWithMiddleware { Payload = 10 }, new TestSignalWithMiddleware { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandler<TestSignalWithMiddlewareHandler>()
                                    .AddTransient<TestSignalMiddleware<TestSignalWithMiddleware>>()
                                    .AddSingleton<TestObservations>(),
            RegisterOnServer = s => s.AddTransient<TestSignalMiddleware<TestSignalWithMiddleware>>()
                                     .AddSingleton<TestObservations>(),
            PublishSignals = async (sp, ct) =>
            {
                await CreatePublisher(sp, TestSignalWithMiddleware.T, transportType)
                      .WithPipeline(p => p.Use(p.ServiceProvider.GetRequiredService<TestSignalMiddleware<TestSignalWithMiddleware>>()))
                      .Handle(new() { Payload = 10 }, ct);

                await CreatePublisher(sp, TestSignalWithMiddleware.T, transportType)
                      .WithPipeline(p => p.Use(p.ServiceProvider.GetRequiredService<TestSignalMiddleware<TestSignalWithMiddleware>>()))
                      .Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalWithMiddlewareHandler>(r, transportType, ct),

            AfterSignalsAreReceived = host =>
            {
                var seenTransportTypeOnServer = host.PublisherHost.Resolve<TestObservations>().SeenTransportTypeInMiddleware;
                var isCorrectTransportTypeOnServer = transportType switch
                {
                    HttpSignalTransportType.Sse => seenTransportTypeOnServer?.IsHttpServerSentEvents(),
                    HttpSignalTransportType.WebSockets => seenTransportTypeOnServer?.IsHttpWebSockets(),
                    _ => false,
                };

                Assert.That(isCorrectTransportTypeOnServer, Is.True, $"transport type is {seenTransportTypeOnServer?.Name}");
                Assert.That(seenTransportTypeOnServer?.Role, Is.EqualTo(SignalTransportRole.Publisher));

                foreach (var receiverHost in host.ReceiverHosts)
                {
                    var seenTransportTypeOnClient = receiverHost.Resolve<TestObservations>().SeenTransportTypeInMiddleware;
                    var isCorrectTransportTypeOnClient = transportType switch
                    {
                        HttpSignalTransportType.Sse => seenTransportTypeOnClient?.IsHttpServerSentEvents(),
                        HttpSignalTransportType.WebSockets => seenTransportTypeOnClient?.IsHttpWebSockets(),
                        _ => false,
                    };

                    Assert.That(isCorrectTransportTypeOnClient, Is.True, $"transport type is {seenTransportTypeOnClient?.Name}");
                    Assert.That(seenTransportTypeOnClient?.Role, Is.EqualTo(SignalTransportRole.Receiver));
                }

                return Task.CompletedTask;
            },
        };

        yield return new()
        {
            Name = "handler discovered via assembly scanning",
            TransportType = transportType,
            ExpectedReceivedSignals = [new TestSignalForAssemblyScanning { Payload = 10 }, new TestSignalForAssemblyScanning { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandlersFromAssembly(typeof(TestSignalForAssemblyScanning).Assembly),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignalForAssemblyScanning.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignalForAssemblyScanning.T, transportType).Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalForAssemblyScanningHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "delegate handlers",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignalWithDelegateHandler { Payload = 10 },
                new TestSignalWithDelegateHandler { Payload = 10 },
                new TestSignalWithDelegateHandler { Payload = 10 },
                new TestSignalWithDelegateHandler { Payload = 10 },
                new TestSignalWithDelegateHandler { Payload = 20 },
                new TestSignalWithDelegateHandler { Payload = 20 },
                new TestSignalWithDelegateHandler { Payload = 20 },
                new TestSignalWithDelegateHandler { Payload = 20 },
            ],
            RegisterHandler = s => AddDelegateHandlers(s, TestSignalWithDelegateHandler.T, transportType),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignalWithDelegateHandler.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignalWithDelegateHandler.T, transportType).Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
            NumOfReceivers = 4,
        };

        yield return new()
        {
            Name = "wild mix of signal types",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignalWithoutPayload(),
                new TestSignalWithCustomSerializer { Payload = 20 },
                new TestSignalWithCustomJsonTypeInfo { MessagePayload = 30 },
                new TestSignal { Payload = 40 },
            ],
            RegisterHandler = s => s.AddSignalHandler<WildMixTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignalWithoutPayload.T, transportType).Handle(new(), ct);
                await CreatePublisher(p, TestSignalWithCustomSerializer.T, transportType).Handle(new() { Payload = 20 }, ct);
                await CreatePublisher(p, TestSignalWithCustomJsonTypeInfo.T, transportType).Handle(new() { MessagePayload = 30 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 40 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<WildMixTestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "signal with hierarchy",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignalBase(1),

                // because we publish these events through TestSignalBase, the event type will be "testSignalBase"
                // and the signal will be deserialized as the base type
                new TestSignalBase(10),
                new TestSignalBase(20),

                // since the receiver observes multiple types from the type hierarchy, we expect each signal
                // to be received twice, but the type of the received signal will be the type that the publisher
                // was using
                new TestSignalSub(30, 31),
                new TestSignalSub(30, 31),

                // because we publish these events through TestSignalSub, the event type will be "testSignalSub"
                // and the signal will be deserialized as that type instead of TestSignalSubSub
                new TestSignalSub(40, 41),
                new TestSignalSub(40, 41),
            ],
            RegisterHandler = s => s.AddSignalHandler<MultiHierarchyTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignalBase.T, transportType).Handle(new(1), ct);
                await CreatePublisher(p, TestSignalBase.T, transportType).Handle(new TestSignalSub(10, 11), ct);
                await CreatePublisher(p, TestSignalBase.T, transportType).Handle(new TestSignalSubSub(20, 21, 22), ct);
                await CreatePublisher(p, TestSignalSub.T, transportType).Handle(new(30, 31), ct);
                await CreatePublisher(p, TestSignalSub.T, transportType).Handle(new TestSignalSubSub(40, 41, 42), ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<MultiHierarchyTestSignalHandler>(r, transportType, ct),
        };
    }

    public static IEnumerable<HttpSignalConformityExecutionSuccessTestCase> CreateSimpleSuccessTestCases(HttpSignalTransportType transportType)
    {
        yield return new()
        {
            Name = "single receiver",
            TransportType = transportType,
            ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "multiple receivers",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>()
                                    .AddSignalHandler<MultiTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 20 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct);
            },
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
            NumOfReceivers = 2,
        };
    }

    public static IEnumerable<HttpSignalConformityExecutionErrorTestCase> CreateErrorTestCases(HttpSignalTransportType transportType)
    {
        yield return new()
        {
            Name = "single handler with configuration error",
            TransportType = transportType,
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [new InvalidOperationException("configuration error")],
            PublishException = null,
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 0,
            HandlerExceptions = [],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
            PublishSignals = (_, _) => Task.CompletedTask,
            RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "multiple handlers, second with configuration error",
            TransportType = transportType,
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [null, new InvalidOperationException("configuration error")],
            PublishException = null,
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 0,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                    .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
        };

        yield return new()
        {
            Name = "single handler with disconnect",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            PublishException = null,
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 1,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct);
            },
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => RunReceiverForTransport<ThrowingTestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "multiple handlers with disconnects",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            PublishException = null,
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 2,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);

                await Task.Delay(10, ct);

                await CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 20 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct);
            },
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                    .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
        };

        yield return new()
        {
            Name = "single handler with unrecoverable connection error",
            TransportType = transportType,
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            PublishException = null,
            ConnectionResponses = [(StatusCodes.Status400BadRequest, ContentTypes.TextPlain, KeepAlive: false)],
            ExpectedInitialConnectionCount = 1,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => RunReceiverForTransport<ThrowingTestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "multiple handlers with unrecoverable connection errors",
            TransportType = transportType,
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            PublishException = null,
            ConnectionResponses =
            [
                (StatusCodes.Status403Forbidden, ContentTypes.TextPlain, KeepAlive: false),
                (StatusCodes.Status409Conflict, ContentTypes.TextPlain, KeepAlive: false),
            ],
            ExpectedInitialConnectionCount = 2,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                    .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
        };

        yield return new()
        {
            Name = "multiple handlers, one of which with unrecoverable connection error",
            TransportType = transportType,
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            PublishException = null,
            ConnectionResponses =
            [
                null,
                (StatusCodes.Status418ImATeapot, ContentTypes.EventStream, KeepAlive: false),
            ],
            ExpectedInitialConnectionCount = 2,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                    .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
        };

        yield return new()
        {
            Name = "single handler with recoverable connection error",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            PublishException = null,
            ConnectionResponses = [(StatusCodes.Status500InternalServerError, ContentTypes.TextPlain, KeepAlive: true)],
            ExpectedInitialConnectionCount = 2,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 20 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct);
            },
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => RunReceiverForTransport<ThrowingTestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "single handler with multiple recoverable connection errors",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            PublishException = null,
            ConnectionResponses =
            [
                (StatusCodes.Status503ServiceUnavailable, ContentTypes.TextPlain, KeepAlive: true),
                (StatusCodes.Status503ServiceUnavailable, ContentTypes.TextPlain, KeepAlive: true),
            ],
            ExpectedInitialConnectionCount = 3,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 20 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct);
            },
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => RunReceiverForTransport<ThrowingTestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "multiple handlers with recoverable connection errors",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            PublishException = null,
            ConnectionResponses =
            [
                (StatusCodes.Status502BadGateway, ContentTypes.TextPlain, KeepAlive: true),
                (StatusCodes.Status504GatewayTimeout, ContentTypes.TextPlain, KeepAlive: true),
            ],
            ExpectedInitialConnectionCount = 4,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 20 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct);
            },
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                    .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
        };

        yield return new()
        {
            Name = "multiple handlers, one of which with recoverable connection error",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            PublishException = null,
            ConnectionResponses =
            [
                null,
                (StatusCodes.Status501NotImplemented, ContentTypes.TextPlain, KeepAlive: true),
            ],
            ExpectedInitialConnectionCount = 3,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                await CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 20 }, ct);
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct);
            },
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                    .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
        };

        yield return new()
        {
            Name = "single handler with handler exception",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            PublishException = null,
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 1,
            HandlerExceptions =
            [
                null,
                new InvalidOperationException("handler exception"),
            ],
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);

                await Task.Delay(10, ct);

                await CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 20 }, ct);

                await Task.Delay(10, ct);

                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct);

                await Task.Delay(10, ct);

                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 40 }, ct);
            },
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => RunReceiverForTransport<ThrowingTestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "multiple handlers with handler exceptions",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            PublishException = null,
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 2,
            NumOfReceivers = 2,
            HandlerExceptions =
            [
                null,
                null,
                null,
                new InvalidOperationException("handler exception 1"),
                new InvalidOperationException("handler exception 2"),
            ],
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);

                await Task.Delay(10, ct);

                await CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 20 }, ct);

                await Task.Delay(10, ct);

                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct);

                await Task.Delay(10, ct);

                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 40 }, ct);
            },
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                    .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
        };

        yield return new()
        {
            Name = "multiple handlers, one of which with handler exception",
            TransportType = transportType,
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            ConfigurationExceptions = [],
            ConnectionResponses = [],
            PublishException = null,
            ExpectedInitialConnectionCount = 2,
            NumOfReceivers = 2,
            HandlerExceptions =
            [
                null, // on test signal 1 handler 1
                null, // on test signal 1 handler 2
                null, // on test signal 2 handler 1
                null, // on test signal 3 handler 1

                // throw the exception only in the second handler to work around rare race condition
                // where the exception in the first handler is caught, and therefore the client disconnects
                // before the second handler has a chance to run
                new InvalidOperationException("handler exception"), // on test signal 3 handler 2
            ],
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);

                await Task.Delay(10, ct);

                await CreatePublisher(p, TestSignal2.T, transportType).Handle(new() { Payload2 = 20 }, ct);

                await Task.Delay(10, ct);

                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct);

                // give client time to disconnect due to handler failure
                await Task.Delay(50, ct);

                await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 40 }, ct);
            },
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                    .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
        };

        yield return new()
        {
            Name = "single receiver with publish error",
            TransportType = transportType,
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            PublishException = new InvalidOperationException("publish error"),
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 1,
            HandlerExceptions = [],
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, ThrowingTestSignal.T, transportType).Handle(new(), ct);
            },
            RunReceivers = (r, ct) => RunReceiverForTransport<ThrowingTestSignalHandler>(r, transportType, ct),
        };

        yield return new()
        {
            Name = "multiple receivers with publish error",
            TransportType = transportType,
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            PublishException = new InvalidOperationException("publish error"),
            ConnectionResponses = [],
            ExpectedInitialConnectionCount = 2,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await CreatePublisher(p, ThrowingTestSignal.T, transportType).Handle(new(), ct);
            },
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>()
                                    .AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
        };
    }

    public static IEnumerable<HttpSignalConformityExecutionErrorTestCase> CreateReconnectDelayTestCases(HttpSignalTransportType transportType)
    {
        yield return new()
        {
            Name = "single handler",
            TransportType = transportType,
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            PublishException = null,
            ConnectionResponses = [(StatusCodes.Status503ServiceUnavailable, ContentTypes.TextPlain, KeepAlive: false)],
            ExpectedInitialConnectionCount = 2,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => RunReceiverForTransport<ThrowingTestSignalHandler>(r, transportType, ct),
        };
    }

    public static IEnumerable<HttpSignalConformityContextTestCase> CreateContextTestCases(HttpSignalTransportType transportType)
    {
        foreach (var (hasActivity, hasDownstream, hasBidirectional) in from hasActivity in new[] { true, false }
                                                                       from hasDownstream in new[] { true, false }
                                                                       from hasBidirectional in new[] { true, false }
                                                                       select (hasActivity, hasDownstream, hasBidirectional))
        {
            yield return new()
            {
                Name =
                    $"single receiver, {nameof(hasActivity)}: {hasActivity}, {nameof(hasDownstream)}: {hasDownstream}, {nameof(hasBidirectional)}: {hasBidirectional}",
                TransportType = transportType,
                HasActivity = hasActivity,
                HasDownstreamData = hasDownstream,
                HasBidirectionalData = hasBidirectional,
                RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
                PublishSignals = async (p, ct) =>
                {
                    await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                    await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 20 }, ct);
                },
                RunReceivers = (r, ct) => RunReceiverForTransport<TestSignalHandler>(r, transportType, ct),
            };

            yield return new()
            {
                Name =
                    $"multiple receivers, {nameof(hasActivity)}: {hasActivity}, {nameof(hasDownstream)}: {hasDownstream}, {nameof(hasBidirectional)}: {hasBidirectional}",
                TransportType = transportType,
                HasActivity = hasActivity,
                HasDownstreamData = hasDownstream,
                HasBidirectionalData = hasBidirectional,
                RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>()
                                        .AddSignalHandler<MultiTestSignalHandler>(),
                PublishSignals = async (p, ct) =>
                {
                    await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 10 }, ct);
                    await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 20 }, ct);
                    await CreatePublisher(p, TestSignal.T, transportType).Handle(new() { Payload = 30 }, ct);
                },
                RunReceivers = (r, ct) => RunReceiversForTransport(r, transportType, ct),
                NumOfReceivers = 2,
            };
        }
    }

    private static TIHandler CreatePublisher<TSignal, TIHandler>(
        ISignalPublishers publishers,
        SignalTypes<TSignal, TIHandler> signalTypes,
        HttpSignalTransportType transportType)
        where TSignal : class, IHttpSseSignal<TSignal>, IHttpWebSocketsSignal<TSignal>
        where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>, IHttpWebSocketsSignalHandler<TSignal, TIHandler>
    {
        return transportType switch
        {
            HttpSignalTransportType.Sse => publishers.For(signalTypes).WithTransport(b => b.UseHttpServerSentEvents()).WithDefaultPublisherPipeline(),
            HttpSignalTransportType.WebSockets => publishers.For(signalTypes).WithTransport(b => b.UseHttpWebSockets()).WithDefaultPublisherPipeline(),
            _ => throw new ArgumentOutOfRangeException(nameof(transportType), transportType, null),
        };
    }

    private static void AddDelegateHandlers<TSignal, TIHandler>(
        IServiceCollection services,
        SignalTypes<TSignal, TIHandler> signalTypes,
        HttpSignalTransportType transportType)
        where TSignal : class, IHttpSseSignal<TSignal>, IHttpWebSocketsSignal<TSignal>
        where TIHandler : class, IHttpSseSignalHandler<TSignal, TIHandler>, IHttpWebSocketsSignalHandler<TSignal, TIHandler>
    {
        _ = transportType switch
        {
            HttpSignalTransportType.Sse => services.AddHttpSseSignalHandlerDelegate(
                                                       signalTypes,
                                                       (s, p, ct) => p.GetRequiredService<FnToCallFromHandler>()(s, ct),
                                                       r => r.ServiceProvider.GetRequiredService<Action<IHttpSseSignalReceiver>>()(r))
                                                   .AddHttpSseSignalHandlerDelegate(
                                                       signalTypes,
                                                       (s, p, ct) => p.GetRequiredService<FnToCallFromHandler>()(s, ct),
                                                       p => p.UseReceiverLogging(),
                                                       r => r.ServiceProvider.GetRequiredService<Action<IHttpSseSignalReceiver>>()(r))
                                                   .AddHttpSseSignalHandlerDelegate(
                                                       signalTypes,
                                                       (s, p) => p.GetRequiredService<FnToCallFromHandler>()(s, CancellationToken.None),
                                                       r => r.ServiceProvider.GetRequiredService<Action<IHttpSseSignalReceiver>>()(r))
                                                   .AddHttpSseSignalHandlerDelegate(
                                                       signalTypes,
                                                       (s, p) => p.GetRequiredService<FnToCallFromHandler>()(s, CancellationToken.None),
                                                       p => p.UseReceiverLogging(),
                                                       r => r.ServiceProvider.GetRequiredService<Action<IHttpSseSignalReceiver>>()(r)),
            HttpSignalTransportType.WebSockets => services.AddHttpWebSocketsSignalHandlerDelegate(
                                                              signalTypes,
                                                              (s, p, ct) => p.GetRequiredService<FnToCallFromHandler>()(s, ct),
                                                              r => r.ServiceProvider.GetRequiredService<Action<IHttpWebSocketsSignalReceiver>>()(r))
                                                          .AddHttpWebSocketsSignalHandlerDelegate(
                                                              signalTypes,
                                                              (s, p, ct) => p.GetRequiredService<FnToCallFromHandler>()(s, ct),
                                                              p => p.UseReceiverLogging(),
                                                              r => r.ServiceProvider.GetRequiredService<Action<IHttpWebSocketsSignalReceiver>>()(r))
                                                          .AddHttpWebSocketsSignalHandlerDelegate(
                                                              signalTypes,
                                                              (s, p) => p.GetRequiredService<FnToCallFromHandler>()(s, CancellationToken.None),
                                                              r => r.ServiceProvider.GetRequiredService<Action<IHttpWebSocketsSignalReceiver>>()(r))
                                                          .AddHttpWebSocketsSignalHandlerDelegate(
                                                              signalTypes,
                                                              (s, p) => p.GetRequiredService<FnToCallFromHandler>()(s, CancellationToken.None),
                                                              p => p.UseReceiverLogging(),
                                                              r => r.ServiceProvider.GetRequiredService<Action<IHttpWebSocketsSignalReceiver>>()(r)),
            _ => throw new ArgumentOutOfRangeException(nameof(transportType), transportType, null),
        };
    }

    private static ReceiverExecutionHandle RunReceiverForTransport<THandler>(
        ISignalReceivers r,
        HttpSignalTransportType transportType,
        CancellationToken ct)
        where THandler : class, ISignalHandlerWithSourceGeneration, IHttpSseSignalHandler, IHttpWebSocketsSignalHandler
    {
        return transportType switch
        {
            HttpSignalTransportType.Sse => r.RunHttpSseSignalReceiver<THandler>(ct),
            HttpSignalTransportType.WebSockets => r.RunHttpWebSocketsSignalReceiver<THandler>(ct),
            _ => throw new ArgumentOutOfRangeException(nameof(transportType), transportType, null),
        };
    }

    private static ReceiverExecutionHandle RunReceiversForTransport(
        ISignalReceivers r,
        HttpSignalTransportType transportType,
        CancellationToken ct)
    {
        return transportType switch
        {
            HttpSignalTransportType.Sse => r.RunHttpSseSignalReceivers(ct),
            HttpSignalTransportType.WebSockets => r.RunHttpWebSocketsSignalReceivers(ct),
            _ => throw new ArgumentOutOfRangeException(nameof(transportType), transportType, null),
        };
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignal
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestSignalHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestSignal.IHandler
    {
        static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            => pipeline.Use(ctx =>
            {
                ctx.ServiceProvider.GetRequiredService<ILogger>()
                   .LogInformation("received signal");

                return ctx.Next(ctx.Signal, ctx.CancellationToken);
            });

        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    public sealed partial class DisabledTestSignalHandler : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            throw new InvalidOperationException("This handler should not be called.");
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
        {
            receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);
            receiver.Disable();
        }

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
        {
            receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
            receiver.Disable();
        }
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignal2
    {
        public required int Payload2 { get; init; }
    }

    public sealed partial class MultiTestSignalHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestSignal.IHandler,
          TestSignal2.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    [Signal]
    public sealed partial record NonHttpTestSignal
    {
        public required int Payload { get; init; }
    }

    public sealed partial class MixedWithNonHttpTestSignalHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestSignal.IHandler,
          NonHttpTestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        public async Task Handle(NonHttpTestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    [HttpSseSignal(EventType = "custom")]
    [HttpWebSocketsSignal(Tag = "custom")]
    public sealed partial record TestSignalWithCustomEventTypeOrTag
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestSignalWithCustomEventTypeOrTagHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestSignalWithCustomEventTypeOrTag.IHandler
    {
        public async Task Handle(TestSignalWithCustomEventTypeOrTag signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignalWithoutPayload;

    public sealed partial class TestSignalWithoutPayloadHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestSignalWithoutPayload.IHandler
    {
        public async Task Handle(TestSignalWithoutPayload signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignalWithCustomSerializedPayloadType
    {
        public required TestSignalWithCustomSerializedPayloadTypePayload Payload { get; init; }
    }

    public sealed record TestSignalWithCustomSerializedPayloadTypePayload(int Payload);

    public sealed partial class TestSignalWithCustomSerializedPayloadTypeHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestSignalWithCustomSerializedPayloadType.IHandler
    {
        public async Task Handle(
            TestSignalWithCustomSerializedPayloadType signal,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);

        internal sealed class PayloadJsonConverterFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TestSignalWithCustomSerializedPayloadTypePayload);

            public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                return Activator.CreateInstance(typeof(PayloadJsonConverter)) as JsonConverter;
            }
        }

        internal sealed class PayloadJsonConverter : JsonConverter<TestSignalWithCustomSerializedPayloadTypePayload>
        {
            public override TestSignalWithCustomSerializedPayloadTypePayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new(reader.GetInt32());
            }

            public override void Write(Utf8JsonWriter writer, TestSignalWithCustomSerializedPayloadTypePayload value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.Payload);
            }
        }
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignalWithCustomSerializer
    {
        public required int Payload { get; init; }

        static IHttpSseSignalSerializer<TestSignalWithCustomSerializer> IHttpSseSignal<TestSignalWithCustomSerializer>.HttpSseSignalSerializer
            => new TestSignalCustomSseSerializer();

        static IHttpWebSocketsSignalSerializer<TestSignalWithCustomSerializer> IHttpWebSocketsSignal<TestSignalWithCustomSerializer>.
            HttpWebSocketsSignalSerializer
            => new TestSignalCustomWebSocketsSerializer();
    }

    private sealed class TestSignalCustomSseSerializer : IHttpSseSignalSerializer<TestSignalWithCustomSerializer>
    {
        public Task<string> SerializeSignal(IServiceProvider serviceProvider, TestSignalWithCustomSerializer signal)
        {
            return Task.FromResult($"payload:{signal.Payload}");
        }

        public Task<TestSignalWithCustomSerializer> DeserializeSignal(IServiceProvider serviceProvider, string serializedSignal)
        {
            var result = int.Parse(serializedSignal.Split(':')[1]);

            return Task.FromResult(new TestSignalWithCustomSerializer { Payload = result });
        }
    }

    private sealed class TestSignalCustomWebSocketsSerializer : IHttpWebSocketsSignalSerializer<TestSignalWithCustomSerializer>
    {
        public Task SerializeSignal(
            IServiceProvider serviceProvider,
            TestSignalWithCustomSerializer signal,
            Stream stream,
            CancellationToken cancellationToken)
        {
            return stream.WriteAsync(Encoding.UTF8.GetBytes($"payload:{signal.Payload}"), cancellationToken).AsTask();
        }

        public async Task<TestSignalWithCustomSerializer> DeserializeSignal(IServiceProvider serviceProvider, Stream stream, CancellationToken cancellationToken)
        {
            using var streamReader = new StreamReader(stream, Encoding.UTF8);
            var serializedSignal = await streamReader.ReadToEndAsync(cancellationToken);

            var result = int.Parse(serializedSignal.Split(':')[1]);

            return new() { Payload = result };
        }
    }

    public sealed partial class TestSignalWithCustomSerializerHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestSignalWithCustomSerializer.IHandler
    {
        public async Task Handle(TestSignalWithCustomSerializer signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignalWithCustomJsonTypeInfo
    {
        public int MessagePayload { get; init; }
    }

    public sealed partial class TestSignalWithCustomJsonTypeInfoHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestSignalWithCustomJsonTypeInfo.IHandler
    {
        public async Task Handle(
            TestSignalWithCustomJsonTypeInfo signal,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
    [JsonSerializable(typeof(TestSignalWithCustomJsonTypeInfo))]
    internal sealed partial class TestSignalWithCustomJsonTypeInfoJsonSerializerContext : JsonSerializerContext;

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignalWithMiddleware
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestSignalWithMiddlewareHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestSignalWithMiddleware.IHandler
    {
        public async Task Handle(TestSignalWithMiddleware signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
            =>
                pipeline.Use(pipeline.ServiceProvider.GetRequiredService<TestSignalMiddleware<T>>());

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    public sealed class TestSignalMiddleware<TSignal>(TestObservations observations) : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public Task Execute(SignalMiddlewareContext<TSignal> ctx)
        {
            observations.SeenTransportTypeInMiddleware = ctx.TransportType;

            return ctx.Next(ctx.Signal, ctx.CancellationToken);
        }
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignalForAssemblyScanning
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestSignalForAssemblyScanningHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestSignalForAssemblyScanning.IHandler
    {
        public async Task Handle(TestSignalForAssemblyScanning signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignalWithDelegateHandler
    {
        public required int Payload { get; init; }
    }

    public sealed partial class WildMixTestSignalHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestSignal.IHandler,
          TestSignalWithoutPayload.IHandler,
          TestSignalWithCustomSerializer.IHandler,
          TestSignalWithCustomJsonTypeInfo.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        public async Task Handle(TestSignalWithoutPayload signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        public async Task Handle(TestSignalWithCustomSerializer signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        public async Task Handle(TestSignalWithCustomJsonTypeInfo signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public partial record TestSignalBase(int Payload);

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public partial record TestSignalSub(int Payload, int PayloadSub) : TestSignalBase(Payload);

    public sealed record TestSignalSubSub(int Payload, int PayloadSub, int PayloadSubSub) : TestSignalSub(Payload, PayloadSub);

    private sealed partial class MultiHierarchyTestSignalHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestSignalBase.IHandler,
          TestSignalSub.IHandler
    {
        public async Task Handle(TestSignalBase signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        public async Task Handle(TestSignalSub signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(signal, cancellationToken);
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    private sealed partial class ThrowingTestSignalHandler(
        ConcurrentQueue<Exception?> exceptions,
        FnToCallFromHandler fnToCallFromHandler)
        : TestSignal.IHandler,
          ThrowingTestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            await fnToCallFromHandler(signal, cancellationToken);

            if (exceptions.TryDequeue(out var ex) && ex is not null)
            {
                await Task.Delay(1, cancellationToken);

                throw ex;
            }
        }

        public Task Handle(ThrowingTestSignal signal, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    private sealed partial class ThrowingTestSignalHandler2(
        ConcurrentQueue<Exception?> exceptions,
        FnToCallFromHandler fnToCallFromHandler)
        : TestSignal.IHandler,
          TestSignal2.IHandler,
          ThrowingTestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            await fnToCallFromHandler(signal, cancellationToken);

            if (exceptions.TryDequeue(out var ex) && ex is not null)
            {
                await Task.Delay(1, cancellationToken);

                throw ex;
            }
        }

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            await fnToCallFromHandler(signal, cancellationToken);

            if (exceptions.TryDequeue(out var ex) && ex is not null)
            {
                await Task.Delay(1, cancellationToken);

                throw ex;
            }
        }

        public Task Handle(ThrowingTestSignal signal, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);
    }

    public sealed class TestObservations
    {
        public ConcurrentQueue<string?> ReceivedSignalIds { get; } = [];

        public ConcurrentQueue<string?> ReceivedTraceIds { get; } = [];

        public SignalTransportType? SeenTransportTypeInMiddleware { get; set; }
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    private sealed partial record ThrowingTestSignal
    {
        static IHttpSseSignalSerializer<ThrowingTestSignal> IHttpSseSignal<ThrowingTestSignal>.HttpSseSignalSerializer { get; }
            = new ThrowingTestSignalSerializer();

        static IHttpWebSocketsSignalSerializer<ThrowingTestSignal> IHttpWebSocketsSignal<ThrowingTestSignal>.HttpWebSocketsSignalSerializer { get; }
            = new ThrowingTestSignalSerializer();
    }

    private sealed class ThrowingTestSignalSerializer : IHttpSseSignalSerializer<ThrowingTestSignal>,
                                                        IHttpWebSocketsSignalSerializer<ThrowingTestSignal>
    {
        public Task<string> SerializeSignal(IServiceProvider serviceProvider, ThrowingTestSignal signal)
        {
            throw serviceProvider.GetRequiredService<Exception>();
        }

        public Task<ThrowingTestSignal> DeserializeSignal(IServiceProvider serviceProvider, string serializedSignal)
        {
            throw new NotSupportedException();
        }

        public Task SerializeSignal(
            IServiceProvider serviceProvider,
            ThrowingTestSignal signal,
            Stream stream,
            CancellationToken cancellationToken)
        {
            throw serviceProvider.GetRequiredService<Exception>();
        }

        public Task<ThrowingTestSignal> DeserializeSignal(IServiceProvider serviceProvider, Stream stream, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}

file static class PipelineExtensions
{
    public static ISignalPipeline<TSignal> UsePublishCallback<TSignal>(
        this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal>
    {
        var publishCallback = pipeline.ServiceProvider.GetService<Func<object, ConquerorContext, CancellationToken, Task>>();

        if (publishCallback is null)
        {
            return pipeline;
        }

        return pipeline.Use(async ctx =>
        {
            await publishCallback(ctx.Signal, ctx.ConquerorContext, ctx.CancellationToken);
            await ctx.Next(ctx.Signal, ctx.CancellationToken);
        });
    }

    public static ISignalPipeline<TSignal> UseLogging<TSignal>(this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal>
    {
        var logger = pipeline.ServiceProvider.GetRequiredService<ILogger>();

        return pipeline.Use(ctx =>
        {
            logger.LogInformation("publishing signal...");

            return ctx.Next(ctx.Signal, ctx.CancellationToken);
        });
    }

    public static void UseReceiverLogging<TSignal>(this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal>
    {
        var logger = pipeline.ServiceProvider.GetRequiredService<ILogger>();

        _ = pipeline.Use(ctx =>
        {
            logger.LogInformation("receiving signal...");

            return ctx.Next(ctx.Signal, ctx.CancellationToken);
        });
    }

    public static TIHandler WithDefaultPublisherPipeline<TSignal, TIHandler>(
        this ISignalHandler<TSignal, TIHandler> handler)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return handler.WithPipeline(p => _ = p.UseLogging().UsePublishCallback());
    }
}
