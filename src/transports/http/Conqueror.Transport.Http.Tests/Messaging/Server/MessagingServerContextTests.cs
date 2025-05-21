using System.Net.Http.Headers;
using System.Net.Mime;
using static Conqueror.Transport.Http.Tests.HttpTestContextData;
using static Conqueror.Transport.Http.Tests.Messaging.HttpTestMessages;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace Conqueror.Transport.Http.Tests.Messaging.Server;

[TestFixture]
public sealed partial class MessagingServerContextTests
{
    private DisposableActivity? activity;

    [Test]
    [TestCaseSource(nameof(GenerateContextDataTestCases))]
    public async Task GivenContextData_WhenSendingMessage_DataIsCorrectlySentAndReturned<TMessage, TResponse, TIHandler, THandler>(
        bool hasUpstream,
        bool hasDownstream,
        bool hasBidirectional,
        MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler
    {
        await using var host = await CreateTestHost(
            services => services.RegisterMessageType<TMessage, TResponse, TIHandler, THandler>(testCase),
            app => app.MapMessageEndpoints<TMessage, TResponse, TIHandler>(testCase));

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();

        var testObservations = host.Resolve<TestObservations>();

        testObservations.ShouldAddUpstreamData = hasUpstream;
        testObservations.ShouldAddBidirectionalData = hasBidirectional;

        using var request = ConstructHttpRequest(testCase);

        if (hasDownstream)
        {
            foreach (var (key, value) in ContextData)
            {
                conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            }

            ((HttpHeaders?)request.Content?.Headers ?? request.Headers).Add(ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData());
        }

        var response = await host.HttpClient.SendAsync(request);

        if (!testCase.HandlerIsEnabled)
        {
            await response.AssertStatusCode(StatusCodes.Status404NotFound);
            Assert.That(response.Headers.TryGetValues(ConquerorContextHeaderName, out _), Is.False);
            return;
        }

        await response.AssertSuccessStatusCode();

        var exists = response.Headers.TryGetValues(ConquerorContextHeaderName, out var values);

        Assert.That(exists, Is.EqualTo(hasUpstream || hasBidirectional));

        if (values is not null)
        {
            conquerorContext.DecodeContextData(values);
        }

        Assert.That(conquerorContext.UpstreamContextData.WhereScopeIsAcrossTransports(), hasUpstream ? Is.EquivalentTo(ContextData) : Is.Empty);
        Assert.That(conquerorContext.ContextData.WhereScopeIsAcrossTransports(), hasBidirectional ? Is.EquivalentTo(ContextData) : Is.Empty);

        var receivedContextData = testObservations.ReceivedDownstreamContextData;
        var receivedBidirectionalContextData = testObservations.ReceivedBidirectionalContextData;

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

    [Test]
    [TestCaseSource(typeof(HttpTestMessages), nameof(GenerateTestCaseData))]
    public async Task GivenMultipleConquerorContextRequestHeadersWithDownstreamAndBidirectionalData_WhenSendingMessage_DataIsReceivedByHandler<TMessage, TResponse, TIHandler, THandler>(
        MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler
    {
        await using var host = await CreateTestHost(
            services => services.RegisterMessageType<TMessage, TResponse, TIHandler, THandler>(testCase),
            app => app.MapMessageEndpoints<TMessage, TResponse, TIHandler>(testCase));

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();

        foreach (var (key, value) in ContextData)
        {
            conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        var encodedData1 = conquerorContext.EncodeDownstreamContextData();

        conquerorContext.DownstreamContextData.Clear();
        conquerorContext.ContextData.Clear();

        conquerorContext.DownstreamContextData.Set("extraKey", "extraValue", ConquerorContextDataScope.AcrossTransports);
        conquerorContext.ContextData.Set("extraKey", "extraValue", ConquerorContextDataScope.AcrossTransports);

        var encodedData2 = conquerorContext.EncodeDownstreamContextData();

        using var request = ConstructHttpRequest(testCase);

        ((HttpHeaders?)request.Content?.Headers ?? request.Headers).Add(ConquerorContextHeaderName, encodedData1);
        ((HttpHeaders?)request.Content?.Headers ?? request.Headers).Add(ConquerorContextHeaderName, encodedData2);

        var response = await host.HttpClient.SendAsync(request);

        if (!testCase.HandlerIsEnabled)
        {
            await response.AssertStatusCode(StatusCodes.Status404NotFound);
            Assert.That(response.Headers.TryGetValues(ConquerorContextHeaderName, out _), Is.False);
            return;
        }

        await response.AssertSuccessStatusCode();

        var receivedDownstreamContextData = host.Resolve<TestObservations>().ReceivedDownstreamContextData;
        var receivedBidirectionalContextData = host.Resolve<TestObservations>().ReceivedBidirectionalContextData;

        Assert.That(receivedDownstreamContextData, Is.Not.Null);
        Assert.That(receivedBidirectionalContextData, Is.Not.Null);
        Assert.That(ContextData.Concat([new("extraKey", "extraValue")]), Is.SubsetOf(receivedDownstreamContextData!.AsKeyValuePairs<string>()));
        Assert.That(ContextData.Concat([new("extraKey", "extraValue")]), Is.SubsetOf(receivedBidirectionalContextData!.AsKeyValuePairs<string>()));
    }

    [Test]
    [TestCaseSource(typeof(HttpTestMessages), nameof(GenerateTestCaseData))]
    public async Task GivenInvalidConquerorContextRequestHeader_WhenSendingMessage_ReturnsBadRequest<TMessage, TResponse, TIHandler, THandler>(
        MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler
    {
        await using var host = await CreateTestHost(
            services => services.RegisterMessageType<TMessage, TResponse, TIHandler, THandler>(testCase),
            app => app.MapMessageEndpoints<TMessage, TResponse, TIHandler>(testCase));

        using var request = ConstructHttpRequest(testCase);

        ((HttpHeaders?)request.Content?.Headers ?? request.Headers).Add(ConquerorContextHeaderName, "foo=bar");

        var response = await host.HttpClient.SendAsync(request);
        await response.AssertStatusCode(testCase.HandlerIsEnabled ? StatusCodes.Status400BadRequest : StatusCodes.Status404NotFound);
        Assert.That(response.Headers.TryGetValues(ConquerorContextHeaderName, out _), Is.False);
    }

    [Test]
    [TestCaseSource(typeof(HttpTestMessages), nameof(GenerateTestCaseData))]
    public async Task GivenMessageIdInContext_WhenSendingMessage_MessageIdIsObservedByHandler<TMessage, TResponse, TIHandler, THandler>(
        MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler
    {
        await using var host = await CreateTestHost(
            services => services.RegisterMessageType<TMessage, TResponse, TIHandler, THandler>(testCase),
            app => app.MapMessageEndpoints<TMessage, TResponse, TIHandler>(testCase));

        const string messageId = "test-message";

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetMessageId(messageId);

        using var request = ConstructHttpRequest(testCase);

        ((HttpHeaders?)request.Content?.Headers ?? request.Headers).Add(ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData());

        var response = await host.HttpClient.SendAsync(request);

        if (!testCase.HandlerIsEnabled)
        {
            await response.AssertStatusCode(StatusCodes.Status404NotFound);
            Assert.That(response.Headers.TryGetValues(ConquerorContextHeaderName, out _), Is.False);
            return;
        }

        await response.AssertSuccessStatusCode();

        var receivedMessageIds = host.Resolve<TestObservations>().ReceivedMessageIds;

        Assert.That(receivedMessageIds, Is.EqualTo(new[] { messageId }));
    }

    [Test]
    [TestCaseSource(typeof(HttpTestMessages), nameof(GenerateTestCaseData))]
    public async Task GivenNoMessageIdInContext_WhenSendingMessage_NonEmptyMessageIdIsObservedByHandler<TMessage, TResponse, TIHandler, THandler>(
        MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler
    {
        await using var host = await CreateTestHost(
            services => services.RegisterMessageType<TMessage, TResponse, TIHandler, THandler>(testCase),
            app => app.MapMessageEndpoints<TMessage, TResponse, TIHandler>(testCase));

        using var request = ConstructHttpRequest(testCase);

        var response = await host.HttpClient.SendAsync(request);

        if (!testCase.HandlerIsEnabled)
        {
            await response.AssertStatusCode(StatusCodes.Status404NotFound);
            Assert.That(response.Headers.TryGetValues(ConquerorContextHeaderName, out _), Is.False);
            return;
        }

        await response.AssertSuccessStatusCode();

        var receivedMessageIds = host.Resolve<TestObservations>().ReceivedMessageIds;

        Assert.That(receivedMessageIds, Has.Count.EqualTo(1));
        Assert.That(receivedMessageIds[0], Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [TestCaseSource(typeof(HttpTestMessages), nameof(GenerateTestCaseData))]
    public async Task GivenTraceIdInTraceParentWithoutActiveActivity_WhenSendingMessage_IdFromActivityIsObservedByHandlers<TMessage, TResponse, TIHandler, THandler>(
        MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler
    {
        await using var host = await CreateTestHost(services =>
        {
            services.RegisterMessageType<TMessage, TResponse, TIHandler, THandler>(testCase);
            _ = services.AddMessageHandler<NestedTestMessageHandler>();

            _ = services.Replace(ServiceDescriptor.Singleton<FnToCallFromHandler>((_, p) =>
            {
                ObserveAndSetContextData(p.GetRequiredService<TestObservations>(), p.GetRequiredService<IConquerorContextAccessor>());
                return p.GetRequiredService<IMessageSenders>().For(NestedTestMessage.T).Handle(new());
            }));
        }, app => app.MapMessageEndpoints<TMessage, TResponse, TIHandler>(testCase));

        using var request = ConstructHttpRequest(testCase);

        const string traceId = "80e1a2ed08e019fc1110464cfa66635c";
        ((HttpHeaders?)request.Content?.Headers ?? request.Headers).Add(TraceParentHeaderName, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01");

        var response = await host.HttpClient.SendAsync(request);

        if (!testCase.HandlerIsEnabled)
        {
            await response.AssertStatusCode(StatusCodes.Status404NotFound);
            Assert.That(response.Headers.TryGetValues(ConquerorContextHeaderName, out _), Is.False);
            return;
        }

        await response.AssertSuccessStatusCode();

        var receivedTraceIds = host.Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Is.EqualTo(new[] { traceId, traceId }));
    }

    [Test]
    [TestCaseSource(typeof(HttpTestMessages), nameof(GenerateTestCaseData))]
    public async Task GivenTraceIdInTraceParentWithActiveActivity_WhenSendingMessage_IdFromActivityIsObservedByHandler<TMessage, TResponse, TIHandler, THandler>(
        MessageTestCase testCase)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler
    {
        await using var host = await CreateTestHost(services =>
        {
            services.RegisterMessageType<TMessage, TResponse, TIHandler, THandler>(testCase);
            _ = services.AddMessageHandler<NestedTestMessageHandler>();

            _ = services.Replace(ServiceDescriptor.Singleton<FnToCallFromHandler>((_, p) =>
            {
                ObserveAndSetContextData(p.GetRequiredService<TestObservations>(), p.GetRequiredService<IConquerorContextAccessor>());
                return p.GetRequiredService<IMessageSenders>().For(NestedTestMessage.T).Handle(new());
            }));
        }, app => app.MapMessageEndpoints<TMessage, TResponse, TIHandler>(testCase));

        using var a = DisposableActivity.Create(nameof(GivenTraceIdInTraceParentWithActiveActivity_WhenSendingMessage_IdFromActivityIsObservedByHandler));
        activity = a;

        using var request = ConstructHttpRequest(testCase);

        ((HttpHeaders?)request.Content?.Headers ?? request.Headers).Add(TraceParentHeaderName, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01");

        var response = await host.HttpClient.SendAsync(request);

        if (!testCase.HandlerIsEnabled)
        {
            await response.AssertStatusCode(StatusCodes.Status404NotFound);
            Assert.That(response.Headers.TryGetValues(ConquerorContextHeaderName, out _), Is.False);
            return;
        }

        await response.AssertSuccessStatusCode();

        var receivedTraceIds = host.Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Is.EqualTo(new[] { a.TraceId, a.TraceId }));
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "content is disposed together with request outside")]
    private static HttpRequestMessage ConstructHttpRequest(MessageTestCase testCase)
    {
        HttpRequestMessage? request = null;
        try
        {
            var targetUriBuilder = new UriBuilder
            {
                Host = "localhost",
                Path = testCase.FullPath,
            };

            if (testCase.QueryString is not null)
            {
                targetUriBuilder.Query = testCase.QueryString;
            }

            request = new(new(testCase.HttpMethod), targetUriBuilder.Uri);

            var content = testCase.Payload is not null ? CreateJsonStringContent(testCase.Payload) : new(string.Empty);

            if (testCase.HttpMethod != MethodGet)
            {
                request.Content = content;
            }

            return request;
        }
        catch
        {
            request?.Dispose();
            throw;
        }
    }

    private Task<HttpTransportTestHost> CreateTestHost(Action<IServiceCollection> configureServices,
                                                       Action<IApplicationBuilder> configure)
    {
        return HttpTransportTestHost.Create(
            services =>
            {
                _ = services.AddSingleton<FnToCallFromHandler>((_, p) =>
                {
                    ObserveAndSetContextData(p.GetRequiredService<TestObservations>(), p.GetRequiredService<IConquerorContextAccessor>());
                    return Task.CompletedTask;
                });

                configureServices(services);

                // we generate a lot of test cases, which would produce a lot of meaningless logs that cause
                // a lot of churn on the disk as well as the CI infra, so we increase the minimal log level
                _ = services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Information));
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

    private static IEnumerable<TestCaseData> GenerateContextDataTestCases()
    {
        return from testCaseData in GenerateTestCaseData()
               from hasUpstream in new[] { true, false }
               from hasDownstream in new[] { true, false }
               from hasBidirectional in new[] { true, false }
               select new TestCaseData(hasUpstream, hasDownstream, hasBidirectional, testCaseData.Arguments[0])
               {
                   TypeArgs = testCaseData.TypeArgs,
               };
    }

    private static StringContent CreateJsonStringContent(string content)
    {
        return new(content, new MediaTypeHeaderValue(MediaTypeNames.Application.Json));
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

    [Message<NestedTestMessageResponse>]
    public sealed partial record NestedTestMessage
    {
        public int Payload { get; init; }
    }

    public sealed record NestedTestMessageResponse
    {
        public int Payload { get; init; }
    }

    public sealed partial class NestedTestMessageHandler(
        TestObservations testObservations,
        IConquerorContextAccessor contextAccessor)
        : NestedTestMessage.IHandler
    {
        public async Task<NestedTestMessageResponse> Handle(NestedTestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            testObservations.ReceivedTraceIds.Add(contextAccessor.ConquerorContext?.GetTraceId());
            return new() { Payload = message.Payload + 1 };
        }
    }
}
