namespace Conqueror.Transport.FileSystem.Tests.Signalling;

using System.Globalization;

[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage(
    "ReSharper",
    "UnusedAutoPropertyAccessor.Global",
    Justification = "Members are used by ASP.NET Core via reflection"
)]
public static partial class FileSystemSignalTestCases
{
    [SuppressMessage(
        "Roslynator",
        "RCS1250:Use implicit/explicit object creation",
        Justification = "it is clear what objects are being created here"
    )]
    public static IEnumerable<FileSystemSignalConformityExecutionSuccessTestCase> CreateSuccessTestCases()
    {
        yield return new()
        {
            Name = "single receiver",
            ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<TestSignalHandler>(ct),
        };

        yield return new()
        {
            Name = "single receiver with parallel publish",
            ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await Task.WhenAll(
                    p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct),
                    p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 20 }, ct)
                );
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<TestSignalHandler>(ct),
            SignalsArePublishedInParallel = true,
        };

        yield return new()
        {
            Name = "single receiver multiple times",
            ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) =>
                r.CombineExecutions(
                    [
                        r.RunFileSystemSignalReceiver<TestSignalHandler>(ct),
                        r.RunFileSystemSignalReceiver<TestSignalHandler>(ct),
                    ]
                ),
            NumOfReceivers = 2,
        };

        yield return new()
        {
            Name = "multiple receivers",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>().AddSignalHandler<MultiTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);
                await p.For(TestSignal2.T).WithDefaultPublisherConfiguration().Handle(new() { Payload2 = 20 }, ct);
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 30 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceivers(ct),
            NumOfReceivers = 2,
        };

        yield return new()
        {
            Name = "multiple receivers with parallel publish",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>().AddSignalHandler<MultiTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await Task.WhenAll(
                    p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct),
                    p.For(TestSignal2.T).WithDefaultPublisherConfiguration().Handle(new() { Payload2 = 20 }, ct),
                    p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 30 }, ct)
                );
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceivers(ct),
            NumOfReceivers = 2,
            SignalsArePublishedInParallel = true,
        };

        yield return new()
        {
            Name = "disabled receiver",
            ExpectedReceivedSignals = [],
            ShouldCompleteImmediately = true,
            RegisterHandler = s => s.AddSignalHandler<DisabledTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<DisabledTestSignalHandler>(ct),
        };

        yield return new()
        {
            Name = "receiver for multiple signal types",
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
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);
                await p.For(TestSignal2.T).WithDefaultPublisherConfiguration().Handle(new() { Payload2 = 11 }, ct);
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 20 }, ct);
                await p.For(TestSignal2.T).WithDefaultPublisherConfiguration().Handle(new() { Payload2 = 21 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<MultiTestSignalHandler>(ct),
        };

        yield return new()
        {
            Name = "handler for file-system and non-file-system signal",
            ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandler<MixedWithNonFileSystemTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);
                await p.For(NonFileSystemTestSignal.T).Handle(new() { Payload = 11 }, ct);
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 20 }, ct);
                await p.For(NonFileSystemTestSignal.T).Handle(new() { Payload = 21 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<MixedWithNonFileSystemTestSignalHandler>(ct),
        };

        yield return new()
        {
            Name = "signal with custom event type or tag",
            ExpectedReceivedSignals =
            [
                new TestSignalWithCustomEventTypeOrTag { Payload = 10 },
                new TestSignalWithCustomEventTypeOrTag { Payload = 20 },
            ],
            RegisterHandler = s => s.AddSignalHandler<TestSignalWithCustomEventTypeOrTagHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignalWithCustomEventTypeOrTag.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                await p.For(TestSignalWithCustomEventTypeOrTag.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<TestSignalWithCustomEventTypeOrTagHandler>(ct),
        };

        yield return new()
        {
            Name = "signal without payload",
            ExpectedReceivedSignals = [new TestSignalWithoutPayload(), new TestSignalWithoutPayload()],
            RegisterHandler = s => s.AddSignalHandler<TestSignalWithoutPayloadHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignalWithoutPayload.T).WithDefaultPublisherConfiguration().Handle(new(), ct);
                await p.For(TestSignalWithoutPayload.T).WithDefaultPublisherConfiguration().Handle(new(), ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<TestSignalWithoutPayloadHandler>(ct),
        };

        yield return new()
        {
            Name = "signal with custom serialized payload type",
            ExpectedReceivedSignals =
            [
                new TestSignalWithCustomSerializedPayloadType { Payload = new(Payload: 10) },
                new TestSignalWithCustomSerializedPayloadType { Payload = new(Payload: 20) },
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
            RegisterOnPublisher = s =>
            {
                var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    Converters = { new TestSignalWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory() },
                };

                jsonSerializerOptions.MakeReadOnly(populateMissingResolver: true);

                _ = s.AddSignalHandler<TestSignalWithCustomSerializedPayloadTypeHandler>()
                    .AddSingleton(jsonSerializerOptions);
            },
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignalWithCustomSerializedPayloadType.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { Payload = new(Payload: 10) }, ct);
                await p.For(TestSignalWithCustomSerializedPayloadType.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { Payload = new(Payload: 20) }, ct);
            },
            RunReceivers = (r, ct) =>
                r.RunFileSystemSignalReceiver<TestSignalWithCustomSerializedPayloadTypeHandler>(ct),
        };

        yield return new()
        {
            Name = "signal with custom serializer",
            ExpectedReceivedSignals =
            [
                new TestSignalWithCustomSerializer { Payload = 10 },
                new TestSignalWithCustomSerializer { Payload = 20 },
            ],
            RegisterHandler = s => s.AddSignalHandler<TestSignalWithCustomSerializerHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignalWithCustomSerializer.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                await p.For(TestSignalWithCustomSerializer.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<TestSignalWithCustomSerializerHandler>(ct),
        };

        yield return new()
        {
            Name = "signal with custom type info",
            ExpectedReceivedSignals =
            [
                new TestSignalWithCustomJsonTypeInfo { SignalPayload = 10 },
                new TestSignalWithCustomJsonTypeInfo { SignalPayload = 20 },
            ],
            RegisterHandler = s => s.AddSignalHandler<TestSignalWithCustomJsonTypeInfoHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignalWithCustomJsonTypeInfo.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { SignalPayload = 10 }, ct);
                await p.For(TestSignalWithCustomJsonTypeInfo.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { SignalPayload = 20 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<TestSignalWithCustomJsonTypeInfoHandler>(ct),
        };

        yield return new()
        {
            Name = "receiver and publisher with middleware",
            ExpectedReceivedSignals =
            [
                new TestSignalWithMiddleware { Payload = 10 },
                new TestSignalWithMiddleware { Payload = 20 },
            ],
            RegisterHandler = s =>
                s.AddSignalHandler<TestSignalWithMiddlewareHandler>()
                    .AddTransient<TestSignalMiddleware<TestSignalWithMiddleware>>()
                    .AddSingleton<TestObservations>(),
            RegisterOnPublisher = s =>
                s.AddTransient<TestSignalMiddleware<TestSignalWithMiddleware>>().AddSingleton<TestObservations>(),
            PublishSignals = async (sp, ct) =>
            {
                await sp.For(TestSignalWithMiddleware.T)
                    .WithDefaultPublisherConfiguration()
                    .WithPipeline(p =>
                        p.Use(p.ServiceProvider.GetRequiredService<TestSignalMiddleware<TestSignalWithMiddleware>>())
                    )
                    .Handle(new() { Payload = 10 }, ct);

                await sp.For(TestSignalWithMiddleware.T)
                    .WithDefaultPublisherConfiguration()
                    .WithPipeline(p =>
                        p.Use(p.ServiceProvider.GetRequiredService<TestSignalMiddleware<TestSignalWithMiddleware>>())
                    )
                    .Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<TestSignalWithMiddlewareHandler>(ct),
            AfterSignalsAreReceived = host =>
            {
                var seenTransportTypeOnServer = host
                    .PublisherHost.Resolve<TestObservations>()
                    .SeenTransportTypeInMiddleware;
                var isCorrectTransportTypeOnServer = seenTransportTypeOnServer?.IsFileSystem();

                Assert.That(
                    isCorrectTransportTypeOnServer,
                    Is.True,
                    $"transport type is {seenTransportTypeOnServer?.Name}"
                );
                Assert.That(seenTransportTypeOnServer?.Role, Is.EqualTo(SignalTransportRole.Publisher));

                foreach (var receiverHost in host.ReceiverHosts)
                {
                    var seenTransportTypeOnClient = receiverHost
                        .Resolve<TestObservations>()
                        .SeenTransportTypeInMiddleware;
                    var isCorrectTransportTypeOnClient = seenTransportTypeOnClient?.IsFileSystem();

                    Assert.That(
                        isCorrectTransportTypeOnClient,
                        Is.True,
                        $"transport type is {seenTransportTypeOnClient?.Name}"
                    );
                    Assert.That(seenTransportTypeOnClient?.Role, Is.EqualTo(SignalTransportRole.Receiver));
                }

                return Task.CompletedTask;
            },
        };

        yield return new()
        {
            Name = "handler discovered via assembly scanning",
            ExpectedReceivedSignals =
            [
                new TestSignalForAssemblyScanning { Payload = 10 },
                new TestSignalForAssemblyScanning { Payload = 20 },
            ],
            RegisterHandler = s => s.AddSignalHandlersFromAssembly(typeof(TestSignalForAssemblyScanning).Assembly),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignalForAssemblyScanning.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                await p.For(TestSignalForAssemblyScanning.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<TestSignalForAssemblyScanningHandler>(ct),
        };

        yield return new()
        {
            Name = "delegate handlers",
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
            RegisterHandler = s => AddDelegateHandlers(s, TestSignalWithDelegateHandler.T),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignalWithDelegateHandler.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                await p.For(TestSignalWithDelegateHandler.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceivers(ct),
            NumOfReceivers = 4,
        };

        yield return new()
        {
            Name = "wild mix of signal types",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignalWithoutPayload(),
                new TestSignalWithCustomSerializer { Payload = 20 },
                new TestSignalWithCustomJsonTypeInfo { SignalPayload = 30 },
                new TestSignal { Payload = 40 },
            ],
            RegisterHandler = s => s.AddSignalHandler<WildMixTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);
                await p.For(TestSignalWithoutPayload.T).WithDefaultPublisherConfiguration().Handle(new(), ct);
                await p.For(TestSignalWithCustomSerializer.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { Payload = 20 }, ct);
                await p.For(TestSignalWithCustomJsonTypeInfo.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new() { SignalPayload = 30 }, ct);
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 40 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<WildMixTestSignalHandler>(ct),
        };

        yield return new()
        {
            Name = "signal with hierarchy",
            ExpectedReceivedSignals =
            [
                new TestSignalBase(Payload: 1),
                // because we publish these events through TestSignalBase, the event type will be "testSignalBase"
                // and the signal will be deserialized as the base type
                new TestSignalBase(Payload: 10),
                new TestSignalBase(Payload: 20),
                // since the receiver observes multiple types from the type hierarchy, we expect each signal
                // to be received twice, but the type of the received signal will be the type that the publisher
                // was using
                new TestSignalSub(Payload: 30, PayloadSub: 31),
                new TestSignalSub(Payload: 30, PayloadSub: 31),
                // because we publish these events through TestSignalSub, the event type will be "testSignalSub"
                // and the signal will be deserialized as that type instead of TestSignalSubSub
                new TestSignalSub(Payload: 40, PayloadSub: 41),
                new TestSignalSub(Payload: 40, PayloadSub: 41),
            ],
            RegisterHandler = s => s.AddSignalHandler<MultiHierarchyTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignalBase.T).WithDefaultPublisherConfiguration().Handle(new(Payload: 1), ct);
                await p.For(TestSignalBase.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new TestSignalSub(Payload: 10, PayloadSub: 11), ct);
                await p.For(TestSignalBase.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new TestSignalSubSub(Payload: 20, PayloadSub: 21, PayloadSubSub: 22), ct);
                await p.For(TestSignalSub.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new(Payload: 30, PayloadSub: 31), ct);
                await p.For(TestSignalSub.T)
                    .WithDefaultPublisherConfiguration()
                    .Handle(new TestSignalSubSub(Payload: 40, PayloadSub: 41, PayloadSubSub: 42), ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<MultiHierarchyTestSignalHandler>(ct),
        };
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1250:Use implicit/explicit object creation",
        Justification = "it is clear what objects are being created here"
    )]
    public static IEnumerable<FileSystemSignalConformityExecutionSuccessTestCase> CreateSimpleSuccessTestCases()
    {
        yield return new()
        {
            Name = "single receiver",
            ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 20 }],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 20 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<TestSignalHandler>(ct),
        };

        yield return new()
        {
            Name = "multiple receivers",
            ExpectedReceivedSignals =
            [
                new TestSignal { Payload = 10 },
                new TestSignal { Payload = 10 },
                new TestSignal2 { Payload2 = 20 },
                new TestSignal { Payload = 30 },
                new TestSignal { Payload = 30 },
            ],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>().AddSignalHandler<MultiTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);
                await p.For(TestSignal2.T).WithDefaultPublisherConfiguration().Handle(new() { Payload2 = 20 }, ct);
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 30 }, ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceivers(ct),
            NumOfReceivers = 2,
        };
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1250:Use implicit/explicit object creation",
        Justification = "it is clear what objects are being created here"
    )]
    public static IEnumerable<FileSystemSignalConformityExecutionErrorTestCase> CreateErrorTestCases()
    {
        yield return new()
        {
            Name = "single handler with configuration error",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [new InvalidOperationException("configuration error")],
            PublishException = null,
            HandlerExceptions = [],
            RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
            PublishSignals = (_, _) => Task.CompletedTask,
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<TestSignalHandler>(ct),
        };

        yield return new()
        {
            Name = "multiple handlers, second with configuration error",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [null, new InvalidOperationException("configuration error")],
            PublishException = null,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandler = s =>
                s.AddSignalHandler<ThrowingTestSignalHandler>().AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceivers(ct),
        };

        yield return new()
        {
            Name = "single handler with unrecoverable connection error",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            PublishException = null,
            NumOfExpectedUnrecoverableConnectionErrors = 1,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) =>
            {
                r.ServiceProvider.GetRequiredService<DirectoryInfo>().Delete(recursive: true);

                return r.RunFileSystemSignalReceiver<ThrowingTestSignalHandler>(ct);
            },
        };

        yield return new()
        {
            Name = "multiple handlers with unrecoverable connection errors",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            PublishException = null,
            NumOfExpectedUnrecoverableConnectionErrors = 2,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandler = s =>
                s.AddSignalHandler<ThrowingTestSignalHandler>().AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) =>
            {
                r.ServiceProvider.GetRequiredService<DirectoryInfo>().Delete(recursive: true);

                return r.RunFileSystemSignalReceivers(ct);
            },
        };

        yield return new()
        {
            Name = "multiple handlers, one of which with unrecoverable connection error",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            PublishException = null,
            NumOfExpectedUnrecoverableConnectionErrors = 1,
            NumOfReceivers = 2,
            HandlerExceptions = [],
            PublishSignals = (_, _) => Task.CompletedTask,
            RegisterHandler = s =>
                s.AddSignalHandler<ThrowingTestSignalHandler>().AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) =>
            {
                var handle1 = r.RunFileSystemSignalReceiver<ThrowingTestSignalHandler>(ct);

#pragma warning disable MA0045 // we want to test a sync delegate here
                handle1.InitialConnectionTask.Wait(ct);
#pragma warning restore MA0045

                // disposing the file stores will trigger the next handler to throw on startup
                r.ServiceProvider.GetRequiredService<FileSystemStores>().Dispose();

                var handle2 = r.RunFileSystemSignalReceiver<ThrowingTestSignalHandler2>(ct);

                return r.CombineExecutions([handle1, handle2]);
            },
        };

        yield return new()
        {
            Name = "single handler with handler exception",
            ExpectedReceivedSignals = [new TestSignal { Payload = 10 }, new TestSignal { Payload = 30 }],
            ConfigurationExceptions = [],
            PublishException = null,
            HandlerExceptions = [null, new InvalidOperationException("handler exception")],
            PublishSignals = async (p, ct) =>
            {
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);

                await Task.Delay(millisecondsDelay: 10, ct);

                await p.For(TestSignal2.T).WithDefaultPublisherConfiguration().Handle(new() { Payload2 = 20 }, ct);

                await Task.Delay(millisecondsDelay: 10, ct);

                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 30 }, ct);

                await Task.Delay(millisecondsDelay: 10, ct);

                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 40 }, ct);
            },
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<ThrowingTestSignalHandler>(ct),
        };

        yield return new()
        {
            Name = "multiple handlers with handler exceptions",
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
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);

                await Task.Delay(millisecondsDelay: 10, ct);

                await p.For(TestSignal2.T).WithDefaultPublisherConfiguration().Handle(new() { Payload2 = 20 }, ct);

                await Task.Delay(millisecondsDelay: 50, ct);

                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 30 }, ct);

                await Task.Delay(millisecondsDelay: 10, ct);

                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 40 }, ct);
            },
            RegisterHandler = s =>
                s.AddSignalHandler<ThrowingTestSignalHandler>()
                    .AddSignalHandler<ThrowingTestSignalHandler2>()
                    .AddSingleton<ExceptionCoordination>(),
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceivers(ct),
        };

        yield return new()
        {
            Name = "multiple handlers, one of which with handler exception",
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
                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);

                await Task.Delay(millisecondsDelay: 10, ct);

                await p.For(TestSignal2.T).WithDefaultPublisherConfiguration().Handle(new() { Payload2 = 20 }, ct);

                await Task.Delay(millisecondsDelay: 10, ct);

                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 30 }, ct);

                // give client time to disconnect due to handler failure
                await Task.Delay(millisecondsDelay: 100, ct);

                await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 40 }, ct);
            },
            RegisterHandler = s =>
                s.AddSignalHandler<ThrowingTestSignalHandler>().AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceivers(ct),
        };

        yield return new()
        {
            Name = "single receiver with publish error",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            PublishException = new InvalidOperationException("publish error"),
            HandlerExceptions = [],
            RegisterHandler = s => s.AddSignalHandler<ThrowingTestSignalHandler>(),
            PublishSignals = async (p, ct) =>
            {
                await p.For(ThrowingTestSignal.T).WithDefaultPublisherConfiguration().Handle(new(), ct);
            },
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<ThrowingTestSignalHandler>(ct),
        };

        yield return new()
        {
            Name = "multiple receivers with publish error",
            ExpectedReceivedSignals = [],
            ConfigurationExceptions = [],
            PublishException = new InvalidOperationException("publish error"),
            NumOfReceivers = 2,
            HandlerExceptions = [],
            PublishSignals = async (p, ct) =>
            {
                await p.For(ThrowingTestSignal.T).WithDefaultPublisherConfiguration().Handle(new(), ct);
            },
            RegisterHandler = s =>
                s.AddSignalHandler<ThrowingTestSignalHandler>().AddSignalHandler<ThrowingTestSignalHandler2>(),
            RunReceivers = (r, ct) => r.RunFileSystemSignalReceivers(ct),
        };
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1250:Use implicit/explicit object creation",
        Justification = "it is clear what objects are being created here"
    )]
    public static IEnumerable<FileSystemSignalConformityContextTestCase> CreateContextTestCases()
    {
        foreach (
            var (hasActivity, hasDownstream, hasBidirectional) in from hasActivity in new[]
                                                                  {
                                                                      true,
                                                                      false,
                                                                  }
                                                                  from hasDownstream in new[]
                                                                  {
                                                                      true,
                                                                      false,
                                                                  }
                                                                  from hasBidirectional in new[]
                                                                  {
                                                                      true,
                                                                      false,
                                                                  }
                                                                  select (hasActivity, hasDownstream, hasBidirectional)
        )
        {
            yield return new()
            {
                Name =
                    $"single receiver, {nameof(hasActivity)}: {hasActivity}, {nameof(hasDownstream)}: {hasDownstream}, {nameof(hasBidirectional)}: {hasBidirectional}",
                HasActivity = hasActivity,
                HasDownstreamData = hasDownstream,
                HasBidirectionalData = hasBidirectional,
                RegisterHandler = s => s.AddSignalHandler<TestSignalHandler>(),
                PublishSignals = async (p, ct) =>
                {
                    await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);
                    await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 20 }, ct);
                },
                RunReceivers = (r, ct) => r.RunFileSystemSignalReceiver<TestSignalHandler>(ct),
            };

            yield return new()
            {
                Name =
                    $"multiple receivers, {nameof(hasActivity)}: {hasActivity}, {nameof(hasDownstream)}: {hasDownstream}, {nameof(hasBidirectional)}: {hasBidirectional}",
                HasActivity = hasActivity,
                HasDownstreamData = hasDownstream,
                HasBidirectionalData = hasBidirectional,
                RegisterHandler = s =>
                    s.AddSignalHandler<TestSignalHandler>().AddSignalHandler<MultiTestSignalHandler>(),
                PublishSignals = async (p, ct) =>
                {
                    await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 10 }, ct);
                    await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 20 }, ct);
                    await p.For(TestSignal.T).WithDefaultPublisherConfiguration().Handle(new() { Payload = 30 }, ct);
                },
                RunReceivers = (r, ct) => r.RunFileSystemSignalReceivers(ct),
                NumOfReceivers = 2,
            };
        }
    }

    private static void AddDelegateHandlers<TSignal, TIHandler>(
        IServiceCollection services,
        SignalTypes<TSignal, TIHandler> signalTypes
    )
        where TSignal : class, IFileSystemSignal<TSignal>
        where TIHandler : class, IFileSystemSignalHandler<TSignal, TIHandler>
    {
        _ = services
            .AddFileSystemSignalHandlerDelegate(
                signalTypes,
                (s, p, ct) => p.GetRequiredService<FnToCallFromHandler>()(s, ct),
                r => r.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>()(r)
            )
            .AddFileSystemSignalHandlerDelegate(
                signalTypes,
                (s, p, ct) => p.GetRequiredService<FnToCallFromHandler>()(s, ct),
                p => p.UseReceiverLogging(),
                r => r.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>()(r)
            )
            .AddFileSystemSignalHandlerDelegate(
                signalTypes,
                (s, p) =>
#pragma warning disable MA0045 // we want to test a sync delegate here
                    p.GetRequiredService<FnToCallFromHandler>()(s, CancellationToken.None).Wait(CancellationToken.None),
#pragma warning restore MA0045
                r => r.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>()(r)
            )
            .AddFileSystemSignalHandlerDelegate(
                signalTypes,
                (s, p) =>
#pragma warning disable MA0045 // we want to test a sync delegate here
                    p.GetRequiredService<FnToCallFromHandler>()(s, CancellationToken.None).Wait(CancellationToken.None),
#pragma warning restore MA0045
                p => p.UseReceiverLogging(),
                r => r.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>()(r)
            );
    }

    [FileSystemSignal]
    public sealed partial record TestSignal
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestSignalHandler(FnToCallFromHandler funToCallFromHandler) : TestSignal.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        static void ISignalHandler.ConfigurePipeline<T>(ISignalPipeline<T> pipeline) =>
            pipeline.Use(ctx =>
            {
                ctx.ServiceProvider.GetRequiredService<ILogger>().LogInformation("received signal");

                return ctx.Next(ctx.Signal, ctx.CancellationToken);
            });

        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }
    }

    public sealed partial class DisabledTestSignalHandler : TestSignal.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver)
        {
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);
            receiver.Disable();
        }

        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            throw new InvalidOperationException("This handler should not be called.");
        }
    }

    [FileSystemSignal]
    public sealed partial record TestSignal2
    {
        public required int Payload2 { get; init; }
    }

    public sealed partial class MultiTestSignalHandler(FnToCallFromHandler funToCallFromHandler)
        : TestSignal.IHandler,
          TestSignal2.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }
    }

    [Signal]
    public sealed partial record NonFileSystemTestSignal
    {
        public required int Payload { get; init; }
    }

    public sealed partial class MixedWithNonFileSystemTestSignalHandler(FnToCallFromHandler funToCallFromHandler)
        : TestSignal.IHandler,
          NonFileSystemTestSignal.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }

        public async Task Handle(NonFileSystemTestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }
    }

    [FileSystemSignal(Tag = "custom")]
    public sealed partial record TestSignalWithCustomEventTypeOrTag
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestSignalWithCustomEventTypeOrTagHandler(FnToCallFromHandler funToCallFromHandler)
        : TestSignalWithCustomEventTypeOrTag.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(
            TestSignalWithCustomEventTypeOrTag signal,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }
    }

    [FileSystemSignal]
    public sealed partial record TestSignalWithoutPayload;

    public sealed partial class TestSignalWithoutPayloadHandler(FnToCallFromHandler funToCallFromHandler)
        : TestSignalWithoutPayload.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(TestSignalWithoutPayload signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }
    }

    [FileSystemSignal]
    public sealed partial record TestSignalWithCustomSerializedPayloadType
    {
        public required TestSignalWithCustomSerializedPayloadTypePayload Payload { get; init; }
    }

    public sealed record TestSignalWithCustomSerializedPayloadTypePayload(int Payload);

    public sealed partial class TestSignalWithCustomSerializedPayloadTypeHandler(
        FnToCallFromHandler funToCallFromHandler
    ) : TestSignalWithCustomSerializedPayloadType.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(
            TestSignalWithCustomSerializedPayloadType signal,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }

        internal sealed class PayloadJsonConverterFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert) =>
                typeToConvert == typeof(TestSignalWithCustomSerializedPayloadTypePayload);

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
                Activator.CreateInstance<PayloadJsonConverter>();
        }

        internal sealed class PayloadJsonConverter : JsonConverter<TestSignalWithCustomSerializedPayloadTypePayload>
        {
            public override TestSignalWithCustomSerializedPayloadTypePayload Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options
            ) => new(reader.GetInt32());

            public override void Write(
                Utf8JsonWriter writer,
                TestSignalWithCustomSerializedPayloadTypePayload value,
                JsonSerializerOptions options
            ) => writer.WriteNumberValue(value.Payload);
        }
    }

    [FileSystemSignal]
    public sealed partial record TestSignalWithCustomSerializer
    {
        public required int Payload { get; init; }

        static IFileSystemSignalSerializer<TestSignalWithCustomSerializer> IFileSystemSignal<TestSignalWithCustomSerializer>.FileSystemSignalSerializer =>
            new TestSignalCustomSerializer();
    }

    private sealed class TestSignalCustomSerializer : IFileSystemSignalSerializer<TestSignalWithCustomSerializer>
    {
        public string FileExtension => ".custom";

        public Task SerializeSignal(
            IServiceProvider serviceProvider,
            TestSignalWithCustomSerializer signal,
            Stream fileStream,
            CancellationToken cancellationToken
        )
        {
            return fileStream
                .WriteAsync(Encoding.UTF8.GetBytes($"payload:{signal.Payload}"), cancellationToken)
                .AsTask();
        }

        public async Task<TestSignalWithCustomSerializer> DeserializeSignal(
            IServiceProvider serviceProvider,
            Stream fileStream,
            CancellationToken cancellationToken
        )
        {
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);
            var serializedSignal = await streamReader.ReadToEndAsync(cancellationToken);

            var result = int.Parse(serializedSignal.Split(':')[1], CultureInfo.InvariantCulture);

            return new TestSignalWithCustomSerializer { Payload = result };
        }
    }

    public sealed partial class TestSignalWithCustomSerializerHandler(FnToCallFromHandler funToCallFromHandler)
        : TestSignalWithCustomSerializer.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(TestSignalWithCustomSerializer signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }
    }

    [FileSystemSignal]
    public sealed partial record TestSignalWithCustomJsonTypeInfo
    {
        public int SignalPayload { get; init; }
    }

    public sealed partial class TestSignalWithCustomJsonTypeInfoHandler(FnToCallFromHandler funToCallFromHandler)
        : TestSignalWithCustomJsonTypeInfo.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(TestSignalWithCustomJsonTypeInfo signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
    [JsonSerializable(typeof(TestSignalWithCustomJsonTypeInfo))]
    internal sealed partial class TestSignalWithCustomJsonTypeInfoJsonSerializerContext : JsonSerializerContext;

    [FileSystemSignal]
    public sealed partial record TestSignalWithMiddleware
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestSignalWithMiddlewareHandler(FnToCallFromHandler funToCallFromHandler)
        : TestSignalWithMiddleware.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(TestSignalWithMiddleware signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T> =>
            pipeline.Use(pipeline.ServiceProvider.GetRequiredService<TestSignalMiddleware<T>>());
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

    [FileSystemSignal]
    public sealed partial record TestSignalForAssemblyScanning
    {
        public int Payload { get; init; }
    }

    public sealed partial class TestSignalForAssemblyScanningHandler(FnToCallFromHandler funToCallFromHandler)
        : TestSignalForAssemblyScanning.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(TestSignalForAssemblyScanning signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }
    }

    [FileSystemSignal]
    public sealed partial record TestSignalWithDelegateHandler
    {
        public required int Payload { get; init; }
    }

    public sealed partial class WildMixTestSignalHandler(FnToCallFromHandler funToCallFromHandler)
        : TestSignal.IHandler,
          TestSignalWithoutPayload.IHandler,
          TestSignalWithCustomSerializer.IHandler,
          TestSignalWithCustomJsonTypeInfo.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }

        public async Task Handle(TestSignalWithoutPayload signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }

        public async Task Handle(TestSignalWithCustomSerializer signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }

        public async Task Handle(TestSignalWithCustomJsonTypeInfo signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }
    }

    [FileSystemSignal]
    public partial record TestSignalBase(int Payload);

    [FileSystemSignal]
    public partial record TestSignalSub(int Payload, int PayloadSub) : TestSignalBase(Payload);

    public sealed record TestSignalSubSub(int Payload, int PayloadSub, int PayloadSubSub)
        : TestSignalSub(Payload, PayloadSub);

    private sealed partial class MultiHierarchyTestSignalHandler(FnToCallFromHandler funToCallFromHandler)
        : TestSignalBase.IHandler,
          TestSignalSub.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(TestSignalBase signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }

        public async Task Handle(TestSignalSub signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(signal, cancellationToken);
        }
    }

    private sealed partial class ThrowingTestSignalHandler(
        ConcurrentQueue<Exception?> exceptions,
        FnToCallFromHandler funToCallFromHandler,
        ExceptionCoordination? exceptionCoordination = null
    ) : TestSignal.IHandler, ThrowingTestSignal.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Delay(millisecondsDelay: 1, cancellationToken);

            await funToCallFromHandler(signal, cancellationToken);

            if (exceptions.TryDequeue(out var ex) && ex is not null)
            {
                if (exceptionCoordination is not null)
                {
                    exceptionCoordination.Tcs1.SetResult();
                    await exceptionCoordination.Tcs2.Task.WaitAsync(cancellationToken);
                }

                throw ex;
            }
        }

        public Task Handle(ThrowingTestSignal signal, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed partial class ThrowingTestSignalHandler2(
        ConcurrentQueue<Exception?> exceptions,
        FnToCallFromHandler funToCallFromHandler,
        ExceptionCoordination? exceptionCoordination = null
    ) : TestSignal.IHandler, TestSignal2.IHandler, ThrowingTestSignal.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IFileSystemSignalReceiver>>().Invoke(receiver);

        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Delay(millisecondsDelay: 1, cancellationToken);

            await funToCallFromHandler(signal, cancellationToken);

            if (exceptions.TryDequeue(out var ex) && ex is not null)
            {
                if (exceptionCoordination is not null)
                {
                    exceptionCoordination.Tcs2.SetResult();
                    await exceptionCoordination.Tcs1.Task.WaitAsync(cancellationToken);
                }

                throw ex;
            }
        }

        public async Task Handle(TestSignal2 signal, CancellationToken cancellationToken = default)
        {
            await Task.Delay(millisecondsDelay: 1, cancellationToken);

            await funToCallFromHandler(signal, cancellationToken);

            if (exceptions.TryDequeue(out var ex) && ex is not null)
            {
                if (exceptionCoordination is not null)
                {
                    exceptionCoordination.Tcs2.SetResult();
                    await exceptionCoordination.Tcs1.Task.WaitAsync(cancellationToken);
                }

                throw ex;
            }
        }

        public Task Handle(ThrowingTestSignal signal, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ExceptionCoordination
    {
        public TaskCompletionSource Tcs1 { get; } = new();

        public TaskCompletionSource Tcs2 { get; } = new();
    }

    public sealed class TestObservations
    {
        public ConcurrentQueue<string?> ReceivedSignalIds { get; } = [];

        public ConcurrentQueue<string?> ReceivedTraceIds { get; } = [];

        public SignalTransportType? SeenTransportTypeInMiddleware { get; set; }
    }

    [FileSystemSignal]
    private sealed partial record ThrowingTestSignal
    {
        static IFileSystemSignalSerializer<ThrowingTestSignal> IFileSystemSignal<ThrowingTestSignal>.FileSystemSignalSerializer { get; } =
            new ThrowingTestSignalSerializer();
    }

    private sealed class ThrowingTestSignalSerializer : IFileSystemSignalSerializer<ThrowingTestSignal>
    {
        public string FileExtension => ".throwing";

        public Task SerializeSignal(
            IServiceProvider serviceProvider,
            ThrowingTestSignal signal,
            Stream fileStream,
            CancellationToken cancellationToken
        ) => throw serviceProvider.GetRequiredService<Exception>();

        public Task<ThrowingTestSignal> DeserializeSignal(
            IServiceProvider serviceProvider,
            Stream fileStream,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }
}

file static class PipelineExtensions
{
    public static ISignalPipeline<TSignal> UsePublishCallback<TSignal>(this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal>
    {
        var publishCallback = pipeline.ServiceProvider.GetService<
            Func<object, ConquerorContext, CancellationToken, Task>
        >();

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
        this ISignalHandler<TSignal, TIHandler> handler
    )
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler> =>
        handler.WithPipeline(p => _ = p.UseLogging().UsePublishCallback());

    public static TIHandler WithDefaultPublisherConfiguration<TSignal, TIHandler>(
        this ISignalHandler<TSignal, TIHandler> handler
    )
        where TSignal : class, IFileSystemSignal<TSignal>
        where TIHandler : class, IFileSystemSignalHandler<TSignal, TIHandler>
    {
        return handler
            .WithDefaultPublisherPipeline()
            .WithTransport(b => b.UseFileSystem(b.ServiceProvider.GetRequiredService<DirectoryInfo>().FullName));
    }
}
