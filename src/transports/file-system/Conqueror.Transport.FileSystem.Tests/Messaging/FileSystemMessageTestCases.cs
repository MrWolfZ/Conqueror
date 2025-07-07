namespace Conqueror.Transport.FileSystem.Tests.Messaging;

[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Members are used by ASP.NET Core via reflection")]
public static partial class FileSystemMessageTestCases
{
    public static IEnumerable<FileSystemMessageConformityExecutionSuccessTestCase> CreateSuccessTestCases()
    {
        yield return new()
        {
            Name = "with response",
            Tag = "test",
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessage { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageHandler>(ct),
        };

        yield return new()
        {
            Name = "disabled handler",
            Tag = "test",
            HandlerIsEnabled = false,
            ExpectedReceivedMessages = [],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (_, _) => await Task.FromResult(Array.Empty<object>()),
            ConfigureReceiverFn = (_, r) => r.Disable(),
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageHandler>(ct),
        };

        yield return new()
        {
            Name = "without payload with response",
            Tag = "testMessageWithoutPayload",
            ExpectedReceivedMessages = [new TestMessageWithoutPayload(), new TestMessageWithoutPayload()],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 11 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithoutPayloadHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithoutPayload.T).WithDefaultSenderConfiguration().Handle(new(), ct);
                var r2 = await s.For(TestMessageWithoutPayload.T).WithDefaultSenderConfiguration().Handle(new(), ct);

                return [r1, r2];
            },
            MessagePayloads = [null, null],
            ResponsePayloads = ["{\"payload\":11}", "{\"payload\":11}"],
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithoutPayloadHandler>(ct),
        };

        yield return new()
        {
            Name = "without response",
            Tag = "testMessageWithoutResponse",
            ExpectedReceivedMessages = [new TestMessageWithoutResponse { Payload = 10 }, new TestMessageWithoutResponse { Payload = 20 }],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithoutResponseHandler>(),
            SendMessages = async (s, ct) =>
            {
                await s.For(TestMessageWithoutResponse.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                await s.For(TestMessageWithoutResponse.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithoutResponseHandler>(ct),
        };

        yield return new()
        {
            Name = "disabled handler without response",
            Tag = "testMessageWithoutResponse",
            HandlerIsEnabled = false,
            ExpectedReceivedMessages = [],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithoutResponseHandler>(),
            SendMessages = async (_, _) => await Task.FromResult(Array.Empty<object>()),
            ConfigureReceiverFn = (_, r) => r.Disable(),
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithoutResponseHandler>(ct),
        };

        yield return new()
        {
            Name = "without payload without response",
            Tag = "testMessageWithoutResponseWithoutPayload",
            ExpectedReceivedMessages = [new TestMessageWithoutResponseWithoutPayload(), new TestMessageWithoutResponseWithoutPayload()],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithoutResponseWithoutPayloadHandler>(),
            SendMessages = async (s, ct) =>
            {
                await s.For(TestMessageWithoutResponseWithoutPayload.T).WithDefaultSenderConfiguration().Handle(new(), ct);
                await s.For(TestMessageWithoutResponseWithoutPayload.T).WithDefaultSenderConfiguration().Handle(new(), ct);

                return [];
            },
            MessagePayloads = [null, null],
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithoutResponseWithoutPayloadHandler>(ct),
        };

        yield return new()
        {
            Name = "with tag",
            Tag = "custom-tag",
            ExpectedReceivedMessages = [new TestMessageWithTag { Payload = 10 }, new TestMessageWithTag { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithTagHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithTag.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithTag.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithTagHandler>(ct),
        };

        yield return new()
        {
            Name = "with version",
            Tag = "test-message-with-version-v2",
            ExpectedReceivedMessages = [new TestMessageWithVersion { Payload = 10 }, new TestMessageWithVersion { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithVersionHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithVersion.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithVersion.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithVersionHandler>(ct),
        };

        yield return new()
        {
            Name = "with version",
            Tag = "test-message-v2",
            ExpectedReceivedMessages = [new TestMessageV2 { Payload = 10 }, new TestMessageV2 { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageV2Handler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageV2.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageV2.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageV2Handler>(ct),
        };

        yield return new()
        {
            Name = "with custom serialized payload type",
            Tag = "testMessageWithCustomSerializedPayloadType",
            ExpectedReceivedMessages =
                [new TestMessageWithCustomSerializedPayloadType { Payload = new(10) }, new TestMessageWithCustomSerializedPayloadType { Payload = new(20) }],
            ExpectedResponses =
            [
                new TestMessageWithCustomSerializedPayloadTypeResponse { Payload = new(11) },
                new TestMessageWithCustomSerializedPayloadTypeResponse { Payload = new(21) },
            ],
            RegisterHandler = services =>
            {
                _ = services.AddMessageHandler<TestMessageWithCustomSerializedPayloadTypeHandler>();

                var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                jsonSerializerOptions.Converters.Add(new TestMessageWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory());
                jsonSerializerOptions.MakeReadOnly(true);

                _ = services.AddSingleton(jsonSerializerOptions);
            },
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithCustomSerializedPayloadType.T).WithDefaultSenderConfiguration().Handle(new() { Payload = new(10) }, ct);
                var r2 = await s.For(TestMessageWithCustomSerializedPayloadType.T).WithDefaultSenderConfiguration().Handle(new() { Payload = new(20) }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithCustomSerializedPayloadTypeHandler>(ct),

            RegisterOnSender = services =>
            {
                var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                jsonSerializerOptions.Converters.Add(new TestMessageWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory());
                jsonSerializerOptions.MakeReadOnly(true);

                _ = services.AddSingleton(jsonSerializerOptions);
            },
            MessagePayloads = ["{\"payload\":10}", "{\"payload\":20}"],
            ResponsePayloads = ["{\"payload\":11}", "{\"payload\":21}"],
        };

        yield return new()
        {
            Name = "with custom serializer",
            Tag = "testMessageWithCustomSerializer",
            ExpectedReceivedMessages =
            [
                new TestMessageWithCustomSerializer { Payload = 10 },
                new TestMessageWithCustomSerializer { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageWithCustomSerializerResponse { Payload = 11 }, new TestMessageWithCustomSerializerResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithCustomSerializerHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithCustomSerializer.T)
                                .WithDefaultSenderConfiguration()
                                .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithCustomSerializer.T)
                                .WithDefaultSenderConfiguration()
                                .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithCustomSerializerHandler>(ct),
            MessagePayloads = ["payload:10", "payload:20"],
            ResponsePayloads = ["payload:11", "payload:21"],
        };

        yield return new()
        {
            Name = "with custom JSON type info",
            Tag = "testMessageWithCustomJsonTypeInfo",
            ExpectedReceivedMessages =
            [
                new TestMessageWithCustomJsonTypeInfo { MessagePayload = 10 },
                new TestMessageWithCustomJsonTypeInfo { MessagePayload = 20 },
            ],
            ExpectedResponses =
            [
                new TestMessageWithCustomJsonTypeInfoResponse { ResponsePayload = 11 }, new TestMessageWithCustomJsonTypeInfoResponse { ResponsePayload = 21 },
            ],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithCustomJsonTypeInfoHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithCustomJsonTypeInfo.T)
                                .WithDefaultSenderConfiguration()
                                .Handle(new() { MessagePayload = 10 }, ct);
                var r2 = await s.For(TestMessageWithCustomJsonTypeInfo.T)
                                .WithDefaultSenderConfiguration()
                                .Handle(new() { MessagePayload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithCustomJsonTypeInfoHandler>(ct),
            MessagePayloads = ["{\"MESSAGE_PAYLOAD\":10}", "{\"MESSAGE_PAYLOAD\":20}"],
            ResponsePayloads = ["{\"RESPONSE_PAYLOAD\":11}", "{\"RESPONSE_PAYLOAD\":21}"],
        };

        yield return new()
        {
            Name = "with middleware",
            Tag = "testMessageWithMiddleware",
            ExpectedReceivedMessages = [new TestMessageWithMiddleware { Payload = 10 }, new TestMessageWithMiddleware { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithMiddlewareHandler>()
                                    .AddSingleton<TestObservations>()
                                    .AddTransient(typeof(TestMessageMiddleware<,>)),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithMiddleware.T)
                                .WithDefaultSenderConfiguration()
                                .WithPipeline(p => p.Use(
                                                  p.ServiceProvider
                                                   .GetRequiredService<TestMessageMiddleware<TestMessageWithMiddleware, TestMessageResponse>>()))
                                .Handle(new() { Payload = 10 }, ct);

                var r2 = await s.For(TestMessageWithMiddleware.T)
                                .WithDefaultSenderConfiguration()
                                .WithPipeline(p => p.Use(
                                                  p.ServiceProvider
                                                   .GetRequiredService<TestMessageMiddleware<TestMessageWithMiddleware, TestMessageResponse>>()))
                                .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithMiddlewareHandler>(ct),

            RegisterOnSender = s => s.AddSingleton<TestObservations>()
                                     .AddTransient(typeof(TestMessageMiddleware<,>)),

            AfterMessagesAreReceived = h =>
            {
                var seenTransportTypeOnServer = h.ReceiverHosts.First().Resolve<TestObservations>().SeenTransportTypeInMiddleware;
                Assert.That(seenTransportTypeOnServer?.IsFileSystem(), Is.True, $"transport type is {seenTransportTypeOnServer?.Name}");
                Assert.That(seenTransportTypeOnServer?.Role, Is.EqualTo(MessageTransportRole.Receiver));

                var seenTransportTypeOnClient = h.SenderHost.Resolve<TestObservations>().SeenTransportTypeInMiddleware;
                Assert.That(seenTransportTypeOnClient?.IsFileSystem(), Is.True, $"transport type is {seenTransportTypeOnClient?.Name}");
                Assert.That(seenTransportTypeOnClient?.Role, Is.EqualTo(MessageTransportRole.Sender));

                return Task.CompletedTask;
            },
        };

        yield return new()
        {
            Name = "without response with middleware",
            Tag = "testMessageWithMiddlewareWithoutResponse",
            ExpectedReceivedMessages =
                [new TestMessageWithMiddlewareWithoutResponse { Payload = 10 }, new TestMessageWithMiddlewareWithoutResponse { Payload = 20 }],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithMiddlewareWithoutResponseHandler>()
                                    .AddSingleton<TestObservations>()
                                    .AddTransient(typeof(TestMessageMiddleware<,>)),
            SendMessages = async (s, ct) =>
            {
                await s.For(TestMessageWithMiddlewareWithoutResponse.T)
                       .WithDefaultSenderConfiguration()
                       .WithPipeline(p => p.Use(
                                         p.ServiceProvider
                                          .GetRequiredService<TestMessageMiddleware<TestMessageWithMiddlewareWithoutResponse, UnitMessageResponse>>()))
                       .Handle(new() { Payload = 10 }, ct);

                await s.For(TestMessageWithMiddlewareWithoutResponse.T)
                       .WithDefaultSenderConfiguration()
                       .WithPipeline(p => p.Use(
                                         p.ServiceProvider
                                          .GetRequiredService<TestMessageMiddleware<TestMessageWithMiddlewareWithoutResponse, UnitMessageResponse>>()))
                       .Handle(new() { Payload = 20 }, ct);

                return [];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithMiddlewareWithoutResponseHandler>(ct),

            RegisterOnSender = s => s.AddSingleton<TestObservations>()
                                     .AddTransient(typeof(TestMessageMiddleware<,>)),

            AfterMessagesAreReceived = h =>
            {
                var seenTransportTypeOnServer = h.ReceiverHosts.First().Resolve<TestObservations>().SeenTransportTypeInMiddleware;
                Assert.That(seenTransportTypeOnServer?.IsFileSystem(), Is.True, $"transport type is {seenTransportTypeOnServer?.Name}");
                Assert.That(seenTransportTypeOnServer?.Role, Is.EqualTo(MessageTransportRole.Receiver));

                var seenTransportTypeOnClient = h.SenderHost.Resolve<TestObservations>().SeenTransportTypeInMiddleware;
                Assert.That(seenTransportTypeOnClient?.IsFileSystem(), Is.True, $"transport type is {seenTransportTypeOnClient?.Name}");
                Assert.That(seenTransportTypeOnClient?.Role, Is.EqualTo(MessageTransportRole.Sender));

                return Task.CompletedTask;
            },
        };

        yield return new()
        {
            Name = "with array response",
            Tag = "testMessageWithArrayResponse",
            ExpectedReceivedMessages = [new TestMessageWithArrayResponse { Payload = 10 }, new TestMessageWithArrayResponse { Payload = 20 }],
            ExpectedResponses =
            [
                new TestMessageResponse[] { new() { Payload = 11 }, new() { Payload = 12 } },
                new TestMessageResponse[] { new() { Payload = 21 }, new() { Payload = 22 } },
            ],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithArrayResponseHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithArrayResponse.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithArrayResponse.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithArrayResponseHandler>(ct),
            ResponsePayloads = ["[{\"payload\":11},{\"payload\":12}]", "[{\"payload\":21},{\"payload\":22}]"],
        };

        yield return new()
        {
            Name = "with list response",
            Tag = "testMessageWithListResponse",
            ExpectedReceivedMessages = [new TestMessageWithListResponse { Payload = 10 }, new TestMessageWithListResponse { Payload = 20 }],
            ExpectedResponses =
            [
                new List<TestMessageResponse> { new() { Payload = 11 }, new() { Payload = 12 } },
                new List<TestMessageResponse> { new() { Payload = 21 }, new() { Payload = 22 } },
            ],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithListResponseHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithListResponse.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithListResponse.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithListResponseHandler>(ct),
            ResponsePayloads = ["[{\"payload\":11},{\"payload\":12}]", "[{\"payload\":21},{\"payload\":22}]"],
        };

        yield return new()
        {
            Name = "with enumerable response",
            Tag = "testMessageWithEnumerableResponse",
            SingleResponseType = typeof(IEnumerable<TestMessageResponse>),
            ExpectedReceivedMessages = [new TestMessageWithEnumerableResponse { Payload = 10 }, new TestMessageWithEnumerableResponse { Payload = 20 }],
            ExpectedResponses =
            [
                new List<TestMessageResponse> { new() { Payload = 11 }, new() { Payload = 12 } },
                new List<TestMessageResponse> { new() { Payload = 21 }, new() { Payload = 22 } },
            ],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithEnumerableResponseHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithEnumerableResponse.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithEnumerableResponse.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithEnumerableResponseHandler>(ct),
            ResponsePayloads = ["[{\"payload\":11},{\"payload\":12}]", "[{\"payload\":21},{\"payload\":22}]"],
        };

        yield return new()
        {
            Name = "from assembly scanning",
            Tag = "testMessageForAssemblyScanning",
            ExpectedReceivedMessages = [new TestMessageForAssemblyScanning { Payload = 10 }, new TestMessageForAssemblyScanning { Payload = 20 }],
            ExpectedResponses = [new TestMessageForAssemblyScanningResponse { Payload = 11 }, new TestMessageForAssemblyScanningResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandlersFromAssembly(typeof(TestMessageForAssemblyScanningHandler).Assembly)
                                    .AddSingleton<TestObservations>()
                                    .AddTransient(typeof(TestMessageMiddleware<,>)),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageForAssemblyScanning.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageForAssemblyScanning.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageForAssemblyScanningHandler>(ct),
        };

        yield return new()
        {
            Name = "without response from assembly scanning",
            Tag = "testMessageWithoutResponseForAssemblyScanning",
            ExpectedReceivedMessages =
                [new TestMessageWithoutResponseForAssemblyScanning { Payload = 10 }, new TestMessageWithoutResponseForAssemblyScanning { Payload = 20 }],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandlersFromAssembly(typeof(TestMessageWithoutResponseForAssemblyScanningHandler).Assembly)
                                    .AddSingleton<TestObservations>()
                                    .AddTransient(typeof(TestMessageMiddleware<,>)),
            SendMessages = async (s, ct) =>
            {
                await s.For(TestMessageWithoutResponseForAssemblyScanning.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                await s.For(TestMessageWithoutResponseForAssemblyScanning.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithoutResponseForAssemblyScanningHandler>(ct),
        };

        foreach (var t in from hasResponse in new[] { true, false }
                          from isSync in new[] { true, false }
                          from configuresPipeline in new[] { true, false }
                          select (hasResponse, isSync, configuresPipeline))
        {
            var middlewareCallCount = 0;
            var receiverConfigurationCount = 0;

            yield return new()
            {
                Name =
                    $"with delegate (hasResponse: {t.hasResponse}, isSync: {t.isSync}, configuresPipeline: {t.configuresPipeline})",
                Tag = t.hasResponse ? "testMessageWithDelegateHandler" : "testMessageWithDelegateHandlerWithoutResponse",
                ExpectedReceivedMessages = t.hasResponse
                    ? [new TestMessageWithDelegateHandler { Payload = 10 }, new TestMessageWithDelegateHandler { Payload = 20 }]
                    : [new TestMessageWithDelegateHandlerWithoutResponse { Payload = 10 }, new TestMessageWithDelegateHandlerWithoutResponse { Payload = 20 }],
                ExpectedResponses = t.hasResponse
                    ? [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }]
                    : [],
                RegisterHandler = s =>
                {
                    _ = (t.hasResponse, t.isSync, t.configuresPipeline) switch
                    {
                        (false, false, false) => s.AddFileSystemMessageHandlerDelegate(
                            TestMessageWithDelegateHandlerWithoutResponse.T,
                            (m, p, ct) => p.GetRequiredService<FnToCallFromHandler>()(m, ct),
                            configureReceiver: r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(r);
                            }),
                        (true, false, false) => s.AddFileSystemMessageHandlerDelegate(
                            TestMessageWithDelegateHandler.T,
                            async (m, p, ct) =>
                            {
                                await p.GetRequiredService<FnToCallFromHandler>()(m, ct);

                                return new() { Payload = m.Payload + 1 };
                            },
                            configureReceiver: r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(r);
                            }),
                        (false, true, false) => s.AddFileSystemMessageHandlerDelegate(
                            TestMessageWithDelegateHandlerWithoutResponse.T,
                            (m, p) => p.GetRequiredService<FnToCallFromHandler>()(m, CancellationToken.None).GetAwaiter().GetResult(),
                            configureReceiver: r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(r);
                            }),
                        (true, true, false) => s.AddFileSystemMessageHandlerDelegate(
                            TestMessageWithDelegateHandler.T,
                            async (m, p, ct) =>
                            {
                                await p.GetRequiredService<FnToCallFromHandler>()(m, ct);

                                return new() { Payload = m.Payload + 1 };
                            },
                            configureReceiver: r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(r);
                            }),
                        (false, false, true) => s.AddFileSystemMessageHandlerDelegate(
                            TestMessageWithDelegateHandlerWithoutResponse.T,
                            (m, p, ct) => p.GetRequiredService<FnToCallFromHandler>()(m, ct),
                            p => p.Use(ctx =>
                            {
                                _ = Interlocked.Increment(ref middlewareCallCount);

                                return ctx.Next(ctx.Message, ctx.CancellationToken);
                            }),
                            r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(r);
                            }),
                        (true, false, true) => s.AddFileSystemMessageHandlerDelegate(
                            TestMessageWithDelegateHandler.T,
                            async (m, p, ct) =>
                            {
                                await p.GetRequiredService<FnToCallFromHandler>()(m, ct);

                                return new() { Payload = m.Payload + 1 };
                            },
                            p => p.Use(ctx =>
                            {
                                _ = Interlocked.Increment(ref middlewareCallCount);

                                return ctx.Next(ctx.Message, ctx.CancellationToken);
                            }),
                            r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(r);
                            }),
                        (false, true, true) => s.AddFileSystemMessageHandlerDelegate(
                            TestMessageWithDelegateHandlerWithoutResponse.T,
                            (m, p) => p.GetRequiredService<FnToCallFromHandler>()(m, CancellationToken.None).GetAwaiter().GetResult(),
                            p => p.Use(ctx =>
                            {
                                _ = Interlocked.Increment(ref middlewareCallCount);

                                return ctx.Next(ctx.Message, ctx.CancellationToken);
                            }),
                            r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(r);
                            }),
                        (true, true, true) => s.AddFileSystemMessageHandlerDelegate(
                            TestMessageWithDelegateHandler.T,
                            (m, p) =>
                            {
                                p.GetRequiredService<FnToCallFromHandler>()(m, CancellationToken.None).GetAwaiter().GetResult();

                                return new() { Payload = m.Payload + 1 };
                            },
                            p => p.Use(ctx =>
                            {
                                _ = Interlocked.Increment(ref middlewareCallCount);

                                return ctx.Next(ctx.Message, ctx.CancellationToken);
                            }),
                            r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(r);
                            }),
                    };
                },
                SendMessages = async (s, ct) =>
                {
                    if (t.hasResponse)
                    {
                        var r1 = await s.For(TestMessageWithDelegateHandler.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                        var r2 = await s.For(TestMessageWithDelegateHandler.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                        return [r1, r2];
                    }

                    await s.For(TestMessageWithDelegateHandlerWithoutResponse.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                    await s.For(TestMessageWithDelegateHandlerWithoutResponse.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                    return [];
                },
                RunReceivers = (r, ct) => r.RunFileSystemMessageReceivers(ct),
                AfterMessagesAreReceived = _ =>
                {
                    Assert.That(middlewareCallCount, Is.EqualTo(t.configuresPipeline ? 2 : 0));
                    Assert.That(receiverConfigurationCount, Is.EqualTo(1));

                    middlewareCallCount = 0;
                    receiverConfigurationCount = 0;

                    return Task.CompletedTask;
                },
            };
        }

        yield return new()
        {
            Name = "with response in parallel",
            MessagesAreSentInParallel = true,
            Tag = "test",
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessage { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var responses = await Task.WhenAll(
                    s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct),
                    s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct));

                return responses;
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageHandler>(ct),
        };

        yield return new()
        {
            Name = "multiple independent receivers",
            NumOfReceivers = 2,
            Tag = string.Empty, // makes no sense with multiple different message types
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessageWithTag { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>()
                                    .AddMessageHandler<TestMessageWithTagHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithTag.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceivers(ct),
        };

        yield return new()
        {
            Name = "multiple competing receivers",
            NumOfReceivers = 2,
            Tag = string.Empty, // makes no sense with multiple different message types
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessage { Payload = 20 }, new TestMessage { Payload = 30 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }, new TestMessageResponse { Payload = 31 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);
                var r3 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 30 }, ct);

                return [r1, r2, r3];
            },
            RunReceivers = (r, ct) =>
            {
                var handle1 = r.RunFileSystemMessageReceiver<TestMessageHandler>(ct);
                var handle2 = r.RunFileSystemMessageReceiver<TestMessageHandler>(ct);

                return r.CombineExecutions([handle1, handle2]);
            },
        };

        yield return new()
        {
            Name = "type hierarchy with response",
            Tag = string.Empty, // makes no sense with multiple different message types
            ExpectedReceivedMessages = [new TestMessageBase(10), new TestMessageSub(20, 30), new TestMessageSub(40, 50)],
            ExpectedResponses =
            [
                new TestMessageResponse { Payload = 11 },
                new TestMessageResponse { Payload = 22 },
                new TestMessageResponse { Payload = 42 },
            ],
            RegisterHandler = s => s.AddMessageHandler<MultiHierarchyTestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageBase.T).WithDefaultSenderConfiguration().Handle(new(10), ct);
                var r2 = await s.For(TestMessageSub.T).WithDefaultSenderConfiguration().Handle(new(20, 30), ct);
                var r3 = await s.For(TestMessageSub.T).WithDefaultSenderConfiguration().Handle(new TestMessageSubSub(40, 50, 60), ct);

                return [r1, r2, r3];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<MultiHierarchyTestMessageHandler>(ct),
        };

        yield return new()
        {
            Name = "type hierarchy without response",
            Tag = string.Empty, // makes no sense with multiple different message types
            ExpectedReceivedMessages =
                [new TestMessageBaseWithoutResponse(10), new TestMessageSubWithoutResponse(20, 30), new TestMessageSubWithoutResponse(40, 50)],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandler<MultiHierarchyTestMessageWithoutResponseHandler>(),
            SendMessages = async (s, ct) =>
            {
                await s.For(TestMessageBaseWithoutResponse.T).WithDefaultSenderConfiguration().Handle(new(10), ct);
                await s.For(TestMessageSubWithoutResponse.T).WithDefaultSenderConfiguration().Handle(new(20, 30), ct);
                await s.For(TestMessageSubWithoutResponse.T).WithDefaultSenderConfiguration().Handle(new TestMessageSubSubWithoutResponse(40, 50, 60), ct);

                return [];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<MultiHierarchyTestMessageWithoutResponseHandler>(ct),
        };
    }

    public static IEnumerable<FileSystemMessageConformityExecutionSuccessTestCase> CreateSimpleSuccessTestCases()
    {
        yield return new()
        {
            Name = "with response",
            Tag = "test",
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessage { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageHandler>(ct),
        };

        yield return new()
        {
            Name = "without response",
            Tag = "test",
            ExpectedReceivedMessages = [new TestMessageWithoutResponse { Payload = 10 }, new TestMessageWithoutResponse { Payload = 20 }],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithoutResponseHandler>(),
            SendMessages = async (s, ct) =>
            {
                await s.For(TestMessageWithoutResponse.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                await s.For(TestMessageWithoutResponse.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageWithoutResponseHandler>(ct),
        };

        yield return new()
        {
            Name = "multiple independent receivers",
            NumOfReceivers = 2,
            Tag = string.Empty, // makes no sense with multiple different message types
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessageWithTag { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>()
                                    .AddMessageHandler<TestMessageWithTagHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithTag.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceivers(ct),
        };

        yield return new()
        {
            Name = "multiple competing receivers",
            NumOfReceivers = 2,
            Tag = string.Empty, // makes no sense with multiple different message types
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessage { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            RunReceivers = (r, ct) =>
            {
                var handle1 = r.RunFileSystemMessageReceiver<TestMessageHandler>(ct);
                var handle2 = r.RunFileSystemMessageReceiver<TestMessageHandler>(ct);

                return r.CombineExecutions([handle1, handle2]);
            },
        };
    }

    public static IEnumerable<FileSystemMessageConformityExecutionErrorTestCase> CreateErrorTestCases()
    {
        yield return new()
        {
            Name = "single handler with configuration error",
            ExpectedReceivedMessages = [],
            ExpectedResponses = [],
            ConfigurationExceptions = [new InvalidOperationException("configuration error")],
            SendException = null,
            HandlerExceptions = [],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (_, _) => await Task.FromResult(Array.Empty<object>()),
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageHandler>(ct),
        };

        yield return new()
        {
            Name = "multiple handlers, second with configuration error",
            ExpectedReceivedMessages = [],
            ExpectedResponses = [],
            ConfigurationExceptions = [null, new InvalidOperationException("configuration error")],
            SendException = null,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            SendMessages = (_, _) => Task.FromResult<IReadOnlyCollection<object>>([]),
            RegisterHandler = s => s.AddMessageHandler<ThrowingTestMessageHandler>()
                                    .AddMessageHandler<ThrowingTestMessageHandler2>(),
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceivers(ct),
        };

        yield return new()
        {
            Name = "single handler with unrecoverable connection error",
            ExpectedReceivedMessages = [],
            ExpectedResponses = [],
            ConfigurationExceptions = [],
            SendException = null,
            NumOfExpectedUnrecoverableConnectionErrors = 1,
            HandlerExceptions = [],
            SendMessages = (_, _) => Task.FromResult<IReadOnlyCollection<object>>([]),
            RegisterHandler = s => s.AddMessageHandler<ThrowingTestMessageHandler>(),
            RunReceivers = (r, ct) =>
            {
                r.ServiceProvider.GetRequiredService<DirectoryInfo>().Delete(true);

                return r.RunFileSystemMessageReceiver<ThrowingTestMessageHandler>(ct);
            },
        };

        yield return new()
        {
            Name = "multiple handlers with unrecoverable connection errors",
            ExpectedReceivedMessages = [],
            ExpectedResponses = [],
            ConfigurationExceptions = [],
            SendException = null,
            NumOfExpectedUnrecoverableConnectionErrors = 2,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            SendMessages = (_, _) => Task.FromResult<IReadOnlyCollection<object>>([]),
            RegisterHandler = s => s.AddMessageHandler<ThrowingTestMessageHandler>()
                                    .AddMessageHandler<ThrowingTestMessageHandler2>(),
            RunReceivers = (r, ct) =>
            {
                r.ServiceProvider.GetRequiredService<DirectoryInfo>().Delete(true);

                return r.RunFileSystemMessageReceivers(ct);
            },
        };

        yield return new()
        {
            Name = "multiple handlers, one of which with unrecoverable connection error",
            ExpectedReceivedMessages = [],
            ExpectedResponses = [],
            ConfigurationExceptions = [],
            SendException = null,
            NumOfExpectedUnrecoverableConnectionErrors = 1,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            SendMessages = (_, _) => Task.FromResult<IReadOnlyCollection<object>>([]),
            RegisterHandler = s => s.AddMessageHandler<ThrowingTestMessageHandler>()
                                    .AddMessageHandler<ThrowingTestMessageHandler2>(),
            RunReceivers = (r, ct) =>
            {
                var handle1 = r.RunFileSystemMessageReceiver<ThrowingTestMessageHandler>(ct);

                handle1.InitialConnectionTask.Wait(ct);

                r.ServiceProvider.GetRequiredService<DirectoryInfo>().Delete(true);

                var handle2 = r.RunFileSystemMessageReceiver<ThrowingTestMessageHandler2>(ct);

                return r.CombineExecutions([handle1, handle2]);
            },
        };

        yield return new()
        {
            Name = "single handler with handler exception",
            ExpectedReceivedMessages =
            [
                new TestMessage { Payload = 10 },
                new TestMessage { Payload = 30 },
            ],
            ExpectedResponses =
            [
                new TestMessageResponse { Payload = 11 },
            ],
            ConfigurationExceptions = [],
            SendException = null,
            HandlerExceptions =
            [
                null,
                new InvalidOperationException("handler exception"),
            ],
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T)
                                .WithDefaultSenderConfiguration()
                                .Handle(new() { Payload = 10 }, ct);

                await Task.Delay(10, ct);

                await s.For(TestMessageWithoutResponse.T)
                       .WithDefaultSenderConfiguration()
                       .Handle(new() { Payload = 20 }, ct);

                await Task.Delay(10, ct);

                var r2 = await s.For(TestMessage.T)
                                .WithDefaultSenderConfiguration()
                                .Handle(new() { Payload = 30 }, ct);

                await Task.Delay(10, ct);

                var r3 = await s.For(TestMessage.T)
                                .WithDefaultSenderConfiguration()
                                .Handle(new() { Payload = 40 }, ct);

                return [r1, r2, r3];
            },
            RegisterHandler = s => s.AddMessageHandler<ThrowingTestMessageHandler>(),
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<ThrowingTestMessageHandler>(ct),
        };

        yield return new()
        {
            Name = "multiple handlers, one of which with handler exception",
            ExpectedReceivedMessages =
            [
                new TestMessage { Payload = 10 },
                new TestMessageWithoutResponse { Payload = 20 },
                new TestMessage { Payload = 30 },
            ],
            ExpectedResponses =
            [
                new TestMessageResponse { Payload = 11 },
            ],
            ConfigurationExceptions = [],
            SendException = null,
            NumOfReceivers = 2,
            HandlerExceptions =
            [
                null,
                null,
                new InvalidOperationException("handler exception"),
            ],
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T)
                                .WithDefaultSenderConfiguration()
                                .Handle(new() { Payload = 10 }, ct);

                await Task.Delay(10, ct);

                await s.For(TestMessageWithoutResponse.T)
                       .WithDefaultSenderConfiguration()
                       .Handle(new() { Payload = 20 }, ct);

                await Task.Delay(50, ct);

                var r2 = await s.For(TestMessage.T)
                                .WithDefaultSenderConfiguration()
                                .Handle(new() { Payload = 30 }, ct);

                return [r1, r2];
            },
            RegisterHandler = s => s.AddMessageHandler<ThrowingTestMessageHandler>()
                                    .AddMessageHandler<ThrowingTestMessageHandler2>(),
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceivers(ct),
        };

        yield return new()
        {
            Name = "single receiver with send error",
            ExpectedReceivedMessages = [],
            ExpectedResponses = [],
            ConfigurationExceptions = [],
            SendException = new InvalidOperationException("send error"),
            HandlerExceptions = [],
            RegisterHandler = s => s.AddMessageHandler<ThrowingTestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var response = await s.For(ThrowingTestMessage.T)
                                      .WithDefaultSenderConfiguration()
                                      .Handle(new() { Payload = 10 }, ct);

                return [response];
            },
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<ThrowingTestMessageHandler>(ct),
        };

        yield return new()
        {
            Name = "multiple receivers with send error",
            ExpectedReceivedMessages = [],
            ExpectedResponses = [],
            ConfigurationExceptions = [],
            SendException = new InvalidOperationException("send error"),
            NumOfReceivers = 2,
            HandlerExceptions = [],
            SendMessages = async (s, ct) =>
            {
                var response = await s.For(ThrowingTestMessage.T)
                                      .WithDefaultSenderConfiguration()
                                      .Handle(new() { Payload = 10 }, ct);

                return [response];
            },
            RegisterHandler = s => s.AddMessageHandler<ThrowingTestMessageHandler>()
                                    .AddMessageHandler<ThrowingTestMessageHandler2>(),
            RunReceivers = (r, ct) => r.RunFileSystemMessageReceivers(ct),
        };
    }

    public static IEnumerable<FileSystemMessageConformityContextTestCase> CreateContextTestCases()
    {
        foreach (var t in from hasActivity in new[] { true, false }
                          from hasDownstream in new[] { true, false }
                          from hasUpstream in new[] { true, false }
                          from hasBidirectional in new[] { true, false }
                          select (hasActivity, hasDownstream, hasUpstream, hasBidirectional))
        {
            yield return new()
            {
                Name =
                    $"single receiver, {nameof(t.hasActivity)}: {t.hasActivity}, {nameof(t.hasDownstream)}: {t.hasDownstream}, {nameof(t.hasUpstream)}: {t.hasUpstream}, {nameof(t.hasBidirectional)}: {t.hasBidirectional}",
                HasActivity = t.hasActivity,
                HasDownstreamData = t.hasDownstream,
                HasUpstreamData = t.hasUpstream,
                HasBidirectionalData = t.hasBidirectional,
                RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
                SendMessages = async (s, ct) =>
                {
                    var response = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);

                    return [response];
                },
                RunReceivers = (r, ct) => r.RunFileSystemMessageReceiver<TestMessageHandler>(ct),
            };
        }
    }

    [FileSystemMessage<TestMessageResponse>]
    public sealed partial record TestMessage
    {
        public required int Payload { get; init; }
    }

    public sealed record TestMessageResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessage.IHandler
    {
        public static void ConfigurePipeline(TestMessage.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return new() { Payload = message.Payload + 1 };
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage<TestMessageResponse>]
    public sealed partial record TestMessageWithoutPayload;

    public sealed partial class TestMessageWithoutPayloadHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithoutPayload.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithoutPayload.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(TestMessageWithoutPayload message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return new() { Payload = 11 };
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage]
    public sealed partial record TestMessageWithoutResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithoutResponseHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithoutResponse.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithoutResponse.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage]
    public sealed partial record TestMessageWithoutResponseWithoutPayload;

    public sealed partial class TestMessageWithoutResponseWithoutPayloadHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithoutResponseWithoutPayload.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithoutResponseWithoutPayload.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task Handle(TestMessageWithoutResponseWithoutPayload message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage<TestMessageResponse>(Tag = "custom-tag")]
    public sealed partial record TestMessageWithTag
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithTagHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithTag.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithTag.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(TestMessageWithTag message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return new() { Payload = message.Payload + 1 };
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage<TestMessageResponse>(Version = "v2")]
    public sealed partial record TestMessageWithVersion
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithVersionHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithVersion.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithVersion.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(TestMessageWithVersion message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return new() { Payload = message.Payload + 1 };
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage<TestMessageResponse>(Version = "v2")]
    public sealed partial record TestMessageV2
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageV2Handler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageV2.IHandler
    {
        public static void ConfigurePipeline(TestMessageV2.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(TestMessageV2 message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return new() { Payload = message.Payload + 1 };
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage<TestMessageWithCustomSerializedPayloadTypeResponse>]
    public sealed partial record TestMessageWithCustomSerializedPayloadType
    {
        public required TestMessageWithCustomSerializedPayloadTypePayload Payload { get; init; }
    }

    public sealed record TestMessageWithCustomSerializedPayloadTypeResponse
    {
        public required TestMessageWithCustomSerializedPayloadTypePayload Payload { get; init; }
    }

    public sealed record TestMessageWithCustomSerializedPayloadTypePayload(int Payload);

    public sealed partial class TestMessageWithCustomSerializedPayloadTypeHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithCustomSerializedPayloadType.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithCustomSerializedPayloadType.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageWithCustomSerializedPayloadTypeResponse> Handle(
            TestMessageWithCustomSerializedPayloadType message,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return new() { Payload = new(message.Payload.Payload + 1) };
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);

        internal sealed class PayloadJsonConverterFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TestMessageWithCustomSerializedPayloadTypePayload);

            public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                return Activator.CreateInstance(typeof(PayloadJsonConverter)) as JsonConverter;
            }
        }

        internal sealed class PayloadJsonConverter : JsonConverter<TestMessageWithCustomSerializedPayloadTypePayload>
        {
            public override TestMessageWithCustomSerializedPayloadTypePayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new(reader.GetInt32());
            }

            public override void Write(Utf8JsonWriter writer, TestMessageWithCustomSerializedPayloadTypePayload value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.Payload);
            }
        }
    }

    [FileSystemMessage<TestMessageWithCustomSerializerResponse>]
    public sealed partial record TestMessageWithCustomSerializer
    {
        public int Payload { get; init; }

        static IFileSystemMessageSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>
            IFileSystemMessage<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>.FileSystemMessageSerializer { get; }
            = new TestMessageCustomSerializer();

        static IFileSystemMessageResponseSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>
            IFileSystemMessage<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>.FileSystemMessageResponseSerializer { get; }
            = new TestMessageCustomSerializer();
    }

    public sealed record TestMessageWithCustomSerializerResponse
    {
        public required int Payload { get; init; }
    }

    private sealed class TestMessageCustomSerializer : IFileSystemMessageSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>,
                                                       IFileSystemMessageResponseSerializer<TestMessageWithCustomSerializer,
                                                           TestMessageWithCustomSerializerResponse>
    {
        public string FileExtension => ".txt";

        public async Task SerializeMessage(
            IServiceProvider serviceProvider,
            TestMessageWithCustomSerializer message,
            Stream fileStream,
            CancellationToken cancellationToken)
        {
            await Task.Yield();

            await using var writer = new StreamWriter(fileStream, Encoding.UTF8, leaveOpen: true);
            await writer.WriteAsync($"payload:{message.Payload}");
        }

        public async Task<TestMessageWithCustomSerializer> DeserializeMessage(
            IServiceProvider serviceProvider,
            Stream fileStream,
            CancellationToken cancellationToken)
        {
            await Task.Yield();
            using var reader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true);
            var bodyContent = await reader.ReadToEndAsync(cancellationToken);
            var payload = int.Parse(bodyContent.Split(':')[1]);

            return new() { Payload = payload };
        }

        public async Task SerializeResponse(
            IServiceProvider serviceProvider,
            TestMessageWithCustomSerializerResponse response,
            Stream fileStream,
            CancellationToken cancellationToken)
        {
            await using var writer = new StreamWriter(fileStream);
            await writer.WriteAsync($"total-payload:{response.Payload}");
        }

        public async Task<TestMessageWithCustomSerializerResponse> DeserializeResponse(
            IServiceProvider serviceProvider,
            Stream fileStream,
            CancellationToken cancellationToken)
        {
            await Task.Yield();
            using var reader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true);
            var bodyContent = await reader.ReadToEndAsync(cancellationToken);
            var payload = int.Parse(bodyContent.Split(':')[1]);

            return new() { Payload = payload };
        }
    }

    public sealed partial class TestMessageWithCustomSerializerHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithCustomSerializer.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithCustomSerializer.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageWithCustomSerializerResponse> Handle(
            TestMessageWithCustomSerializer message,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return new() { Payload = message.Payload + 1 };
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage<TestMessageWithCustomJsonTypeInfoResponse>]
    public sealed partial record TestMessageWithCustomJsonTypeInfo
    {
        public int MessagePayload { get; init; }
    }

    public sealed record TestMessageWithCustomJsonTypeInfoResponse
    {
        public int ResponsePayload { get; init; }
    }

    public sealed partial class TestMessageWithCustomJsonTypeInfoHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithCustomJsonTypeInfo.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithCustomJsonTypeInfo.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageWithCustomJsonTypeInfoResponse> Handle(
            TestMessageWithCustomJsonTypeInfo message,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return new() { ResponsePayload = message.MessagePayload + 1 };
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
    [JsonSerializable(typeof(TestMessageWithCustomJsonTypeInfo))]
    [JsonSerializable(typeof(TestMessageWithCustomJsonTypeInfoResponse))]
    internal sealed partial class TestMessageWithCustomJsonTypeInfoJsonSerializerContext : JsonSerializerContext;

    [FileSystemMessage<TestMessageResponse>]
    public sealed partial record TestMessageWithMiddleware
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithMiddlewareHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithMiddleware.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithMiddleware.IPipeline pipeline) =>
            pipeline.UseReceiverLogging()
                    .Use(pipeline.ServiceProvider.GetRequiredService<TestMessageMiddleware<TestMessageWithMiddleware, TestMessageResponse>>());

        public async Task<TestMessageResponse> Handle(TestMessageWithMiddleware message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return new() { Payload = message.Payload + 1 };
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage]
    public sealed partial record TestMessageWithMiddlewareWithoutResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithMiddlewareWithoutResponseHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithMiddlewareWithoutResponse.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithMiddlewareWithoutResponse.IPipeline pipeline) =>
            pipeline.UseReceiverLogging()
                    .Use(pipeline.ServiceProvider.GetRequiredService<TestMessageMiddleware<TestMessageWithMiddlewareWithoutResponse, UnitMessageResponse>>());

        public async Task Handle(TestMessageWithMiddlewareWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    public sealed class TestMessageMiddleware<TMessage, TResponse>(TestObservations observations) : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            observations.SeenTransportTypeInMiddleware = ctx.TransportType;

            return ctx.Next(ctx.Message, ctx.CancellationToken);
        }
    }

    [FileSystemMessage<TestMessageResponse[]>]
    public sealed partial record TestMessageWithArrayResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithArrayResponseHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithArrayResponse.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithArrayResponse.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse[]> Handle(TestMessageWithArrayResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return [new() { Payload = message.Payload + 1 }, new() { Payload = message.Payload + 2 }];
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage<List<TestMessageResponse>>]
    public sealed partial record TestMessageWithListResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithListResponseHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithListResponse.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithListResponse.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<List<TestMessageResponse>> Handle(TestMessageWithListResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return [new() { Payload = message.Payload + 1 }, new() { Payload = message.Payload + 2 }];
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage<IEnumerable<TestMessageResponse>>]
    public sealed partial record TestMessageWithEnumerableResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithEnumerableResponseHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithEnumerableResponse.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithEnumerableResponse.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<IEnumerable<TestMessageResponse>> Handle(TestMessageWithEnumerableResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return [new() { Payload = message.Payload + 1 }, new() { Payload = message.Payload + 2 }];
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage<TestMessageForAssemblyScanningResponse>]
    public sealed partial record TestMessageForAssemblyScanning
    {
        public required int Payload { get; init; }
    }

    public sealed record TestMessageForAssemblyScanningResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageForAssemblyScanningHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageForAssemblyScanning.IHandler
    {
        public static void ConfigurePipeline(TestMessageForAssemblyScanning.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageForAssemblyScanningResponse> Handle(TestMessageForAssemblyScanning message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return new() { Payload = message.Payload + 1 };
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage]
    public sealed partial record TestMessageWithoutResponseForAssemblyScanning
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithoutResponseForAssemblyScanningHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithoutResponseForAssemblyScanning.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithoutResponseForAssemblyScanning.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task Handle(TestMessageWithoutResponseForAssemblyScanning message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage<TestMessageResponse>]
    public sealed partial record TestMessageWithDelegateHandler
    {
        public required int Payload { get; init; }
    }

    [FileSystemMessage]
    public sealed partial record TestMessageWithDelegateHandlerWithoutResponse
    {
        public required int Payload { get; init; }
    }

    [FileSystemMessage<TestMessageResponse>]
    public partial record TestMessageBase(int Payload);

    [FileSystemMessage<TestMessageResponse>]
    public partial record TestMessageSub(int Payload, int PayloadSub) : TestMessageBase(Payload);

    public sealed record TestMessageSubSub(int Payload, int PayloadSub, int PayloadSubSub) : TestMessageSub(Payload, PayloadSub);

    private sealed partial class MultiHierarchyTestMessageHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageBase.IHandler,
          TestMessageSub.IHandler
    {
        public static void ConfigurePipeline(TestMessageBase.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public static void ConfigurePipeline(TestMessageSub.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(TestMessageBase message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return new() { Payload = message.Payload + 1 };
        }

        public async Task<TestMessageResponse> Handle(TestMessageSub message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);

            return new() { Payload = message.Payload + 2 };
        }

        static void IFileSystemMessageHandler.ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage]
    public partial record TestMessageBaseWithoutResponse(int Payload);

    [FileSystemMessage]
    public partial record TestMessageSubWithoutResponse(int Payload, int PayloadSub) : TestMessageBaseWithoutResponse(Payload);

    public sealed record TestMessageSubSubWithoutResponse(int Payload, int PayloadSub, int PayloadSubSub) : TestMessageSubWithoutResponse(Payload, PayloadSub);

    private sealed partial class MultiHierarchyTestMessageWithoutResponseHandler(FnToCallFromHandler fnToCallFromHandler)
        : TestMessageBaseWithoutResponse.IHandler,
          TestMessageSubWithoutResponse.IHandler
    {
        public static void ConfigurePipeline(TestMessageBaseWithoutResponse.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public static void ConfigurePipeline(TestMessageSubWithoutResponse.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task Handle(TestMessageBaseWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);
        }

        public async Task Handle(TestMessageSubWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await fnToCallFromHandler(message, cancellationToken);
        }

        static void IFileSystemMessageHandler.ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    [FileSystemMessage<TestMessageResponse>]
    private sealed partial record ThrowingTestMessage
    {
        public required int Payload { get; init; }

        static IFileSystemMessageSerializer<ThrowingTestMessage, TestMessageResponse> IFileSystemMessage<ThrowingTestMessage, TestMessageResponse>
            .FileSystemMessageSerializer { get; } = new ThrowingTestMessageSerializer();
    }

    private sealed class ThrowingTestMessageSerializer : IFileSystemMessageSerializer<ThrowingTestMessage, TestMessageResponse>
    {
        public string FileExtension => ".throwing";

        public Task SerializeMessage(
            IServiceProvider serviceProvider,
            ThrowingTestMessage message,
            Stream fileStream,
            CancellationToken cancellationToken)
        {
            throw serviceProvider.GetRequiredService<Exception>();
        }

        public Task<ThrowingTestMessage> DeserializeMessage(
            IServiceProvider serviceProvider,
            Stream fileStream,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed partial class ThrowingTestMessageHandler(
        ConcurrentQueue<Exception?> exceptions,
        FnToCallFromHandler fnToCallFromHandler)
        : TestMessage.IHandler,
          ThrowingTestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            await fnToCallFromHandler(message, cancellationToken);

            if (exceptions.TryDequeue(out var ex) && ex is not null)
            {
                await Task.Delay(1, cancellationToken);

                throw ex;
            }

            return new() { Payload = message.Payload + 1 };
        }

        public Task<TestMessageResponse> Handle(ThrowingTestMessage message, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        static void IFileSystemMessageHandler.ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    private sealed partial class ThrowingTestMessageHandler2(
        ConcurrentQueue<Exception?> exceptions,
        FnToCallFromHandler fnToCallFromHandler)
        : TestMessageWithoutResponse.IHandler,
          ThrowingTestMessage.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            await fnToCallFromHandler(message, cancellationToken);

            if (exceptions.TryDequeue(out var ex) && ex is not null)
            {
                await Task.Delay(1, cancellationToken);

                throw ex;
            }
        }

        public Task<TestMessageResponse> Handle(ThrowingTestMessage message, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        static void IFileSystemMessageHandler.ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
            => receiver.ServiceProvider.GetRequiredService<Action<IFileSystemMessageReceiver>>().Invoke(receiver);
    }

    public sealed class TestObservations
    {
        public ConcurrentQueue<string?> ReceivedMessageIds { get; } = [];

        public ConcurrentQueue<string?> ReceivedTraceIds { get; } = [];

        public MessageTransportType? SeenTransportTypeInMiddleware { get; set; }
    }
}

file static class PipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseSendCallback<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var sendCallback = pipeline.ServiceProvider.GetService<Func<object, ConquerorContext, CancellationToken, Task>>();

        if (sendCallback is null)
        {
            return pipeline;
        }

        return pipeline.Use(async ctx =>
        {
            await sendCallback(ctx.Message, ctx.ConquerorContext, ctx.CancellationToken);

            return await ctx.Next(ctx.Message, ctx.CancellationToken);
        });
    }

    public static IMessagePipeline<TMessage, TResponse> UseLogging<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var logger = pipeline.ServiceProvider.GetRequiredService<ILogger>();

        return pipeline.Use(ctx =>
        {
            logger.LogInformation("sending message...");

            return ctx.Next(ctx.Message, ctx.CancellationToken);
        });
    }

    public static IMessagePipeline<TMessage, TResponse> UseReceiverLogging<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var logger = pipeline.ServiceProvider.GetRequiredService<ILogger>();

        return pipeline.Use(ctx =>
        {
            logger.LogInformation("receiving message...");

            return ctx.Next(ctx.Message, ctx.CancellationToken);
        });
    }

    public static TIHandler WithDefaultSenderConfiguration<TMessage, TResponse, TIHandler>(
        this IMessageHandler<TMessage, TResponse, TIHandler> handler)
        where TMessage : class, IFileSystemMessage<TMessage, TResponse>
        where TIHandler : class, IFileSystemMessageHandler<TMessage, TResponse, TIHandler>
    {
        return handler.WithPipeline(p => _ = p.UseLogging().UseSendCallback())
                      .WithTransport(b => b.UseFileSystem(
                                         b.ServiceProvider.GetRequiredService<DirectoryInfo>().FullName,
                                         pollingInterval: TimeSpan.FromMilliseconds(10)));
    }
}
