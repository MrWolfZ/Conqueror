using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace Conqueror.Transport.Http.Tests.Signalling;

[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Members are used by ASP.NET Core via reflection")]
public static partial class HttpTestSignals
{
    public delegate Task FnToCallFromHandler(object signal, IServiceProvider serviceProvider);

    public static readonly Uri SseAddress = new("http://localhost/api/signals/sse");
    public static readonly Uri WebSocketsAddress = new("ws://localhost/api/signals/ws");

    public static void MapSignalEndpoints(this IApplicationBuilder app)
    {
        _ = app.UseConquerorWellKnownErrorHandling();
        _ = app.UseRouting().UseWebSockets();

        _ = app.UseEndpoints(endpoints =>
        {
            endpoints.MapMethods("debug/{param:int}", ["GET"], (int param, HttpContext _) => TypedResults.Ok(param))
                     .Finally(e =>
                     {
                         // to allow stepping in with debugger
                         _ = e;
                     });

            _ = endpoints.MapServerSentEventsSignalsEndpoint(SseAddress.AbsolutePath);
            _ = endpoints.MapWebSocketsSignalsEndpoint(WebSocketsAddress.AbsolutePath);
        });
    }

    public static IEnumerable<TestCaseData> GenerateTestCaseData(TransportType transportType)
    {
        return GenerateTestCases(transportType).Select(c => new TestCaseData(c));
    }

    public enum TransportType
    {
        Sse,
        WebSockets,
    }

    private static IEnumerable<HttpSignalTestCase> GenerateTestCases(TransportType transportType)
    {
        var queryParamName = transportType switch
        {
            TransportType.Sse => QueryParameterNames.SignalSseEventType,
            TransportType.WebSockets => QueryParameterNames.SignalWebSocketsTag,
            _ => throw new ArgumentOutOfRangeException(nameof(transportType), transportType, null),
        };

        ISignalPublisher<TSignal> CreatePublisher<TSignal>(ISignalPublisherBuilder<TSignal> builder)
            where TSignal : class, IHttpSseSignal<TSignal>, IHttpWebSocketsSignal<TSignal>
        {
            return transportType switch
            {
                TransportType.Sse => builder.UseHttpServerSentEvents(),
                TransportType.WebSockets => builder.UseHttpWebSockets(),
                _ => throw new ArgumentOutOfRangeException(nameof(transportType), transportType, null),
            };
        }

        SignalReceiverExecutionHandle RunReceiverForTransport<THandler>(ISignalReceivers r, CancellationToken ct)
            where THandler : class, ISignalHandlerWithSourceGeneration, IHttpSseSignalHandler, IHttpWebSocketsSignalHandler
        {
            return transportType switch
            {
                TransportType.Sse => r.RunHttpSseSignalReceiver<THandler>(ct),
                TransportType.WebSockets => r.RunHttpWebSocketsSignalReceiver<THandler>(ct),
                _ => throw new ArgumentOutOfRangeException(nameof(transportType), transportType, null),
            };
        }

        SignalReceiverExecutionHandle RunReceiversForTransport(ISignalReceivers r, CancellationToken ct)
        {
            return transportType switch
            {
                TransportType.Sse => r.RunHttpSseSignalReceivers(ct),
                TransportType.WebSockets => r.RunHttpWebSocketsSignalReceivers(ct),
                _ => throw new ArgumentOutOfRangeException(nameof(transportType), transportType, null),
            };
        }

        foreach (var runIndividually in new[] { true, false })
        {
            yield return new()
            {
                QueryString = QueryStringBuilder.Of(queryParamName, "test"),
                ExpectedPayloads = ["{\"payload\":10}", "{\"payload\":20}"],
                ExpectedEventTypesOrTags = ["test", "test"],
                ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 20 }],
                RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
                PublishSignals = async p =>
                {
                    await p.For(TestSignal.T).WithTransport(CreatePublisher).Handle(new() { Payload = 10 });
                    await p.For(TestSignal.T).WithTransport(CreatePublisher).Handle(new() { Payload = 20 });
                },
                RunReceiver = (r, ct) => runIndividually ? RunReceiverForTransport<TestSignalHandler>(r, ct) : RunReceiversForTransport(r, ct),
            };

            yield return new()
            {
                QueryString = string.Empty,
                ExpectedPayloads = [],
                ExpectedEventTypesOrTags = [],
                ExpectedReceivedSignals = [],
                RegisterHandler = s => s.AddSignalHandler<DisabledTestSignalHandler>(),
                PublishSignals = async p =>
                {
                    await p.For(TestSignal.T).WithTransport(CreatePublisher).Handle(new() { Payload = 10 });
                    await p.For(TestSignal.T).WithTransport(CreatePublisher).Handle(new() { Payload = 20 });
                },
                RunReceiver = (r, ct) => runIndividually ? RunReceiverForTransport<DisabledTestSignalHandler>(r, ct) : RunReceiversForTransport(r, ct),
            };

            yield return new()
            {
                QueryString = QueryStringBuilder.Of((queryParamName, "test"),
                                                    (queryParamName, "testSignal2")),
                ExpectedPayloads = ["{\"payload\":10}", "{\"payload2\":11}", "{\"payload\":20}", "{\"payload2\":21}"],
                ExpectedEventTypesOrTags = ["test", "testSignal2", "test", "testSignal2"],
                ExpectedReceivedSignals =
                [
                    new TestSignal { Payload = 10 },
                    new TestSignal2 { Payload2 = 11 },
                    new TestSignal { Payload = 20 },
                    new TestSignal2 { Payload2 = 21 },
                ],
                RegisterHandler = s => s.AddSignalHandler<MultiTestSignalHandler>(),
                PublishSignals = async p =>
                {
                    await p.For(TestSignal.T).WithTransport(CreatePublisher).Handle(new() { Payload = 10 });
                    await p.For(TestSignal2.T).WithTransport(CreatePublisher).Handle(new() { Payload2 = 11 });
                    await p.For(TestSignal.T).WithTransport(CreatePublisher).Handle(new() { Payload = 20 });
                    await p.For(TestSignal2.T).WithTransport(CreatePublisher).Handle(new() { Payload2 = 21 });
                },
                RunReceiver = (r, ct) => runIndividually ? RunReceiverForTransport<MultiTestSignalHandler>(r, ct) : RunReceiversForTransport(r, ct),
            };

            yield return new()
            {
                QueryString = QueryStringBuilder.Of(queryParamName, "test"),
                ExpectedPayloads = ["{\"payload\":10}", "{\"payload\":20}"],
                ExpectedEventTypesOrTags = ["test", "test"],
                ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 20 }],
                RegisterHandler = s => s.AddSignalHandler<MixedWithNonHttpTestSignalHandler>(),
                PublishSignals = async p =>
                {
                    await p.For(TestSignal.T).WithTransport(CreatePublisher).Handle(new() { Payload = 10 });
                    await p.For(NonHttpTestSignal.T).Handle(new() { Payload = 11 });

                    await p.For(TestSignal.T).WithTransport(CreatePublisher).Handle(new() { Payload = 20 });
                    await p.For(NonHttpTestSignal.T).Handle(new() { Payload = 21 });
                },
                RunReceiver = (r, ct) => runIndividually ? RunReceiverForTransport<MixedWithNonHttpTestSignalHandler>(r, ct) : RunReceiversForTransport(r, ct),
            };

            yield return new()
            {
                QueryString = QueryStringBuilder.Of(queryParamName, "custom"),
                ExpectedPayloads = ["{\"payload\":10}", "{\"payload\":20}"],
                ExpectedEventTypesOrTags = ["custom", "custom"],
                ExpectedReceivedSignals = [new TestSignalWithCustomEventTypeOrTag { Payload = 10 }, new TestSignalWithCustomEventTypeOrTag { Payload = 20 }],
                RegisterHandler = s => s.AddSignalHandler<TestSignalWithCustomEventTypeOrTagHandler>(),
                PublishSignals = async p =>
                {
                    await p.For(TestSignalWithCustomEventTypeOrTag.T).WithTransport(CreatePublisher).Handle(new() { Payload = 10 });
                    await p.For(TestSignalWithCustomEventTypeOrTag.T).WithTransport(CreatePublisher).Handle(new() { Payload = 20 });
                },
                RunReceiver = (r, ct) => runIndividually ? RunReceiverForTransport<TestSignalWithCustomEventTypeOrTagHandler>(r, ct) : RunReceiversForTransport(r, ct),
            };

            yield return new()
            {
                QueryString = QueryStringBuilder.Of(queryParamName, "testSignalWithoutPayload"),
                ExpectedPayloads = ["{}", "{}"],
                ExpectedEventTypesOrTags = ["testSignalWithoutPayload", "testSignalWithoutPayload"],
                ExpectedReceivedSignals = [new TestSignalWithoutPayload(), new TestSignalWithoutPayload()],
                RegisterHandler = s => s.AddSignalHandler<TestSignalWithoutPayloadHandler>(),
                PublishSignals = async p =>
                {
                    await p.For(TestSignalWithoutPayload.T).WithTransport(CreatePublisher).Handle(new());
                    await p.For(TestSignalWithoutPayload.T).WithTransport(CreatePublisher).Handle(new());
                },
                RunReceiver = (r, ct) => runIndividually ? RunReceiverForTransport<TestSignalWithoutPayloadHandler>(r, ct) : RunReceiversForTransport(r, ct),
            };

            yield return new()
            {
                ExpectedPayloads = ["{\"payload\":10}", "{\"payload\":20}"],
                QueryString = QueryStringBuilder.Of(queryParamName, "testSignalWithCustomSerializedPayloadType"),
                ExpectedEventTypesOrTags = ["testSignalWithCustomSerializedPayloadType", "testSignalWithCustomSerializedPayloadType"],
                ExpectedReceivedSignals =
                [
                    new TestSignalWithCustomSerializedPayloadType { Payload = new(10) },
                    new TestSignalWithCustomSerializedPayloadType { Payload = new(20) },
                ],
                RegisterHandler = s =>
                {
                    _ = s.AddSignalHandler<TestSignalWithCustomSerializedPayloadTypeHandler>()
                         .AddSingleton(
                             new JsonSerializerOptions(JsonSerializerDefaults.Web)
                             {
                                 Converters = { new TestSignalWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory() },
                             });
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
                PublishSignals = async p =>
                {
                    await p.For(TestSignalWithCustomSerializedPayloadType.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new() { Payload = new(10) });

                    await p.For(TestSignalWithCustomSerializedPayloadType.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new() { Payload = new(20) });
                },
                RunReceiver = (r, ct) => runIndividually ? RunReceiverForTransport<TestSignalWithCustomSerializedPayloadTypeHandler>(r, ct) : RunReceiversForTransport(r, ct),
            };

            yield return new()
            {
                ExpectedPayloads = ["payload:10", "payload:20"],
                QueryString = QueryStringBuilder.Of(queryParamName, "testSignalWithCustomSerializer"),
                ExpectedEventTypesOrTags = ["testSignalWithCustomSerializer", "testSignalWithCustomSerializer"],
                ExpectedReceivedSignals =
                [
                    new TestSignalWithCustomSerializer { Payload = 10 },
                    new TestSignalWithCustomSerializer { Payload = 20 },
                ],
                RegisterHandler = s => s.AddSignalHandler<TestSignalWithCustomSerializerHandler>(),
                PublishSignals = async p =>
                {
                    await p.For(TestSignalWithCustomSerializer.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new() { Payload = 10 });

                    await p.For(TestSignalWithCustomSerializer.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new() { Payload = 20 });
                },
                RunReceiver = (r, ct) => runIndividually ? RunReceiverForTransport<TestSignalWithCustomSerializerHandler>(r, ct) : RunReceiversForTransport(r, ct),
            };

            yield return new()
            {
                ExpectedPayloads = ["{\"MESSAGE_PAYLOAD\":10}", "{\"MESSAGE_PAYLOAD\":20}"],
                QueryString = QueryStringBuilder.Of(queryParamName, "testSignalWithCustomJsonTypeInfo"),
                ExpectedEventTypesOrTags = ["testSignalWithCustomJsonTypeInfo", "testSignalWithCustomJsonTypeInfo"],
                ExpectedReceivedSignals =
                [
                    new TestSignalWithCustomJsonTypeInfo2 { MessagePayload = 10 },
                    new TestSignalWithCustomJsonTypeInfo2 { MessagePayload = 20 },
                ],
                RegisterHandler = s => s.AddSignalHandler<TestSignalWithCustomJsonTypeInfoHandler>(),
                JsonSerializerContext = GetJsonSerializerContext<TestSignalWithCustomJsonTypeInfo2>(),
                PublishSignals = async p =>
                {
                    await p.For(TestSignalWithCustomJsonTypeInfo2.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new() { MessagePayload = 10 });

                    await p.For(TestSignalWithCustomJsonTypeInfo2.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new() { MessagePayload = 20 });
                },
                RunReceiver = (r, ct) => runIndividually ? RunReceiverForTransport<TestSignalWithCustomJsonTypeInfoHandler>(r, ct) : RunReceiversForTransport(r, ct),
            };

            static JsonSerializerContext? GetJsonSerializerContext<TSignal>()
                where TSignal : class, ISignal<TSignal>
                => TSignal.JsonSerializerContext;

            yield return new()
            {
                ExpectedPayloads = ["{\"payload\":10}", "{\"payload\":20}"],
                QueryString = QueryStringBuilder.Of(queryParamName, "testSignalWithMiddleware"),
                ExpectedEventTypesOrTags = ["testSignalWithMiddleware", "testSignalWithMiddleware"],
                ExpectedReceivedSignals = [new TestSignalWithMiddleware { Payload = 10 }, new TestSignalWithMiddleware { Payload = 20 }],
                RegisterHandler = s => s.AddSignalHandler<TestSignalWithMiddlewareHandler>(),
                PublishSignals = async pubs =>
                {
                    await pubs.For(TestSignalWithMiddleware.T)
                              .WithPipeline(p => p.Use(p.ServiceProvider.GetRequiredService<TestSignalMiddleware<TestSignalWithMiddleware>>()))
                              .WithTransport(CreatePublisher)
                              .Handle(new() { Payload = 10 });

                    await pubs.For(TestSignalWithMiddleware.T)
                              .WithPipeline(p => p.Use(p.ServiceProvider.GetRequiredService<TestSignalMiddleware<TestSignalWithMiddleware>>()))
                              .WithTransport(CreatePublisher)
                              .Handle(new() { Payload = 20 });
                },
                RunReceiver = (r, ct) => runIndividually ? RunReceiverForTransport<TestSignalWithMiddlewareHandler>(r, ct) : RunReceiversForTransport(r, ct),
            };

            yield return new()
            {
                ExpectedPayloads = ["{\"payload\":10}", "{\"payload\":20}"],
                QueryString = QueryStringBuilder.Of(queryParamName, "testSignalForAssemblyScanning"),
                ExpectedEventTypesOrTags = ["testSignalForAssemblyScanning", "testSignalForAssemblyScanning"],
                ExpectedReceivedSignals = [new TestSignalForAssemblyScanning { Payload = 10 }, new TestSignalForAssemblyScanning { Payload = 20 }],
                RegisterHandler = s => s.AddSignalHandlersFromAssembly(typeof(TestSignalForAssemblyScanning).Assembly),
                PublishSignals = async p =>
                {
                    await p.For(TestSignalForAssemblyScanning.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new() { Payload = 10 });

                    await p.For(TestSignalForAssemblyScanning.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new() { Payload = 20 });
                },

                // the assembly scanning will find all the handlers here, so we always run the receiver individually since otherwise
                // all the other handlers would run as well
                RunReceiver = RunReceiverForTransport<TestSignalForAssemblyScanningHandler>,
            };

            yield return new()
            {
                ExpectedPayloads = ["{\"payload\":10}", "{}", "payload:20", "{\"MESSAGE_PAYLOAD\":30}", "{\"payload\":40}"],
                QueryString = QueryStringBuilder.Of((queryParamName, "test"),
                                                    (queryParamName, "testSignalWithoutPayload"),
                                                    (queryParamName, "testSignalWithCustomSerializer"),
                                                    (queryParamName, "testSignalWithCustomJsonTypeInfo")),
                ExpectedEventTypesOrTags = ["test", "testSignalWithoutPayload", "testSignalWithCustomSerializer", "testSignalWithCustomJsonTypeInfo", "test"],
                ExpectedReceivedSignals =
                [
                    new TestSignal { Payload = 10 },
                    new TestSignalWithoutPayload(),
                    new TestSignalWithCustomSerializer { Payload = 20 },
                    new TestSignalWithCustomJsonTypeInfo2 { MessagePayload = 30 },
                    new TestSignal { Payload = 40 },
                ],
                RegisterHandler = s => s.AddSignalHandler<WildMixTestSignalHandler>(),
                PublishSignals = async p =>
                {
                    await p.For(TestSignal.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new() { Payload = 10 });

                    await p.For(TestSignalWithoutPayload.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new());

                    await p.For(TestSignalWithCustomSerializer.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new() { Payload = 20 });

                    await p.For(TestSignalWithCustomJsonTypeInfo2.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new() { MessagePayload = 30 });

                    await p.For(TestSignal.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new() { Payload = 40 });
                },
                RunReceiver = (r, ct) => runIndividually ? RunReceiverForTransport<WildMixTestSignalHandler>(r, ct) : RunReceiversForTransport(r, ct),
            };

            yield return new()
            {
                ExpectedPayloads =
                [
                    "{\"payload\":1}",
                    "{\"payload\":10}",
                    "{\"payload\":20}",
                    "{\"payloadSub\":31,\"payload\":30}",
                    "{\"payloadSub\":41,\"payload\":40}",
                ],
                QueryString = QueryStringBuilder.Of((queryParamName, "testSignalBase"),
                                                    (queryParamName, "testSignalSub")),
                ExpectedEventTypesOrTags = ["testSignalBase", "testSignalBase", "testSignalBase", "testSignalSub", "testSignalSub"],
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
                PublishSignals = async p =>
                {
                    await p.For(TestSignalBase.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new(1));

                    await p.For(TestSignalBase.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new TestSignalSub(10, 11));

                    await p.For(TestSignalBase.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new TestSignalSubSub(20, 21, 22));

                    await p.For(TestSignalSub.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new(30, 31));

                    await p.For(TestSignalSub.T)
                           .WithTransport(CreatePublisher)
                           .Handle(new TestSignalSubSub(40, 41, 42));
                },
                RunReceiver = (r, ct) => runIndividually ? RunReceiverForTransport<MultiHierarchyTestSignalHandler>(r, ct) : RunReceiversForTransport(r, ct),
            };
        }
    }

    public sealed record HttpSignalTestCase
    {
        public required string QueryString { get; init; }

        public required List<string> ExpectedPayloads { get; init; }

        public required List<string> ExpectedEventTypesOrTags { get; init; }

        public JsonSerializerContext? JsonSerializerContext { get; init; }

        public required List<object> ExpectedReceivedSignals { get; init; }

        public required Action<IServiceCollection> RegisterHandler { get; init; }

        public Action<IServiceCollection>? RegisterOnServer { get; init; }

        public required Func<ISignalPublishers, Task> PublishSignals { get; init; }

        public required Func<ISignalReceivers, CancellationToken, SignalReceiverExecutionHandle> RunReceiver { get; init; }

        public IServiceCollection RegisterClientServices(IServiceCollection services)
        {
            RegisterHandler(services);

            return services.AddSingleton<TestObservations>()
                           .AddTransient(typeof(TestSignalMiddleware<>));
        }

        public void RegisterServerServices(IServiceCollection services)
        {
            RegisterOnServer?.Invoke(services);

            _ = services.AddConquerorHttpServerAspNetCore()
                        .AddSingleton<TestObservations>()
                        .AddTransient(typeof(TestSignalMiddleware<>))
                        .AddRouting();
        }

        public override string ToString()
        {
            return $"QueryString:{QueryString};ExpectedReceivedSignalTypes:{string.Join(",", ExpectedReceivedSignals.Select(s => s.GetType().Name))}";
        }
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignal
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestSignalHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestSignal.IHandler
    {
        static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            => pipeline.Use(ctx =>
               {
                   ctx.ServiceProvider.GetRequiredService<ILogger>().LogInformation("received signal");

                   return ctx.Next(ctx.Signal, ctx.CancellationToken);
               });

        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
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

    public sealed partial class MultiTestSignalHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestSignal.IHandler,
          TestSignal2.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
        }

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
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

    public sealed partial class MixedWithNonHttpTestSignalHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestSignal.IHandler,
          NonHttpTestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
        }

        public async Task Handle(NonHttpTestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
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

    public sealed partial class TestSignalWithCustomEventTypeOrTagHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestSignalWithCustomEventTypeOrTag.IHandler
    {
        public async Task Handle(TestSignalWithCustomEventTypeOrTag signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignalWithoutPayload;

    public sealed partial class TestSignalWithoutPayloadHandler(
        IServiceProvider serviceProvider,
        FnToCallFromHandler? fnToCallFromHandler = null)
        : TestSignalWithoutPayload.IHandler
    {
        public async Task Handle(TestSignalWithoutPayload signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
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

    public sealed partial class TestSignalWithCustomSerializedPayloadTypeHandler(
        IServiceProvider serviceProvider,
        FnToCallFromHandler? fnToCallFromHandler = null)
        : TestSignalWithCustomSerializedPayloadType.IHandler
    {
        public async Task Handle(
            TestSignalWithCustomSerializedPayloadType signal,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
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

        static IHttpWebSocketsSignalSerializer<TestSignalWithCustomSerializer> IHttpWebSocketsSignal<TestSignalWithCustomSerializer>.HttpWebSocketsSignalSerializer
            => new TestSignalCustomWebSocketsSerializer();
    }

    private sealed class TestSignalCustomSseSerializer : IHttpSseSignalSerializer<TestSignalWithCustomSerializer>
    {
        public string Serialize(IServiceProvider serviceProvider, TestSignalWithCustomSerializer signal)
        {
            return $"payload:{signal.Payload}";
        }

        public TestSignalWithCustomSerializer Deserialize(IServiceProvider serviceProvider, string serializedSignal)
        {
            var result = int.Parse(serializedSignal.Split(':')[1]);

            return new() { Payload = result };
        }
    }

    private sealed class TestSignalCustomWebSocketsSerializer : IHttpWebSocketsSignalSerializer<TestSignalWithCustomSerializer>
    {
        public ValueTask Serialize(
            IServiceProvider serviceProvider,
            TestSignalWithCustomSerializer signal,
            Stream stream,
            CancellationToken cancellationToken)
        {
            return stream.WriteAsync(Encoding.UTF8.GetBytes($"payload:{signal.Payload}"), cancellationToken);
        }

        public async ValueTask<TestSignalWithCustomSerializer> Deserialize(IServiceProvider serviceProvider, Stream stream, CancellationToken cancellationToken)
        {
            using var streamReader = new StreamReader(stream, Encoding.UTF8);
            var serializedSignal = await streamReader.ReadToEndAsync(cancellationToken);

            var result = int.Parse(serializedSignal.Split(':')[1]);

            return new() { Payload = result };
        }
    }

    public sealed partial class TestSignalWithCustomSerializerHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestSignalWithCustomSerializer.IHandler
    {
        public async Task Handle(TestSignalWithCustomSerializer signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignalWithCustomJsonTypeInfo2
    {
        public int MessagePayload { get; init; }
    }

    public sealed partial class TestSignalWithCustomJsonTypeInfoHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestSignalWithCustomJsonTypeInfo2.IHandler
    {
        public async Task Handle(
            TestSignalWithCustomJsonTypeInfo2 signal,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
    [JsonSerializable(typeof(TestSignalWithCustomJsonTypeInfo2))]
    internal sealed partial class TestSignalWithCustomJsonTypeInfo2JsonSerializerContext : JsonSerializerContext;

    [HttpSseSignal]
    [HttpWebSocketsSignal]
    public sealed partial record TestSignalWithMiddleware
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestSignalWithMiddlewareHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestSignalWithMiddleware.IHandler
    {
        public async Task Handle(TestSignalWithMiddleware signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
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

    public sealed partial class TestSignalForAssemblyScanningHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestSignalForAssemblyScanning.IHandler
    {
        public async Task Handle(TestSignalForAssemblyScanning signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    public sealed partial class WildMixTestSignalHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestSignal.IHandler,
          TestSignalWithoutPayload.IHandler,
          TestSignalWithCustomSerializer.IHandler,
          TestSignalWithCustomJsonTypeInfo2.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
        }

        public async Task Handle(TestSignalWithoutPayload signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
        }

        public async Task Handle(TestSignalWithCustomSerializer signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
        }

        public async Task Handle(TestSignalWithCustomJsonTypeInfo2 signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
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

    private sealed partial class MultiHierarchyTestSignalHandler(IServiceProvider serviceProvider, FnToCallFromHandler? fnToCallFromHandler = null)
        : TestSignalBase.IHandler,
          TestSignalSub.IHandler
    {
        public async Task Handle(TestSignalBase signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
        }

        public async Task Handle(TestSignalSub signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (fnToCallFromHandler is not null)
            {
                await fnToCallFromHandler(signal, serviceProvider);
            }
        }

        static void IHttpSseSignalHandler.ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpSseSignalReceiver>>()?.Invoke(receiver);

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.ServiceProvider.GetService<Action<IHttpWebSocketsSignalReceiver>>()?.Invoke(receiver);
    }

    public sealed class TestObservations
    {
        public ConcurrentQueue<object> ReceivedSignals { get; } = [];

        public ConcurrentQueue<string?> ReceivedSignalIds { get; } = [];

        public ConcurrentQueue<string?> ReceivedTraceIds { get; } = [];

        public SignalTransportType? SeenTransportTypeInMiddleware { get; set; }
    }
}
