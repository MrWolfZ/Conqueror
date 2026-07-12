namespace Conqueror.Transport.Http.Tests.Messaging;

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Conqueror.Messaging;

[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage(
    "ReSharper",
    "UnusedAutoPropertyAccessor.Global",
    Justification = "Members are used by ASP.NET Core via reflection"
)]
public static partial class HttpMessageTestCases
{
    private const string AuthorizationHeaderScheme = "Basic";
    private const string AuthorizationHeaderValue = "username:password";
    private const string AuthorizationHeader = $"{AuthorizationHeaderScheme} {AuthorizationHeaderValue}";
    private const string TestHeaderName = "test-value";
    private const string TestHeaderValue = "test-value";

    [SuppressMessage(
        "Roslynator",
        "RCS1250:Use implicit/explicit object creation",
        Justification = "it is clear what objects are being created here"
    )]
    [SuppressMessage(
        "Design",
        "MA0045:Do not use blocking calls in a sync method (need to make calling method async)",
        Justification = "we want to explicitly test sync delegates"
    )]
    public static IEnumerable<HttpMessageConformityExecutionSuccessTestCase> CreateSuccessTestCases()
    {
        yield return new()
        {
            Name = "with response",
            FullPath = "/api/test",
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessage { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessage.T),
        };

        yield return new()
        {
            Name = "disabled handler",
            FullPath = "/api/test",
            HandlerIsEnabled = false,
            ExpectedReceivedMessages = [],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (_, _) => await Task.FromResult(Array.Empty<object>()),
            MapEndpoints = e => e.MapMessageEndpoint(TestMessage.T),
            ConfigureReceiverFn = (_, r) => r.Disable(),
        };

        yield return new()
        {
            Name = "without payload with response",
            FullPath = "/api/testMessageWithoutPayload",
            ParameterCount = 0,
            ExpectedReceivedMessages = [new TestMessageWithoutPayload(), new TestMessageWithoutPayload()],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 11 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithoutPayloadHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithoutPayload.T).WithDefaultSenderConfiguration().Handle(new(), ct);
                var r2 = await s.For(TestMessageWithoutPayload.T).WithDefaultSenderConfiguration().Handle(new(), ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithoutPayload.T),
            MessageContentType = null,
            MessagePayloads = [null, null],
            ResponsePayloads = ["{\"payload\":11}", "{\"payload\":11}"],
        };

        yield return new()
        {
            Name = "without response",
            FullPath = "/api/testMessageWithoutResponse",
            SuccessStatusCode = 204,
            ExpectedReceivedMessages =
            [
                new TestMessageWithoutResponse { Payload = 10 },
                new TestMessageWithoutResponse { Payload = 20 },
            ],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithoutResponseHandler>(),
            SendMessages = async (s, ct) =>
            {
                await s.For(TestMessageWithoutResponse.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                await s.For(TestMessageWithoutResponse.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithoutResponse.T),
        };

        yield return new()
        {
            Name = "disabled handler without response",
            FullPath = "/api/testMessageWithoutResponse",
            SuccessStatusCode = 204,
            HandlerIsEnabled = false,
            ExpectedReceivedMessages = [],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithoutResponseHandler>(),
            SendMessages = async (_, _) => await Task.FromResult(Array.Empty<object>()),
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithoutResponse.T),
            ConfigureReceiverFn = (_, r) => r.Disable(),
        };

        yield return new()
        {
            Name = "without payload without response",
            FullPath = "/api/testMessageWithoutResponseWithoutPayload",
            ParameterCount = 0,
            SuccessStatusCode = 204,
            ExpectedReceivedMessages =
            [
                new TestMessageWithoutResponseWithoutPayload(),
                new TestMessageWithoutResponseWithoutPayload(),
            ],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithoutResponseWithoutPayloadHandler>(),
            SendMessages = async (s, ct) =>
            {
                await s.For(TestMessageWithoutResponseWithoutPayload.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new(), ct);
                await s.For(TestMessageWithoutResponseWithoutPayload.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new(), ct);

                return [];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithoutResponseWithoutPayload.T),
            MessageContentType = null,
            MessagePayloads = [null, null],
        };

        yield return new()
        {
            Name = "with HTTP headers",
            FullPath = "/api/test",
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessage { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T)
                    .WithPipeline(p => _ = p.UseLogging().UseSendCallback())
                    .WithTransport(b =>
                        b.UseHttp(new("http://conqueror.test"))
                            .WithHttpClient(b.ServiceProvider.GetRequiredService<HttpClient>())
                            .WithHeaders(h =>
                            {
                                h.Authorization = new(AuthorizationHeaderScheme, AuthorizationHeaderValue);
                                h.Add(TestHeaderName, TestHeaderValue);
                            })
                    )
                    .Handle(new() { Payload = 10 }, ct);

                var r2 = await s.For(TestMessage.T)
                    .WithPipeline(p => _ = p.UseLogging().UseSendCallback())
                    .WithTransport(b =>
                        b.UseHttp(new("http://conqueror.test"))
                            .WithHttpClient(b.ServiceProvider.GetRequiredService<HttpClient>())
                            .WithHeaders(h =>
                            {
                                h.Authorization = new(AuthorizationHeaderScheme, AuthorizationHeaderValue);
                                h.Add(TestHeaderName, TestHeaderValue);
                            })
                    )
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessage.T),
            AfterMessagesAreReceived = h =>
            {
                Assert.That(h.ReceiverHost.ReceivedHeadersOnServer, Has.Count.EqualTo(expected: 2));

                foreach (var headers in h.ReceiverHost.ReceivedHeadersOnServer)
                {
                    Assert.That(headers.Authorization.ToString(), Is.EqualTo(AuthorizationHeader));
                    Assert.That(headers, Does.ContainKey(TestHeaderName).WithValue(TestHeaderValue));
                }

                return Task.CompletedTask;
            },
        };

        yield return new()
        {
            Name = "with HTTP method",
            FullPath = "/api/testMessageWithMethod",
            HttpMethod = MethodNames.Delete,
            ParameterCount = 1,
            ExpectedReceivedMessages =
            [
                new TestMessageWithMethod { Payload = 10 },
                new TestMessageWithMethod { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithMethodHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithMethod.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithMethod.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithMethod.T),
        };

        yield return new()
        {
            Name = "with path prefix",
            FullPath = "/custom/prefix/testMessageWithPathPrefix",
            ExpectedReceivedMessages =
            [
                new TestMessageWithPathPrefix { Payload = 10 },
                new TestMessageWithPathPrefix { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithPathPrefixHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithPathPrefix.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithPathPrefix.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithPathPrefix.T),
        };

        yield return new()
        {
            Name = "with version",
            FullPath = "/api/v2/testMessageWithVersion",
            ExpectedReceivedMessages =
            [
                new TestMessageWithVersion { Payload = 10 },
                new TestMessageWithVersion { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithVersionHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithVersion.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithVersion.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithVersion.T),
        };

        yield return new()
        {
            Name = "with path",
            FullPath = "/api/custom/path",
            ExpectedReceivedMessages =
            [
                new TestMessageWithPath { Payload = 10 },
                new TestMessageWithPath { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithPathHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithPath.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithPath.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithPath.T),
        };

        yield return new()
        {
            Name = "with path prefix and version",
            FullPath = "/custom/prefix/v3/custom/path",
            ExpectedReceivedMessages =
            [
                new TestMessageWithPathPrefixAndPathAndVersion { Payload = 10 },
                new TestMessageWithPathPrefixAndPathAndVersion { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithPathPrefixAndPathAndVersionHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithPathPrefixAndPathAndVersion.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithPathPrefixAndPathAndVersion.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithPathPrefixAndPathAndVersion.T),
        };

        yield return new()
        {
            Name = "with full path",
            FullPath = "/custom/full/path/for/message",
            ExpectedReceivedMessages =
            [
                new TestMessageWithFullPath { Payload = 10 },
                new TestMessageWithFullPath { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithFullPathHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithFullPath.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithFullPath.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithFullPath.T),
        };

        yield return new()
        {
            Name = "with full path and version",
            FullPath = "/custom/full/path/for/message/ignoring/version",
            ExpectedReceivedMessages =
            [
                new TestMessageWithFullPathAndVersion { Payload = 10 },
                new TestMessageWithFullPathAndVersion { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithFullPathAndVersionHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithFullPathAndVersion.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithFullPathAndVersion.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithFullPathAndVersion.T),
        };

        yield return new()
        {
            Name = "with success status code",
            FullPath = "/api/testMessageWithSuccessStatusCode",
            SuccessStatusCode = 201,
            ExpectedReceivedMessages =
            [
                new TestMessageWithSuccessStatusCode { Payload = 10 },
                new TestMessageWithSuccessStatusCode { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithSuccessStatusCodeHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithSuccessStatusCode.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithSuccessStatusCode.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithSuccessStatusCode.T),
        };

        yield return new()
        {
            Name = "with name",
            FullPath = "/api/testMessageWithName",
            EndpointName = "custom-message-name",
            ExpectedReceivedMessages =
            [
                new TestMessageWithName { Payload = 10 },
                new TestMessageWithName { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithNameHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithName.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithName.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithName.T),
        };

        yield return new()
        {
            Name = "with API group name",
            FullPath = "/api/testMessageWithApiGroupName",
            ApiGroupName = "Custom Message Group",
            ExpectedReceivedMessages =
            [
                new TestMessageWithApiGroupName { Payload = 10 },
                new TestMessageWithApiGroupName { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithApiGroupNameHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithApiGroupName.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithApiGroupName.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithApiGroupName.T),
        };

        yield return new()
        {
            Name = "GET",
            HttpMethod = MethodNames.Get,
            FullPath = "/api/testMessageWithGet",
            ParameterCount = 2,
            MessageContentType = null,
            ExpectedReceivedMessages =
            [
                new TestMessageWithGet
                {
                    Payload = 10,
                    Param = "test",
                },
                new TestMessageWithGet
                {
                    Payload = 20,
                    Param = "test2",
                },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithGetHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithGet.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new()
                    {
                        Payload = 10,
                        Param = "test",
                    }, ct);
                var r2 = await s.For(TestMessageWithGet.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new()
                    {
                        Payload = 20,
                        Param = "test2",
                    }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithGet.T),
            QueryStrings = ["?payload=10&param=test", "?payload=20&param=test2"],
            MessagePayloads = [null, null],
        };

        yield return new()
        {
            Name = "GET without payload",
            HttpMethod = MethodNames.Get,
            FullPath = "/api/testMessageWithGetWithoutPayload",
            ParameterCount = 0,
            MessageContentType = null,
            ExpectedReceivedMessages = [new TestMessageWithGetWithoutPayload(), new TestMessageWithGetWithoutPayload()],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 11 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithGetWithoutPayloadHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithGetWithoutPayload.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new(), ct);
                var r2 = await s.For(TestMessageWithGetWithoutPayload.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new(), ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithGetWithoutPayload.T),
            QueryStrings = [null, null],
            MessagePayloads = [null, null],
        };

        yield return new()
        {
            Name = "GET with optional payload",
            HttpMethod = MethodNames.Get,
            FullPath = "/api/testMessageWithGetWithOptionalPayload",
            ParameterCount = 2,
            MessageContentType = null,
            ExpectedReceivedMessages =
            [
                new TestMessageWithGetWithOptionalPayload(),
                new TestMessageWithGetWithOptionalPayload { Payload = 10 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 1 }, new TestMessageResponse { Payload = 11 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithGetWithOptionalPayloadHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithGetWithOptionalPayload.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new(), ct);
                var r2 = await s.For(TestMessageWithGetWithOptionalPayload.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithGetWithOptionalPayload.T),
            QueryStrings = [null, "?payload=10"],
            MessagePayloads = [null, null],
            ResponsePayloads = ["{\"payload\":1}", "{\"payload\":11}"],
        };

        yield return new()
        {
            Name = "GET with primary constructor",
            HttpMethod = MethodNames.Get,
            FullPath = "/api/testMessageWithGetWithPrimaryConstructor",
            ParameterCount = 3,
            MessageContentType = null,
            ExpectedReceivedMessages =
            [
                new TestMessageWithGetWithPrimaryConstructor(Payload: 10, "test", [11, 12]),
                new TestMessageWithGetWithPrimaryConstructor(Payload: 20, "test2", [21, 22]),
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 33 }, new TestMessageResponse { Payload = 63 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithGetWithPrimaryConstructorHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithGetWithPrimaryConstructor.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new(Payload: 10, "test", [11, 12]), ct);
                var r2 = await s.For(TestMessageWithGetWithPrimaryConstructor.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new(Payload: 20, "test2", [21, 22]), ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithGetWithPrimaryConstructor.T),
            QueryStrings =
            [
                "?payload=10&param=test&intArray=11&intArray=12",
                "?payload=20&param=test2&intArray=21&intArray=22",
            ],
            MessagePayloads = [null, null],
            ResponsePayloads = ["{\"payload\":33}", "{\"payload\":63}"],
        };

        yield return new()
        {
            Name = "GET with primary constructor with optional parameters",
            HttpMethod = MethodNames.Get,
            FullPath = "/api/testMessageWithGetWithPrimaryConstructorWithOptionalParameters",
            ParameterCount = 2,
            MessageContentType = null,
            ExpectedReceivedMessages =
            [
                new TestMessageWithGetWithPrimaryConstructorWithOptionalParameters(),
                new TestMessageWithGetWithPrimaryConstructorWithOptionalParameters(Payload: 10, "test"),
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 1 }, new TestMessageResponse { Payload = 11 }],
            RegisterHandler = s =>
                s.AddMessageHandler<TestMessageWithGetWithPrimaryConstructorWithOptionalParametersHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithGetWithPrimaryConstructorWithOptionalParameters.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new(), ct);

                var r2 = await s.For(TestMessageWithGetWithPrimaryConstructorWithOptionalParameters.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new(Payload: 10, "test"), ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithGetWithPrimaryConstructorWithOptionalParameters.T),
            QueryStrings = [null, "?payload=10&param=test"],
            MessagePayloads = [null, null],
            ResponsePayloads = ["{\"payload\":1}", "{\"payload\":11}"],
        };

        yield return new()
        {
            Name = "GET with complex payload",
            HttpMethod = MethodNames.Get,
            FullPath = "/api/testMessageWithComplexGetPayload",
            ParameterCount = 3,
            MessageContentType = null,
            ExpectedReceivedMessages =
            [
                new TestMessageWithComplexGetPayload
                {
                    Payload = 10,
                    NestedList = [11, 12],
                    NestedArray = [13, 14],
                },
                new TestMessageWithComplexGetPayload
                {
                    Payload = 20,
                    NestedList = [21, 22],
                    NestedArray = [23, 24],
                },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 60 }, new TestMessageResponse { Payload = 110 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithComplexGetPayloadHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithComplexGetPayload.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(
                        new()
                        {
                            Payload = 10,
                            NestedList = [11, 12],
                            NestedArray = [13, 14],
                        },
                        ct
                    );
                var r2 = await s.For(TestMessageWithComplexGetPayload.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(
                        new()
                        {
                            Payload = 20,
                            NestedList = [21, 22],
                            NestedArray = [23, 24],
                        },
                        ct
                    );

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithComplexGetPayload.T),
            QueryStrings =
            [
                "?payload=10&nestedList=11&nestedList=12&nestedArray=13&nestedArray=14",
                "?payload=20&nestedList=21&nestedList=22&nestedArray=23&nestedArray=24",
            ],
            MessagePayloads = [null, null],
            ResponsePayloads = ["{\"payload\":60}", "{\"payload\":110}"],
        };

        yield return new()
        {
            Name = "with custom serialized payload type",
            FullPath = "/api/testMessageWithCustomSerializedPayloadType",
            ExpectedReceivedMessages =
            [
                new TestMessageWithCustomSerializedPayloadType { Payload = new(Payload: 10) },
                new TestMessageWithCustomSerializedPayloadType { Payload = new(Payload: 20) },
            ],
            ExpectedResponses =
            [
                new TestMessageWithCustomSerializedPayloadTypeResponse { Payload = new(Payload: 11) },
                new TestMessageWithCustomSerializedPayloadTypeResponse { Payload = new(Payload: 21) },
            ],
            RegisterHandler = s =>
            {
                _ = s.AddMessageHandler<TestMessageWithCustomSerializedPayloadTypeHandler>();

                _ = s.AddTransient<JsonSerializerOptions>(p =>
                        p.GetRequiredService<
                            IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>
                        >().Value.SerializerOptions
                    )
                    .PostConfigure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
                    {
                        options.SerializerOptions.Converters.Add(
                            new TestMessageWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory()
                        );
                    })
                    .PostConfigure<JsonOptions>(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(
                            new TestMessageWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory()
                        );
                    });
            },
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithCustomSerializedPayloadType.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = new(Payload: 10) }, ct);
                var r2 = await s.For(TestMessageWithCustomSerializedPayloadType.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = new(Payload: 20) }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithCustomSerializedPayloadType.T),
            RegisterOnClient = services =>
            {
                var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                jsonSerializerOptions.Converters.Add(
                    new TestMessageWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory()
                );
                jsonSerializerOptions.MakeReadOnly(populateMissingResolver: true);

                _ = services.AddSingleton(jsonSerializerOptions);
            },
            MessagePayloads = ["{\"payload\":10}", "{\"payload\":20}"],
            ResponsePayloads = ["{\"payload\":11}", "{\"payload\":21}"],
        };

        yield return new()
        {
            Name = "with custom serializer",
            FullPath = "/api/custom/path/for/serializer/12",
            Template = "/api/custom/path/for/serializer/{pathPayload:int}",
            MessageContentType = "application/custom-message",
            ResponseContentType = "application/custom-response",
            ExpectedReceivedMessages =
            [
                new TestMessageWithCustomSerializer
                {
                    QueryPayload = 10,
                    BodyPayload = 11,
                    PathPayload = 12,
                },
                new TestMessageWithCustomSerializer
                {
                    QueryPayload = 20,
                    BodyPayload = 21,
                    PathPayload = 12,
                },
            ],
            ExpectedResponses =
            [
                new TestMessageWithCustomSerializerResponse { Payload = 33 },
                new TestMessageWithCustomSerializerResponse { Payload = 53 },
            ],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithCustomSerializerHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithCustomSerializer.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(
                        new()
                        {
                            QueryPayload = 10,
                            BodyPayload = 11,
                            PathPayload = 12,
                        },
                        ct
                    );
                var r2 = await s.For(TestMessageWithCustomSerializer.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(
                        new()
                        {
                            QueryPayload = 20,
                            BodyPayload = 21,
                            PathPayload = 12,
                        },
                        ct
                    );

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithCustomSerializer.T),
            QueryStrings = ["?query-payload=10", "?query-payload=20"],
            MessagePayloads = ["bodyPayload:11", "bodyPayload:21"],
            ResponsePayloads = ["total-payload:33", "total-payload:53"],
        };

        yield return new()
        {
            Name = "with custom JSON type info",
            FullPath = "/api/testMessageWithCustomJsonTypeInfo",
            ExpectedReceivedMessages =
            [
                new TestMessageWithCustomJsonTypeInfo { MessagePayload = 10 },
                new TestMessageWithCustomJsonTypeInfo { MessagePayload = 20 },
            ],
            ExpectedResponses =
            [
                new TestMessageWithCustomJsonTypeInfoResponse { ResponsePayload = 11 },
                new TestMessageWithCustomJsonTypeInfoResponse { ResponsePayload = 21 },
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
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithCustomJsonTypeInfo.T),
            MessagePayloads = ["{\"MESSAGE_PAYLOAD\":10}", "{\"MESSAGE_PAYLOAD\":20}"],
            ResponsePayloads = ["{\"RESPONSE_PAYLOAD\":11}", "{\"RESPONSE_PAYLOAD\":21}"],
        };

        yield return new()
        {
            Name = "with middleware",
            FullPath = "/api/testMessageWithMiddleware",
            ExpectedReceivedMessages =
            [
                new TestMessageWithMiddleware { Payload = 10 },
                new TestMessageWithMiddleware { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s =>
                s.AddMessageHandler<TestMessageWithMiddlewareHandler>()
                    .AddSingleton<TestObservations>()
                    .AddTransient(typeof(TestMessageMiddleware<,>)),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithMiddleware.T)
                    .WithDefaultSenderConfiguration()
                    .WithPipeline(p =>
                        p.Use(
                            p.ServiceProvider.GetRequiredService<
                                TestMessageMiddleware<TestMessageWithMiddleware, TestMessageResponse>
                            >()
                        )
                    )
                    .Handle(new() { Payload = 10 }, ct);

                var r2 = await s.For(TestMessageWithMiddleware.T)
                    .WithDefaultSenderConfiguration()
                    .WithPipeline(p =>
                        p.Use(
                            p.ServiceProvider.GetRequiredService<
                                TestMessageMiddleware<TestMessageWithMiddleware, TestMessageResponse>
                            >()
                        )
                    )
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithMiddleware.T),
            RegisterOnClient = s => s.AddSingleton<TestObservations>().AddTransient(typeof(TestMessageMiddleware<,>)),
            AfterMessagesAreReceived = h =>
            {
                var seenTransportTypeOnServer = h
                    .ReceiverHost.Resolve<TestObservations>()
                    .SeenTransportTypeInMiddleware;
                Assert.That(
                    seenTransportTypeOnServer?.IsHttp(),
                    Is.True,
                    $"transport type is {seenTransportTypeOnServer?.Name}"
                );
                Assert.That(seenTransportTypeOnServer?.Role, Is.EqualTo(MessageTransportRole.Receiver));

                var seenTransportTypeOnClient = h.SenderHost.Resolve<TestObservations>().SeenTransportTypeInMiddleware;
                Assert.That(
                    seenTransportTypeOnClient?.IsHttp(),
                    Is.True,
                    $"transport type is {seenTransportTypeOnClient?.Name}"
                );
                Assert.That(seenTransportTypeOnClient?.Role, Is.EqualTo(MessageTransportRole.Sender));

                return Task.CompletedTask;
            },
        };

        yield return new()
        {
            Name = "without response with middleware",
            FullPath = "/api/testMessageWithMiddlewareWithoutResponse",
            SuccessStatusCode = 204,
            ExpectedReceivedMessages =
            [
                new TestMessageWithMiddlewareWithoutResponse { Payload = 10 },
                new TestMessageWithMiddlewareWithoutResponse { Payload = 20 },
            ],
            ExpectedResponses = [],
            RegisterHandler = s =>
                s.AddMessageHandler<TestMessageWithMiddlewareWithoutResponseHandler>()
                    .AddSingleton<TestObservations>()
                    .AddTransient(typeof(TestMessageMiddleware<,>)),
            SendMessages = async (s, ct) =>
            {
                await s.For(TestMessageWithMiddlewareWithoutResponse.T)
                    .WithDefaultSenderConfiguration()
                    .WithPipeline(p =>
                        p.Use(
                            p.ServiceProvider.GetRequiredService<
                                TestMessageMiddleware<TestMessageWithMiddlewareWithoutResponse, UnitMessageResponse>
                            >()
                        )
                    )
                    .Handle(new() { Payload = 10 }, ct);

                await s.For(TestMessageWithMiddlewareWithoutResponse.T)
                    .WithDefaultSenderConfiguration()
                    .WithPipeline(p =>
                        p.Use(
                            p.ServiceProvider.GetRequiredService<
                                TestMessageMiddleware<TestMessageWithMiddlewareWithoutResponse, UnitMessageResponse>
                            >()
                        )
                    )
                    .Handle(new() { Payload = 20 }, ct);

                return [];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithMiddlewareWithoutResponse.T),
            RegisterOnClient = s => s.AddSingleton<TestObservations>().AddTransient(typeof(TestMessageMiddleware<,>)),
            AfterMessagesAreReceived = h =>
            {
                var seenTransportTypeOnServer = h
                    .ReceiverHost.Resolve<TestObservations>()
                    .SeenTransportTypeInMiddleware;
                Assert.That(
                    seenTransportTypeOnServer?.IsHttp(),
                    Is.True,
                    $"transport type is {seenTransportTypeOnServer?.Name}"
                );
                Assert.That(seenTransportTypeOnServer?.Role, Is.EqualTo(MessageTransportRole.Receiver));

                var seenTransportTypeOnClient = h.SenderHost.Resolve<TestObservations>().SeenTransportTypeInMiddleware;
                Assert.That(
                    seenTransportTypeOnClient?.IsHttp(),
                    Is.True,
                    $"transport type is {seenTransportTypeOnClient?.Name}"
                );
                Assert.That(seenTransportTypeOnClient?.Role, Is.EqualTo(MessageTransportRole.Sender));

                return Task.CompletedTask;
            },
        };

        yield return new()
        {
            Name = "with array response",
            FullPath = "/api/testMessageWithArrayResponse",
            ExpectedReceivedMessages =
            [
                new TestMessageWithArrayResponse { Payload = 10 },
                new TestMessageWithArrayResponse { Payload = 20 },
            ],
            ExpectedResponses =
            [
                new TestMessageResponse[] { new() { Payload = 11 }, new() { Payload = 12 }, },
                new TestMessageResponse[] { new() { Payload = 21 }, new() { Payload = 22 }, },
            ],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithArrayResponseHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithArrayResponse.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithArrayResponse.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithArrayResponse.T),
            ResponsePayloads = ["[{\"payload\":11},{\"payload\":12}]", "[{\"payload\":21},{\"payload\":22}]"],
        };

        yield return new()
        {
            Name = "with list response",
            FullPath = "/api/testMessageWithListResponse",
            ExpectedReceivedMessages =
            [
                new TestMessageWithListResponse { Payload = 10 },
                new TestMessageWithListResponse { Payload = 20 },
            ],
            ExpectedResponses =
            [
                new List<TestMessageResponse>
                {
                    new() { Payload = 11 },
                    new() { Payload = 12 },
                },
                new List<TestMessageResponse>
                {
                    new() { Payload = 21 },
                    new() { Payload = 22 },
                },
            ],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithListResponseHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithListResponse.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithListResponse.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithListResponse.T),
            ResponsePayloads = ["[{\"payload\":11},{\"payload\":12}]", "[{\"payload\":21},{\"payload\":22}]"],
        };

        yield return new()
        {
            Name = "with enumerable response",
            FullPath = "/api/testMessageWithEnumerableResponse",
            SingleResponseType = typeof(IEnumerable<TestMessageResponse>),
            ExpectedReceivedMessages =
            [
                new TestMessageWithEnumerableResponse { Payload = 10 },
                new TestMessageWithEnumerableResponse { Payload = 20 },
            ],
            ExpectedResponses =
            [
                new List<TestMessageResponse>
                {
                    new() { Payload = 11 },
                    new() { Payload = 12 },
                },
                new List<TestMessageResponse>
                {
                    new() { Payload = 21 },
                    new() { Payload = 22 },
                },
            ],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithEnumerableResponseHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithEnumerableResponse.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithEnumerableResponse.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithEnumerableResponse.T),
            ResponsePayloads = ["[{\"payload\":11},{\"payload\":12}]", "[{\"payload\":21},{\"payload\":22}]"],
        };

        yield return new()
        {
            Name = "from assembly scanning",
            FullPath = "/api/testMessageForAssemblyScanning",
            ExpectedReceivedMessages =
            [
                new TestMessageForAssemblyScanning { Payload = 10 },
                new TestMessageForAssemblyScanning { Payload = 20 },
            ],
            ExpectedResponses =
            [
                new TestMessageForAssemblyScanningResponse { Payload = 11 },
                new TestMessageForAssemblyScanningResponse { Payload = 21 },
            ],
            RegisterHandler = s =>
                s.AddMessageHandlersFromAssembly(typeof(TestMessageForAssemblyScanningHandler).Assembly),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageForAssemblyScanning.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageForAssemblyScanning.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageForAssemblyScanning.T),
        };

        yield return new()
        {
            Name = "without response from assembly scanning",
            FullPath = "/api/testMessageWithoutResponseForAssemblyScanning",
            SuccessStatusCode = 204,
            ExpectedReceivedMessages =
            [
                new TestMessageWithoutResponseForAssemblyScanning { Payload = 10 },
                new TestMessageWithoutResponseForAssemblyScanning { Payload = 20 },
            ],
            ExpectedResponses = [],
            RegisterHandler = s =>
                s.AddMessageHandlersFromAssembly(typeof(TestMessageWithoutResponseForAssemblyScanningHandler).Assembly),
            SendMessages = async (s, ct) =>
            {
                await s.For(TestMessageWithoutResponseForAssemblyScanning.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                await s.For(TestMessageWithoutResponseForAssemblyScanning.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithoutResponseForAssemblyScanning.T),
        };

        yield return new()
        {
            Name = "omitted from API description",
            FullPath = "/api/test",
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessage { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessage.T),
            ConfigureReceiverFn = (_, r) => r.OmitFromApiDescription(),
            IsOmittedFromApiDescriptions = true,
        };

        yield return new()
        {
            Name = "with custom conventions",
            FullPath = "/customApi/testMessageWithCustomConventions",
            SuccessStatusCode = 201,
            ExpectedReceivedMessages =
            [
                new TestMessageWithCustomConventions { Payload = 10 },
                new TestMessageWithCustomConventions { Payload = 20 },
            ],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageWithCustomConventionsHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageWithCustomConventions.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithCustomConventions.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessageWithCustomConventions.T),
        };

        foreach (
            var t in from hasResponse in new[] { true, false }
                     from isSync in new[] { true, false }
                     from configuresPipeline in new[] { true, false }
                     from configuresReceiver in new[] { true, false }
                     select (hasResponse, isSync, configuresPipeline, configuresReceiver)
        )
        {
            var middlewareCallCount = 0;
            var receiverConfigurationCount = 0;

            yield return new()
            {
                Name =
                    $"with delegate (hasResponse: {t.hasResponse}, isSync: {t.isSync}, configuresPipeline: {t.configuresPipeline}, configuresReceiver: {t.configuresReceiver})",
                FullPath = t.hasResponse
                    ? "/api/testMessageWithDelegateHandler"
                    : "/api/testMessageWithDelegateHandlerWithoutResponse",
                ExpectedReceivedMessages = t.hasResponse
                    ?
                    [
                        new TestMessageWithDelegateHandler { Payload = 10 },
                        new TestMessageWithDelegateHandler { Payload = 20 },
                    ]
                    :
                    [
                        new TestMessageWithDelegateHandlerWithoutResponse { Payload = 10 },
                        new TestMessageWithDelegateHandlerWithoutResponse { Payload = 20 },
                    ],
                ExpectedResponses = t.hasResponse
                    ? [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }]
                    : [],
                SuccessStatusCode = t.hasResponse ? 200 : 204,
                RegisterHandler = s =>
                {
                    _ = (t.hasResponse, t.isSync, t.configuresPipeline, t.configuresReceiver) switch
                    {
                        (false, false, false, false) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandlerWithoutResponse.T,
                            (m, p, ct) => p.GetRequiredService<FnToCallFromHandler>()(m, ct)
                        ),
                        (true, false, false, false) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandler.T,
                            async (m, p, ct) =>
                            {
                                await p.GetRequiredService<FnToCallFromHandler>()(m, ct);

                                return new() { Payload = m.Payload + 1 };
                            }
                        ),
                        (false, true, false, false) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandlerWithoutResponse.T,
                            (m, p) =>
                                p.GetRequiredService<FnToCallFromHandler>()(m, CancellationToken.None)
                                    .GetAwaiter()
                                    .GetResult()
                        ),
                        (true, true, false, false) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandler.T,
                            (m, p) =>
                            {
                                p.GetRequiredService<FnToCallFromHandler>()(m, CancellationToken.None)
                                    .GetAwaiter()
                                    .GetResult();

                                return new() { Payload = m.Payload + 1 };
                            }
                        ),
                        (false, false, true, false) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandlerWithoutResponse.T,
                            (m, p, ct) => p.GetRequiredService<FnToCallFromHandler>()(m, ct),
                            p =>
                                p.Use(ctx =>
                                {
                                    _ = Interlocked.Increment(ref middlewareCallCount);

                                    return ctx.Next(ctx.Message, ctx.CancellationToken);
                                })
                        ),
                        (true, false, true, false) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandler.T,
                            async (m, p, ct) =>
                            {
                                await p.GetRequiredService<FnToCallFromHandler>()(m, ct);

                                return new() { Payload = m.Payload + 1 };
                            },
                            p =>
                                p.Use(ctx =>
                                {
                                    _ = Interlocked.Increment(ref middlewareCallCount);

                                    return ctx.Next(ctx.Message, ctx.CancellationToken);
                                })
                        ),
                        (false, true, true, false) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandlerWithoutResponse.T,
                            (m, p) =>
                                p.GetRequiredService<FnToCallFromHandler>()(m, CancellationToken.None)
                                    .GetAwaiter()
                                    .GetResult(),
                            p =>
                                p.Use(ctx =>
                                {
                                    _ = Interlocked.Increment(ref middlewareCallCount);

                                    return ctx.Next(ctx.Message, ctx.CancellationToken);
                                })
                        ),
                        (true, true, true, false) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandler.T,
                            async (m, p, ct) =>
                            {
                                await p.GetRequiredService<FnToCallFromHandler>()(m, ct);

                                return new() { Payload = m.Payload + 1 };
                            },
                            p =>
                                p.Use(ctx =>
                                {
                                    _ = Interlocked.Increment(ref middlewareCallCount);

                                    return ctx.Next(ctx.Message, ctx.CancellationToken);
                                })
                        ),
                        (false, false, false, true) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandlerWithoutResponse.T,
                            (m, p, ct) => p.GetRequiredService<FnToCallFromHandler>()(m, ct),
                            r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(r);
                            }
                        ),
                        (true, false, false, true) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandler.T,
                            async (m, p, ct) =>
                            {
                                await p.GetRequiredService<FnToCallFromHandler>()(m, ct);

                                return new() { Payload = m.Payload + 1 };
                            },
                            r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(r);
                            }
                        ),
                        (false, true, false, true) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandlerWithoutResponse.T,
                            (m, p) =>
                                p.GetRequiredService<FnToCallFromHandler>()(m, CancellationToken.None)
                                    .GetAwaiter()
                                    .GetResult(),
                            r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(r);
                            }
                        ),
                        (true, true, false, true) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandler.T,
                            async (m, p, ct) =>
                            {
                                await p.GetRequiredService<FnToCallFromHandler>()(m, ct);

                                return new() { Payload = m.Payload + 1 };
                            },
                            r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(r);
                            }
                        ),
                        (false, false, true, true) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandlerWithoutResponse.T,
                            (m, p, ct) => p.GetRequiredService<FnToCallFromHandler>()(m, ct),
                            p =>
                                p.Use(ctx =>
                                {
                                    _ = Interlocked.Increment(ref middlewareCallCount);

                                    return ctx.Next(ctx.Message, ctx.CancellationToken);
                                }),
                            r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(r);
                            }
                        ),
                        (true, false, true, true) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandler.T,
                            async (m, p, ct) =>
                            {
                                await p.GetRequiredService<FnToCallFromHandler>()(m, ct);

                                return new() { Payload = m.Payload + 1 };
                            },
                            p =>
                                p.Use(ctx =>
                                {
                                    _ = Interlocked.Increment(ref middlewareCallCount);

                                    return ctx.Next(ctx.Message, ctx.CancellationToken);
                                }),
                            r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(r);
                            }
                        ),
                        (false, true, true, true) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandlerWithoutResponse.T,
                            (m, p) =>
                                p.GetRequiredService<FnToCallFromHandler>()(m, CancellationToken.None)
                                    .GetAwaiter()
                                    .GetResult(),
                            p =>
                                p.Use(ctx =>
                                {
                                    _ = Interlocked.Increment(ref middlewareCallCount);

                                    return ctx.Next(ctx.Message, ctx.CancellationToken);
                                }),
                            r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(r);
                            }
                        ),
                        (true, true, true, true) => s.AddHttpMessageHandlerDelegate(
                            TestMessageWithDelegateHandler.T,
                            (m, p) =>
                            {
                                p.GetRequiredService<FnToCallFromHandler>()(m, CancellationToken.None)
                                    .GetAwaiter()
                                    .GetResult();

                                return new() { Payload = m.Payload + 1 };
                            },
                            p =>
                                p.Use(ctx =>
                                {
                                    _ = Interlocked.Increment(ref middlewareCallCount);

                                    return ctx.Next(ctx.Message, ctx.CancellationToken);
                                }),
                            r =>
                            {
                                _ = Interlocked.Increment(ref receiverConfigurationCount);

                                r.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(r);
                            }
                        ),
                    };
                },
                SendMessages = async (s, ct) =>
                {
                    if (t.hasResponse)
                    {
                        var r1 = await s.For(TestMessageWithDelegateHandler.T)
                            .WithDefaultSenderConfiguration()
                            .Handle(new() { Payload = 10 }, ct);
                        var r2 = await s.For(TestMessageWithDelegateHandler.T)
                            .WithDefaultSenderConfiguration()
                            .Handle(new() { Payload = 20 }, ct);

                        return [r1, r2];
                    }

                    await s.For(TestMessageWithDelegateHandlerWithoutResponse.T)
                        .WithDefaultSenderConfiguration()
                        .Handle(new() { Payload = 10 }, ct);
                    await s.For(TestMessageWithDelegateHandlerWithoutResponse.T)
                        .WithDefaultSenderConfiguration()
                        .Handle(new() { Payload = 20 }, ct);

                    return [];
                },
                MapEndpoints = e =>
                {
                    if (t.hasResponse)
                    {
                        _ = e.MapMessageEndpoint(TestMessageWithDelegateHandler.T);

                        return;
                    }

                    _ = e.MapMessageEndpoint(TestMessageWithDelegateHandlerWithoutResponse.T);
                },
                AfterMessagesAreReceived = _ =>
                {
                    Assert.That(middlewareCallCount, Is.EqualTo(t.configuresPipeline ? 2 : 0));
                    Assert.That(receiverConfigurationCount, Is.EqualTo(t.configuresReceiver ? 1 : 0));

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
            FullPath = "/api/test",
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessage { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var responses = await Task.WhenAll(
                    s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct),
                    s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct)
                );

                return responses;
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessage.T),
        };

        yield return new()
        {
            Name = "multiple receivers",
            NumOfReceivers = 2,
            FullPath = "", // makes no sense with multiple different message types
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessageWithFullPath { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s =>
                s.AddMessageHandler<TestMessageHandler>().AddMessageHandler<TestMessageWithFullPathHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithFullPath.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoints(),
        };

        yield return new()
        {
            Name = "type hierarchy with response",
            FullPath = "", // makes no sense with multiple different message types
            ExpectedReceivedMessages =
            [
                new TestMessageBase(Payload: 10),
                new TestMessageSub(Payload: 20, PayloadSub: 30),
                new TestMessageSub(Payload: 40, PayloadSub: 50),
            ],
            ExpectedResponses =
            [
                new TestMessageResponse { Payload = 11 },
                new TestMessageResponse { Payload = 22 },
                new TestMessageResponse { Payload = 42 },
            ],
            RegisterHandler = s => s.AddMessageHandler<MultiHierarchyTestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessageBase.T).WithDefaultSenderConfiguration().Handle(new(Payload: 10), ct);
                var r2 = await s.For(TestMessageSub.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new(Payload: 20, PayloadSub: 30), ct);
                var r3 = await s.For(TestMessageSub.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new TestMessageSubSub(Payload: 40, PayloadSub: 50, PayloadSubSub: 60), ct);

                return [r1, r2, r3];
            },
            MapEndpoints = e => e.MapMessageEndpoints(),
        };

        yield return new()
        {
            Name = "type hierarchy without response",
            FullPath = "", // makes no sense with multiple different message types
            ExpectedReceivedMessages =
            [
                new TestMessageBaseWithoutResponse(Payload: 10),
                new TestMessageSubWithoutResponse(Payload: 20, PayloadSub: 30),
                new TestMessageSubWithoutResponse(Payload: 40, PayloadSub: 50),
            ],
            ExpectedResponses = [],
            RegisterHandler = s => s.AddMessageHandler<MultiHierarchyTestMessageWithoutResponseHandler>(),
            SendMessages = async (s, ct) =>
            {
                await s.For(TestMessageBaseWithoutResponse.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new(Payload: 10), ct);
                await s.For(TestMessageSubWithoutResponse.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new(Payload: 20, PayloadSub: 30), ct);
                await s.For(TestMessageSubWithoutResponse.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new TestMessageSubSubWithoutResponse(Payload: 40, PayloadSub: 50, PayloadSubSub: 60), ct);

                return [];
            },
            MapEndpoints = e => e.MapMessageEndpoints(),
        };
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1250:Use implicit/explicit object creation",
        Justification = "it is clear what objects are being created here"
    )]
    public static IEnumerable<HttpMessageConformityExecutionSuccessTestCase> CreateSimpleSuccessTestCases()
    {
        yield return new()
        {
            Name = "with response",
            FullPath = "/api/test",
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessage { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s => s.AddMessageHandler<TestMessageHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoint(TestMessage.T),
        };

        yield return new()
        {
            Name = "multiple receivers",
            NumOfReceivers = 2,
            FullPath = "", // makes no sense with multiple different message types
            ExpectedReceivedMessages = [new TestMessage { Payload = 10 }, new TestMessageWithFullPath { Payload = 20 }],
            ExpectedResponses = [new TestMessageResponse { Payload = 11 }, new TestMessageResponse { Payload = 21 }],
            RegisterHandler = s =>
                s.AddMessageHandler<TestMessageHandler>().AddMessageHandler<TestMessageWithFullPathHandler>(),
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(TestMessage.T).WithDefaultSenderConfiguration().Handle(new() { Payload = 10 }, ct);
                var r2 = await s.For(TestMessageWithFullPath.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 20 }, ct);

                return [r1, r2];
            },
            MapEndpoints = e => e.MapMessageEndpoints(),
        };
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1250:Use implicit/explicit object creation",
        Justification = "it is clear what objects are being created here"
    )]
    public static IEnumerable<HttpMessageConformityExecutionErrorTestCase> CreateErrorTestCases()
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
            MapEndpoints = e => e.MapMessageEndpoint(TestMessage.T),
        };

        yield return new()
        {
            Name = "send error",
            ExpectedReceivedMessages = [],
            ExpectedResponses = [],
            ConfigurationExceptions = [],
            SendException = new InvalidOperationException("send error"),
            HandlerExceptions = [],
            RegisterHandler = _ => { },
            SendMessages = async (s, ct) =>
            {
                var r1 = await s.For(ThrowingTestMessage.T)
                    .WithDefaultSenderConfiguration()
                    .Handle(new() { Payload = 10 }, ct);

                return [r1];
            },
            MapEndpoints = _ => { },
        };
    }

    [SuppressMessage(
        "Roslynator",
        "RCS1250:Use implicit/explicit object creation",
        Justification = "it is clear what objects are being created here"
    )]
    public static IEnumerable<HttpMessageConformityContextTestCase> CreateContextTestCases()
    {
        foreach (
            var t in from hasActivity in new[] { true, false }
                     from hasDownstream in new[] { true, false }
                     from hasUpstream in new[] { true, false }
                     from hasBidirectional in new[] { true, false }
                     select (hasActivity, hasDownstream, hasUpstream, hasBidirectional)
        )
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
                    var response = await s.For(TestMessage.T)
                        .WithDefaultSenderConfiguration()
                        .Handle(new() { Payload = 10 }, ct);

                    return [response];
                },
                MapEndpoints = e => e.MapMessageEndpoints(),
            };
        }
    }

    [HttpMessage<TestMessageResponse>]
    public sealed partial record TestMessage
    {
        public required int Payload { get; init; }
    }

    public sealed record TestMessageResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageHandler(FnToCallFromHandler funToCallFromHandler) : TestMessage.IHandler
    {
        public static void ConfigurePipeline(TestMessage.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessage message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>]
    public sealed partial record TestMessageWithoutPayload;

    public sealed partial class TestMessageWithoutPayloadHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithoutPayload.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithoutPayload.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithoutPayload message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = 11 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage]
    public sealed partial record TestMessageWithoutResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithoutResponseHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithoutResponse.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithoutResponse.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage]
    public sealed partial record TestMessageWithoutResponseWithoutPayload;

    public sealed partial class TestMessageWithoutResponseWithoutPayloadHandler(
        FnToCallFromHandler funToCallFromHandler
    ) : TestMessageWithoutResponseWithoutPayload.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithoutResponseWithoutPayload.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task Handle(
            TestMessageWithoutResponseWithoutPayload message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodNames.Delete)]
    public sealed partial record TestMessageWithMethod
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithMethodHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithMethod.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithMethod.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithMethod message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(PathPrefix = "/custom/prefix")]
    public sealed partial record TestMessageWithPathPrefix
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithPathPrefixHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithPathPrefix.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithPathPrefix.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithPathPrefix message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(Version = "v2")]
    public sealed partial record TestMessageWithVersion
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithVersionHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithVersion.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithVersion.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithVersion message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(Path = "/custom/path")]
    public sealed partial record TestMessageWithPath
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithPathHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithPath.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithPath.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithPath message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(PathPrefix = "/custom/prefix", Version = "v3", Path = "/custom/path")]
    public sealed partial record TestMessageWithPathPrefixAndPathAndVersion
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithPathPrefixAndPathAndVersionHandler(
        FnToCallFromHandler funToCallFromHandler
    ) : TestMessageWithPathPrefixAndPathAndVersion.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithPathPrefixAndPathAndVersion.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithPathPrefixAndPathAndVersion message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(FullPath = "/custom/full/path/for/message")]
    public sealed partial record TestMessageWithFullPath
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithFullPathHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithFullPath.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithFullPath.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithFullPath message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(FullPath = "/custom/full/path/for/message/ignoring/version", Version = "v2")]
    public sealed partial record TestMessageWithFullPathAndVersion
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithFullPathAndVersionHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithFullPathAndVersion.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithFullPathAndVersion.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithFullPathAndVersion message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(SuccessStatusCode = 201)]
    public sealed partial record TestMessageWithSuccessStatusCode
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithSuccessStatusCodeHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithSuccessStatusCode.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithSuccessStatusCode.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithSuccessStatusCode message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(Name = "custom-message-name")]
    public sealed partial record TestMessageWithName
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithNameHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithName.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithName.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithName message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(ApiGroupName = "Custom Message Group")]
    public sealed partial record TestMessageWithApiGroupName
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithApiGroupNameHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithApiGroupName.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithApiGroupName.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithApiGroupName message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodNames.Get)]
    public sealed partial record TestMessageWithGet
    {
        public required int Payload { get; init; }

        public required string Param { get; init; }
    }

    public sealed partial class TestMessageWithGetHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithGet.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithGet.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithGet message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodNames.Get)]
    public sealed partial record TestMessageWithGetWithoutPayload;

    public sealed partial class TestMessageWithGetWithoutPayloadHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithGetWithoutPayload.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithGetWithoutPayload.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithGetWithoutPayload message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = 11 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodNames.Get)]
    public sealed partial record TestMessageWithGetWithOptionalPayload
    {
        public int? Payload { get; init; }

        public string? Param { get; init; }
    }

    public sealed partial class TestMessageWithGetWithOptionalPayloadHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithGetWithOptionalPayload.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithGetWithOptionalPayload.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithGetWithOptionalPayload message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = (message.Payload ?? 0) + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodNames.Get)]
    public sealed partial record TestMessageWithGetWithPrimaryConstructor(int Payload, string Param, int[] IntArray)
    {
        public bool Equals(TestMessageWithGetWithPrimaryConstructor? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Payload == other.Payload
                   && string.Equals(Param, other.Param, StringComparison.Ordinal)
                   && IntArray.SequenceEqual(other.IntArray);
        }

        public override int GetHashCode() => HashCode.Combine(Payload, Param, IntArray);
    }

    public sealed partial class TestMessageWithGetWithPrimaryConstructorHandler(
        FnToCallFromHandler funToCallFromHandler
    ) : TestMessageWithGetWithPrimaryConstructor.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithGetWithPrimaryConstructor.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithGetWithPrimaryConstructor message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + message.IntArray.Sum() };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodNames.Get)]
    public sealed partial record TestMessageWithGetWithPrimaryConstructorWithOptionalParameters(
        int? Payload = null,
        string? Param = null
    );

    public sealed partial class TestMessageWithGetWithPrimaryConstructorWithOptionalParametersHandler(
        FnToCallFromHandler funToCallFromHandler
    ) : TestMessageWithGetWithPrimaryConstructorWithOptionalParameters.IHandler
    {
        public static void ConfigurePipeline(
            TestMessageWithGetWithPrimaryConstructorWithOptionalParameters.IPipeline pipeline
        ) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithGetWithPrimaryConstructorWithOptionalParameters message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = (message.Payload ?? 0) + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>(HttpMethod = MethodNames.Get)]
    public sealed partial record TestMessageWithComplexGetPayload
    {
        [Required]
        public int? Payload { get; init; }

        public required List<int> NestedList { get; init; }

        public required int[] NestedArray { get; init; }

        public bool Equals(TestMessageWithComplexGetPayload? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Payload == other.Payload
                   && NestedList.SequenceEqual(other.NestedList)
                   && NestedArray.SequenceEqual(other.NestedArray);
        }

        public override int GetHashCode() => HashCode.Combine(Payload, NestedList, NestedArray);
    }

    public sealed partial class TestMessageWithComplexGetPayloadHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithComplexGetPayload.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithComplexGetPayload.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithComplexGetPayload message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse
            {
                Payload = (message.Payload ?? 0) + message.NestedList.Sum() + message.NestedArray.Sum(),
            };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageWithCustomSerializedPayloadTypeResponse>]
    public sealed partial record TestMessageWithCustomSerializedPayloadType
    {
        public required TestMessageWithCustomSerializedPayloadTypePayload Payload { get; init; }
    }

    public sealed record TestMessageWithCustomSerializedPayloadTypeResponse
    {
        public required TestMessageWithCustomSerializedPayloadTypePayload Payload { get; init; }
    }

    public sealed record TestMessageWithCustomSerializedPayloadTypePayload(int Payload);

    public sealed partial class TestMessageWithCustomSerializedPayloadTypeHandler(
        FnToCallFromHandler funToCallFromHandler
    ) : TestMessageWithCustomSerializedPayloadType.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithCustomSerializedPayloadType.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageWithCustomSerializedPayloadTypeResponse> Handle(
            TestMessageWithCustomSerializedPayloadType message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageWithCustomSerializedPayloadTypeResponse
            {
                Payload = new TestMessageWithCustomSerializedPayloadTypePayload(message.Payload.Payload + 1),
            };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);

        internal sealed class PayloadJsonConverterFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert) =>
                typeToConvert == typeof(TestMessageWithCustomSerializedPayloadTypePayload);

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
                Activator.CreateInstance<PayloadJsonConverter>();
        }

        internal sealed class PayloadJsonConverter : JsonConverter<TestMessageWithCustomSerializedPayloadTypePayload>
        {
            public override TestMessageWithCustomSerializedPayloadTypePayload Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options
            ) => new(reader.GetInt32());

            public override void Write(
                Utf8JsonWriter writer,
                TestMessageWithCustomSerializedPayloadTypePayload value,
                JsonSerializerOptions options
            ) => writer.WriteNumberValue(value.Payload);
        }
    }

    [HttpMessage<TestMessageWithCustomSerializerResponse>]
    public sealed partial record TestMessageWithCustomSerializer
    {
        public int PathPayload { get; init; }

        public int QueryPayload { get; init; }

        public int BodyPayload { get; init; }

        public static string FullPath => "/api/custom/path/for/serializer/{pathPayload:int}";

        static IHttpMessageSerializer<
            TestMessageWithCustomSerializer,
            TestMessageWithCustomSerializerResponse
        > IHttpMessage<
            TestMessageWithCustomSerializer,
            TestMessageWithCustomSerializerResponse
        >.HttpMessageSerializer
        {
            get;
        } = new TestMessageCustomSerializer();

        static IHttpMessageResponseSerializer<
            TestMessageWithCustomSerializer,
            TestMessageWithCustomSerializerResponse
        > IHttpMessage<
            TestMessageWithCustomSerializer,
            TestMessageWithCustomSerializerResponse
        >.HttpMessageResponseSerializer
        {
            get;
        } = new TestMessageCustomSerializer();
    }

    public sealed record TestMessageWithCustomSerializerResponse
    {
        public required int Payload { get; init; }
    }

    private sealed class TestMessageCustomSerializer
        : IHttpMessageSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>,
          IHttpMessageResponseSerializer<TestMessageWithCustomSerializer, TestMessageWithCustomSerializerResponse>
    {
        string IHttpMessageResponseSerializer<
            TestMessageWithCustomSerializer,
            TestMessageWithCustomSerializerResponse
        >.ContentType => "application/custom-response";

        public async Task SerializeResponse(
            IServiceProvider serviceProvider,
            Stream bodyStream,
            TestMessageWithCustomSerializerResponse response,
            CancellationToken cancellationToken
        )
        {
            await using var writer = new StreamWriter(bodyStream);
            await writer.WriteAsync($"total-payload:{response.Payload}");
        }

        public async Task<TestMessageWithCustomSerializerResponse> DeserializeResponse(
            IServiceProvider serviceProvider,
            Stream bodyStream,
            Encoding? encoding,
            CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            using var reader = new StreamReader(bodyStream, encoding ?? Encoding.UTF8, leaveOpen: true);
            var bodyContent = await reader.ReadToEndAsync(cancellationToken);
            var payload = int.Parse(bodyContent.Split(':')[1], CultureInfo.InvariantCulture);

            return new TestMessageWithCustomSerializerResponse { Payload = payload };
        }

        string IHttpMessageSerializer<
            TestMessageWithCustomSerializer,
            TestMessageWithCustomSerializerResponse
        >.ContentType => "application/custom-message";

        public string SerializeMessageToPath(
            IServiceProvider serviceProvider,
            TestMessageWithCustomSerializer message
        ) => $"/api/custom/path/for/serializer/{message.PathPayload}";

        public string SerializeMessageToQuery(
            IServiceProvider serviceProvider,
            TestMessageWithCustomSerializer message
        ) => $"?query-payload={message.QueryPayload}";

        public async Task SerializeMessageToBody(
            IServiceProvider serviceProvider,
            TestMessageWithCustomSerializer message,
            Stream bodyStream,
            CancellationToken cancellationToken
        )
        {
            await Task.Yield();

            await using var writer = new StreamWriter(bodyStream, Encoding.UTF8, leaveOpen: true);
            await writer.WriteAsync($"payload:{message.BodyPayload}");
        }

        public async Task<TestMessageWithCustomSerializer> DeserializeMessage(
            IServiceProvider serviceProvider,
            Stream bodyStream,
            Encoding? encoding,
            string path,
            IEnumerable<KeyValuePair<string, IReadOnlyList<string?>>> query,
            CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            using var reader = new StreamReader(bodyStream, encoding ?? Encoding.UTF8, leaveOpen: true);
            var bodyContent = await reader.ReadToEndAsync(cancellationToken);
            var bodyPayload = int.Parse(bodyContent.Split(':')[1], CultureInfo.InvariantCulture);
            var pathParam = int.Parse(
                path.Trim(trimChar: '/').Replace("api/custom/path/for/serializer/", "", StringComparison.Ordinal),
                CultureInfo.InvariantCulture
            );
            var queryParam = int.Parse(
                query.First(p => string.Equals(p.Key, "query-payload", StringComparison.Ordinal)).Value[0]!,
                CultureInfo.InvariantCulture
            );

            return new TestMessageWithCustomSerializer
            {
                BodyPayload = bodyPayload,
                PathPayload = pathParam,
                QueryPayload = queryParam,
            };
        }
    }

    public sealed partial class TestMessageWithCustomSerializerHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithCustomSerializer.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithCustomSerializer.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageWithCustomSerializerResponse> Handle(
            TestMessageWithCustomSerializer message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageWithCustomSerializerResponse
            {
                Payload = message.PathPayload + message.QueryPayload + message.BodyPayload,
            };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageWithCustomJsonTypeInfoResponse>]
    public sealed partial record TestMessageWithCustomJsonTypeInfo
    {
        public int MessagePayload { get; init; }
    }

    public sealed record TestMessageWithCustomJsonTypeInfoResponse
    {
        public int ResponsePayload { get; init; }
    }

    public sealed partial class TestMessageWithCustomJsonTypeInfoHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithCustomJsonTypeInfo.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithCustomJsonTypeInfo.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageWithCustomJsonTypeInfoResponse> Handle(
            TestMessageWithCustomJsonTypeInfo message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageWithCustomJsonTypeInfoResponse { ResponsePayload = message.MessagePayload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
    [JsonSerializable(typeof(TestMessageWithCustomJsonTypeInfo))]
    [JsonSerializable(typeof(TestMessageWithCustomJsonTypeInfoResponse))]
    internal sealed partial class TestMessageWithCustomJsonTypeInfoJsonSerializerContext : JsonSerializerContext;

    [HttpMessage<TestMessageResponse>]
    public sealed partial record TestMessageWithMiddleware
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithMiddlewareHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithMiddleware.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithMiddleware.IPipeline pipeline) =>
            pipeline
                .UseReceiverLogging()
                .Use(
                    pipeline.ServiceProvider.GetRequiredService<
                        TestMessageMiddleware<TestMessageWithMiddleware, TestMessageResponse>
                    >()
                );

        public async Task<TestMessageResponse> Handle(
            TestMessageWithMiddleware message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage]
    public sealed partial record TestMessageWithMiddlewareWithoutResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithMiddlewareWithoutResponseHandler(
        FnToCallFromHandler funToCallFromHandler
    ) : TestMessageWithMiddlewareWithoutResponse.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithMiddlewareWithoutResponse.IPipeline pipeline) =>
            pipeline
                .UseReceiverLogging()
                .Use(
                    pipeline.ServiceProvider.GetRequiredService<
                        TestMessageMiddleware<TestMessageWithMiddlewareWithoutResponse, UnitMessageResponse>
                    >()
                );

        public async Task Handle(
            TestMessageWithMiddlewareWithoutResponse message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    public sealed class TestMessageMiddleware<TMessage, TResponse>(TestObservations observations)
        : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            observations.SeenTransportTypeInMiddleware = ctx.TransportType;

            return ctx.Next(ctx.Message, ctx.CancellationToken);
        }
    }

    [HttpMessage<TestMessageResponse[]>]
    public sealed partial record TestMessageWithArrayResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithArrayResponseHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithArrayResponse.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithArrayResponse.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse[]> Handle(
            TestMessageWithArrayResponse message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return [new() { Payload = message.Payload + 1 }, new() { Payload = message.Payload + 2 }];
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<List<TestMessageResponse>>]
    public sealed partial record TestMessageWithListResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithListResponseHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithListResponse.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithListResponse.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<List<TestMessageResponse>> Handle(
            TestMessageWithListResponse message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new List<TestMessageResponse>
            {
                new() { Payload = message.Payload + 1 },
                new() { Payload = message.Payload + 2 },
            };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<IEnumerable<TestMessageResponse>>]
    public sealed partial record TestMessageWithEnumerableResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithEnumerableResponseHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithEnumerableResponse.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithEnumerableResponse.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<IEnumerable<TestMessageResponse>> Handle(
            TestMessageWithEnumerableResponse message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new List<TestMessageResponse>
            {
                new() { Payload = message.Payload + 1 },
                new() { Payload = message.Payload + 2 },
            }.AsReadOnly();
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageForAssemblyScanningResponse>]
    public sealed partial record TestMessageForAssemblyScanning
    {
        public required int Payload { get; init; }
    }

    public sealed record TestMessageForAssemblyScanningResponse
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageForAssemblyScanningHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageForAssemblyScanning.IHandler
    {
        public static void ConfigurePipeline(TestMessageForAssemblyScanning.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageForAssemblyScanningResponse> Handle(
            TestMessageForAssemblyScanning message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageForAssemblyScanningResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage]
    public sealed partial record TestMessageWithoutResponseForAssemblyScanning
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithoutResponseForAssemblyScanningHandler(
        FnToCallFromHandler funToCallFromHandler
    ) : TestMessageWithoutResponseForAssemblyScanning.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithoutResponseForAssemblyScanning.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task Handle(
            TestMessageWithoutResponseForAssemblyScanning message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [CustomHttpMessage<TestMessageResponse>(CustomPathPrefix = "customApi")]
    public sealed partial record TestMessageWithCustomConventions
    {
        public required int Payload { get; init; }
    }

    public sealed partial class TestMessageWithCustomConventionsHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageWithCustomConventions.IHandler
    {
        public static void ConfigurePipeline(TestMessageWithCustomConventions.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageWithCustomConventions message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public static void ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>]
    public sealed partial record TestMessageWithDelegateHandler
    {
        public required int Payload { get; init; }
    }

    [HttpMessage]
    public sealed partial record TestMessageWithDelegateHandlerWithoutResponse
    {
        public required int Payload { get; init; }
    }

    [HttpMessage<TestMessageResponse>]
    public partial record TestMessageBase(int Payload);

    [HttpMessage<TestMessageResponse>]
    public partial record TestMessageSub(int Payload, int PayloadSub) : TestMessageBase(Payload);

    public sealed record TestMessageSubSub(int Payload, int PayloadSub, int PayloadSubSub)
        : TestMessageSub(Payload, PayloadSub);

    private sealed partial class MultiHierarchyTestMessageHandler(FnToCallFromHandler funToCallFromHandler)
        : TestMessageBase.IHandler,
          TestMessageSub.IHandler
    {
        public static void ConfigurePipeline(TestMessageBase.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public static void ConfigurePipeline(TestMessageSub.IPipeline pipeline) => pipeline.UseReceiverLogging();

        public async Task<TestMessageResponse> Handle(
            TestMessageBase message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 1 };
        }

        public async Task<TestMessageResponse> Handle(
            TestMessageSub message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);

            return new TestMessageResponse { Payload = message.Payload + 2 };
        }

        static void IHttpMessageHandler.ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage]
    public partial record TestMessageBaseWithoutResponse(int Payload);

    [HttpMessage]
    public partial record TestMessageSubWithoutResponse(int Payload, int PayloadSub)
        : TestMessageBaseWithoutResponse(Payload);

    public sealed record TestMessageSubSubWithoutResponse(int Payload, int PayloadSub, int PayloadSubSub)
        : TestMessageSubWithoutResponse(Payload, PayloadSub);

    private sealed partial class MultiHierarchyTestMessageWithoutResponseHandler(
        FnToCallFromHandler funToCallFromHandler
    ) : TestMessageBaseWithoutResponse.IHandler, TestMessageSubWithoutResponse.IHandler
    {
        public static void ConfigurePipeline(TestMessageBaseWithoutResponse.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public static void ConfigurePipeline(TestMessageSubWithoutResponse.IPipeline pipeline) =>
            pipeline.UseReceiverLogging();

        public async Task Handle(TestMessageBaseWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);
        }

        public async Task Handle(TestMessageSubWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            await funToCallFromHandler(message, cancellationToken);
        }

        static void IHttpMessageHandler.ConfigureHttpReceiver(IHttpMessageReceiver receiver) =>
            receiver.ServiceProvider.GetService<Action<IHttpMessageReceiver>>()?.Invoke(receiver);
    }

    [HttpMessage<TestMessageResponse>]
    private sealed partial record ThrowingTestMessage
    {
        public required int Payload { get; init; }

        static IHttpMessageSerializer<ThrowingTestMessage, TestMessageResponse> IHttpMessage<
            ThrowingTestMessage,
            TestMessageResponse
        >.HttpMessageSerializer
        {
            get;
        } = new ThrowingTestMessageSerializer();
    }

    private sealed class ThrowingTestMessageSerializer
        : IHttpMessageSerializer<ThrowingTestMessage, TestMessageResponse>
    {
        public string ContentType => "application/throwing";

        public Task SerializeMessageToBody(
            IServiceProvider serviceProvider,
            ThrowingTestMessage message,
            Stream bodyStream,
            CancellationToken cancellationToken
        ) => throw serviceProvider.GetRequiredService<Exception>();

        public Task<ThrowingTestMessage> DeserializeMessage(
            IServiceProvider serviceProvider,
            Stream bodyStream,
            Encoding? encoding,
            string path,
            IEnumerable<KeyValuePair<string, IReadOnlyList<string?>>> query,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
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
        this IMessagePipeline<TMessage, TResponse> pipeline
    )
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var sendCallback = pipeline.ServiceProvider.GetService<
            Func<object, ConquerorContext, CancellationToken, Task>
        >();

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

    public static IMessagePipeline<TMessage, TResponse> UseLogging<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline
    )
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var logger = pipeline.ServiceProvider.GetRequiredService<ILogger>();

        return pipeline.Use(ctx =>
        {
            logger.LogInformation("sending message...");

            return ctx.Next(ctx.Message, ctx.CancellationToken);
        });
    }

    public static IMessagePipeline<TMessage, TResponse> UseReceiverLogging<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline
    )
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
        this IMessageHandler<TMessage, TResponse, TIHandler> handler
    )
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        return handler
            .WithPipeline(p => _ = p.UseLogging().UseSendCallback())
            .WithTransport(b =>
                b.UseHttp(new("http://conqueror.test"))
                    .WithHttpClient(b.ServiceProvider.GetRequiredService<HttpClient>())
            );
    }
}

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "used by source generator")]
[MessageTransport(
    Prefix = "Http",
    Namespace = "Conqueror",
    FullyQualifiedMessageTypeName = "Conqueror.Transport.Http.Tests.Messaging.ICustomHttpMessage"
)]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "used by source generator"
)]
public sealed class CustomHttpMessageAttribute<TResponse> : Attribute
{
    public string? CustomPathPrefix { get; set; }
}

[SuppressMessage(
    "ReSharper",
    "StaticMemberInGenericType",
    Justification = "The static members are intentionally per generic type"
)]
[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Members are set via code generation")]
public interface ICustomHttpMessage<TMessage, TResponse> : IHttpMessage<TMessage, TResponse>
    where TMessage : class, ICustomHttpMessage<TMessage, TResponse>
{
    static virtual string? CustomPathPrefix { get; }
    static string IHttpMessage<TMessage, TResponse>.PathPrefix => TMessage.CustomPathPrefix ?? "api";

    static int IHttpMessage<TMessage, TResponse>.SuccessStatusCode => 201;
}
