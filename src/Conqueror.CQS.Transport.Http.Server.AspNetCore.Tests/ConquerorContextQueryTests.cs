using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using Conqueror.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Net.Http.Headers;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
[NonParallelizable]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
public sealed class ConquerorContextQueryTests : TestBase
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

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenUpstreamContextData_DataIsReturnedInHeader(string path, string? postContent = null)
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        var response = postContent != null ? await HttpClient.PostAsync(path, content) : await HttpClient.GetAsync(path);
        await response.AssertSuccessStatusCode();

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        using var ctx = CreateConquerorContext();
        ctx.DecodeContextData(values!);

        Assert.That(ctx.UpstreamContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
        Assert.That(ctx.ContextData, Is.Empty);
    }

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenBidirectionalContextData_DataIsReturnedInHeader(string path, string? postContent = null)
    {
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;

        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        var response = postContent != null ? await HttpClient.PostAsync(path, content) : await HttpClient.GetAsync(path);
        await response.AssertSuccessStatusCode();

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        using var ctx = CreateConquerorContext();
        ctx.DecodeContextData(values!);

        Assert.That(ctx.UpstreamContextData, Is.Empty);
        Assert.That(ctx.ContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
    }

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenUpstreamAndBidirectionalContextData_DataIsReturnedInHeader(string path, string? postContent = null)
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;
        Resolve<TestObservations>().ShouldAddBidirectionalData = true;

        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        var response = postContent != null ? await HttpClient.PostAsync(path, content) : await HttpClient.GetAsync(path);
        await response.AssertSuccessStatusCode();

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        using var ctx = CreateConquerorContext();
        ctx.DecodeContextData(values!);

        Assert.That(ctx.UpstreamContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
        Assert.That(ctx.ContextData.WhereScopeIsAcrossTransports(), Is.EquivalentTo(ContextData));
    }

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenConquerorContextRequestHeaderWithDownstreamData_DataIsReceivedByHandler(string path, string? postContent = null)
    {
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();

        foreach (var (key, value) in ContextData)
        {
            conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        using var msg = new HttpRequestMessage
        {
            Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
            RequestUri = new(path, UriKind.Relative),
            Headers = { { HttpConstants.ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData() } },
            Content = postContent != null ? content : null,
        };

        var response = await HttpClient.SendAsync(msg);
        await response.AssertSuccessStatusCode();

        var receivedContextData = Resolve<TestObservations>().ReceivedDownstreamContextData;

        Assert.That(receivedContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(receivedContextData!.AsKeyValuePairs<string>()));
        Assert.That(Resolve<TestObservations>().ReceivedBidirectionalContextData?.WhereScopeIsAcrossTransports().Intersect(ContextData), Is.Empty);
    }

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenConquerorContextRequestHeaderWithBidirectionalData_DataIsReceivedByHandler(string path, string? postContent = null)
    {
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();

        foreach (var (key, value) in ContextData)
        {
            conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        using var msg = new HttpRequestMessage
        {
            Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
            RequestUri = new(path, UriKind.Relative),
            Headers = { { HttpConstants.ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData() } },
            Content = postContent != null ? content : null,
        };

        var response = await HttpClient.SendAsync(msg);
        await response.AssertSuccessStatusCode();

        var receivedContextData = Resolve<TestObservations>().ReceivedBidirectionalContextData;

        Assert.That(receivedContextData?.AsKeyValuePairs<string>(), Is.EquivalentTo(ContextData));
        Assert.That(Resolve<TestObservations>().ReceivedDownstreamContextData?.WhereScopeIsAcrossTransports().Intersect(ContextData), Is.Empty);
    }

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenConquerorContextRequestHeaderWithDownstreamAndBidirectionalData_DataIsReceivedByHandler(string path, string? postContent = null)
    {
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();

        foreach (var (key, value) in ContextData)
        {
            conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        using var msg = new HttpRequestMessage
        {
            Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
            RequestUri = new(path, UriKind.Relative),
            Headers = { { HttpConstants.ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData() } },
            Content = postContent != null ? content : null,
        };

        var response = await HttpClient.SendAsync(msg);
        await response.AssertSuccessStatusCode();

        var receivedDownstreamContextData = Resolve<TestObservations>().ReceivedDownstreamContextData;
        var receivedBidirectionalContextData = Resolve<TestObservations>().ReceivedBidirectionalContextData;

        Assert.That(receivedDownstreamContextData, Is.Not.Null);
        Assert.That(receivedBidirectionalContextData, Is.Not.Null);
        Assert.That(ContextData, Is.SubsetOf(receivedDownstreamContextData!.AsKeyValuePairs<string>()));
        Assert.That(ContextData, Is.SubsetOf(receivedBidirectionalContextData!.AsKeyValuePairs<string>()));
    }

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenMultipleConquerorContextRequestHeadersWithDownstreamAndBidirectionalData_DataIsReceivedByHandler(string path, string? postContent = null)
    {
        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();

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

        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        using var msg = new HttpRequestMessage
        {
            Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
            RequestUri = new(path, UriKind.Relative),
            Headers =
            {
                { HttpConstants.ConquerorContextHeaderName, encodedData1 },
                { HttpConstants.ConquerorContextHeaderName, encodedData2 },
            },
            Content = postContent != null ? content : null,
        };

        var response = await HttpClient.SendAsync(msg);
        await response.AssertSuccessStatusCode();

        var receivedDownstreamContextData = Resolve<TestObservations>().ReceivedDownstreamContextData;
        var receivedBidirectionalContextData = Resolve<TestObservations>().ReceivedBidirectionalContextData;

        Assert.That(receivedDownstreamContextData, Is.Not.Null);
        Assert.That(receivedBidirectionalContextData, Is.Not.Null);
        Assert.That(ContextData.Concat([new("extraKey", "extraValue")]), Is.SubsetOf(receivedDownstreamContextData!.AsKeyValuePairs<string>()));
        Assert.That(ContextData.Concat([new("extraKey", "extraValue")]), Is.SubsetOf(receivedBidirectionalContextData!.AsKeyValuePairs<string>()));
    }

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenInvalidConquerorContextRequestHeader_ReturnsBadRequest(string path, string? postContent = null)
    {
        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        using var msg = new HttpRequestMessage
        {
            Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
            RequestUri = new(path, UriKind.Relative),
            Headers = { { HttpConstants.ConquerorContextHeaderName, "foo=bar" } },
            Content = postContent != null ? content : null,
        };

        var response = await HttpClient.SendAsync(msg);
        await response.AssertStatusCode(HttpStatusCode.BadRequest);
    }

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenQueryIdInContext_QueryIdIsObservedByHandler(string path, string? postContent = null)
    {
        const string queryId = "test-query";

        using var conquerorContext = Resolve<IConquerorContextAccessor>().GetOrCreate();
        conquerorContext.SetQueryId(queryId);

        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        using var msg = new HttpRequestMessage
        {
            Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
            RequestUri = new(path, UriKind.Relative),
            Headers = { { HttpConstants.ConquerorContextHeaderName, conquerorContext.EncodeDownstreamContextData() } },
            Content = postContent != null ? content : null,
        };

        var response = await HttpClient.SendAsync(msg);
        await response.AssertSuccessStatusCode();

        var receivedQueryIds = Resolve<TestObservations>().ReceivedQueryIds;

        Assert.That(receivedQueryIds, Is.EqualTo(new[] { queryId }));
    }

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenNoQueryIdInContext_NonEmptyQueryIdIsObservedByHandler(string path, string? postContent = null)
    {
        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        using var msg = new HttpRequestMessage
        {
            Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
            RequestUri = new(path, UriKind.Relative),
            Content = postContent != null ? content : null,
        };

        var response = await HttpClient.SendAsync(msg);
        await response.AssertSuccessStatusCode();

        var receivedQueryIds = Resolve<TestObservations>().ReceivedQueryIds;

        Assert.That(receivedQueryIds, Has.Count.EqualTo(1));
        Assert.That(receivedQueryIds[0], Is.Not.Null.And.Not.Empty);
    }

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenTraceIdInTraceParentHeaderWithoutActiveActivity_IdFromHeaderIsObservedByHandler(string path, string? postContent = null)
    {
        const string testTraceId = "80e1a2ed08e019fc1110464cfa66635c";
        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        using var msg = new HttpRequestMessage
        {
            Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
            RequestUri = new(path, UriKind.Relative),
            Headers =
            {
                { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
            },
            Content = postContent != null ? content : null,
        };

        var response = await HttpClient.SendAsync(msg);
        await response.AssertSuccessStatusCode();

        var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Is.EquivalentTo(new[] { testTraceId }));
    }

    [Test]
    public async Task GivenTraceIdInTraceParentHeaderWithoutActiveActivity_IdFromHeaderIsObservedByHandlerAndNestedHandler()
    {
        const string testTraceId = "80e1a2ed08e019fc1110464cfa66635c";
        using var msg = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new("/api/queries/testQueryWithNested", UriKind.Relative),
            Headers =
            {
                { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
            },
        };

        var response = await HttpClient.SendAsync(msg);
        await response.AssertSuccessStatusCode();

        var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Has.Count.EqualTo(2));
        Assert.That(receivedTraceIds[0], Is.EqualTo(testTraceId));
        Assert.That(receivedTraceIds[1], Is.EqualTo(testTraceId));
    }

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandler(string path, string? postContent = null)
    {
        using var a = CreateActivity(nameof(GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandler));
        activity = a;

        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        using var msg = new HttpRequestMessage
        {
            Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
            RequestUri = new(path, UriKind.Relative),
            Headers =
            {
                { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
            },
            Content = postContent != null ? content : null,
        };

        var response = await HttpClient.SendAsync(msg);
        await response.AssertSuccessStatusCode();

        var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Is.EquivalentTo(new[] { a.TraceId }));
    }

    [Test]
    public async Task GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandlerAndNestedHandler()
    {
        using var a = CreateActivity(nameof(GivenTraceIdInTraceParentWithActiveActivity_IdFromActivityIsObservedByHandlerAndNestedHandler));
        activity = a;

        using var msg = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new("/api/queries/testQueryWithNested", UriKind.Relative),
            Headers =
            {
                { HeaderNames.TraceParent, "00-80e1a2ed08e019fc1110464cfa66635c-7a085853722dc6d2-01" },
            },
        };

        var response = await HttpClient.SendAsync(msg);
        await response.AssertSuccessStatusCode();

        var receivedTraceIds = Resolve<TestObservations>().ReceivedTraceIds;

        Assert.That(receivedTraceIds, Has.Count.EqualTo(2));
        Assert.That(receivedTraceIds[0], Is.EqualTo(a.TraceId));
        Assert.That(receivedTraceIds[1], Is.EqualTo(a.TraceId));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        var applicationPartManager = new ApplicationPartManager();
        applicationPartManager.ApplicationParts.Add(new TestControllerApplicationPart());
        applicationPartManager.FeatureProviders.Add(new TestControllerFeatureProvider());

        _ = services.AddSingleton(applicationPartManager);

        _ = services.AddMvc().AddConquerorCQSHttpControllers();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddConquerorQueryHandler<TestQueryHandlerWithoutPayload>()
                    .AddConquerorQueryHandler<TestQueryWithNestedQueryHandler>()
                    .AddConquerorQueryHandler<NestedTestQueryHandler>()
                    .AddConquerorQueryHandler<TestPostQueryHandler>()
                    .AddConquerorQueryHandlerDelegate<TestDelegateQuery, TestDelegateQueryResponse>(async (_, p, _) =>
                    {
                        await Task.CompletedTask;

                        var testObservations = p.GetRequiredService<TestObservations>();
                        var conquerorContextAccessor = p.GetRequiredService<IConquerorContextAccessor>();

                        ObserveAndSetContextData(testObservations, conquerorContextAccessor);

                        return new();
                    })
                    .AddSingleton<TestObservations>();
    }

    protected override void Configure(IApplicationBuilder app)
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

        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    private IDisposableConquerorContext CreateConquerorContext()
    {
        return Resolve<IConquerorContextAccessor>().GetOrCreate();
    }

    private static void ObserveAndSetContextData(TestObservations testObservations, IConquerorContextAccessor conquerorContextAccessor)
    {
        testObservations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
        testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
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

    [HttpQuery]
    public sealed record TestQuery;

    public sealed record TestQueryResponse;

    [HttpQuery]
    public sealed record TestQueryWithoutPayload;

    [HttpQuery(UsePost = true)]
    public sealed record TestPostQuery;

    [HttpQuery]
    public sealed record TestQueryWithNestedQuery;

    [HttpQuery]
    public sealed record TestDelegateQuery;

    public sealed record TestDelegateQueryResponse;

    public sealed record NestedTestQuery;

    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;

        public TestQueryHandler(IConquerorContextAccessor conquerorContextAccessor,
                                TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);

            return Task.FromResult(new TestQueryResponse());
        }
    }

    public sealed class TestPostQueryHandler : IQueryHandler<TestPostQuery, TestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;

        public TestPostQueryHandler(IConquerorContextAccessor conquerorContextAccessor,
                                    TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TestQueryResponse> ExecuteQuery(TestPostQuery query, CancellationToken cancellationToken = default)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);

            return Task.FromResult(new TestQueryResponse());
        }
    }

    public sealed class TestQueryHandlerWithoutPayload : IQueryHandler<TestQueryWithoutPayload, TestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;

        public TestQueryHandlerWithoutPayload(IConquerorContextAccessor conquerorContextAccessor,
                                              TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TestQueryResponse> ExecuteQuery(TestQueryWithoutPayload query, CancellationToken cancellationToken = default)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);

            return Task.FromResult(new TestQueryResponse());
        }
    }

    public sealed class TestQueryWithNestedQueryHandler : IQueryHandler<TestQueryWithNestedQuery, TestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly IQueryHandler<NestedTestQuery, TestQueryResponse> nestedHandler;
        private readonly TestObservations testObservations;

        public TestQueryWithNestedQueryHandler(IConquerorContextAccessor conquerorContextAccessor,
                                               IQueryHandler<NestedTestQuery, TestQueryResponse> nestedHandler,
                                               TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
            this.nestedHandler = nestedHandler;
        }

        public Task<TestQueryResponse> ExecuteQuery(TestQueryWithNestedQuery query, CancellationToken cancellationToken = default)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);

            return nestedHandler.ExecuteQuery(new(), cancellationToken);
        }
    }

    public sealed class NestedTestQueryHandler : IQueryHandler<NestedTestQuery, TestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestObservations testObservations;

        public NestedTestQueryHandler(IConquerorContextAccessor conquerorContextAccessor,
                                      TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TestQueryResponse> ExecuteQuery(NestedTestQuery query, CancellationToken cancellationToken = default)
        {
            ObserveAndSetContextData(testObservations, conquerorContextAccessor);

            return Task.FromResult(new TestQueryResponse());
        }
    }

    public sealed class TestObservations
    {
        public List<string?> ReceivedQueryIds { get; } = new();

        public List<string?> ReceivedTraceIds { get; } = new();

        public bool ShouldAddUpstreamData { get; set; }

        public bool ShouldAddBidirectionalData { get; set; }

        public IConquerorContextData? ReceivedDownstreamContextData { get; set; }

        public IConquerorContextData? ReceivedBidirectionalContextData { get; set; }
    }

    [ApiController]
    private sealed class TestHttpQueryController : ControllerBase
    {
        private readonly IQueryHandler<TestQuery, TestQueryResponse> queryHandler;
        private readonly IQueryHandler<TestQueryWithoutPayload, TestQueryResponse> queryWithoutPayloadHandler;

        public TestHttpQueryController(IQueryHandler<TestQuery, TestQueryResponse> queryHandler, IQueryHandler<TestQueryWithoutPayload, TestQueryResponse> queryWithoutPayloadHandler)
        {
            this.queryHandler = queryHandler;
            this.queryWithoutPayloadHandler = queryWithoutPayloadHandler;
        }

        [HttpGet("/api/custom/queries/test")]
        public Task<TestQueryResponse> ExecuteTestQuery([FromQuery] TestQuery query, CancellationToken cancellationToken)
        {
            return queryHandler.ExecuteQuery(query, cancellationToken);
        }

        [HttpGet("/api/custom/queries/testQueryWithoutPayload")]
        public Task<TestQueryResponse> ExecuteTestQueryWithoutPayload(CancellationToken cancellationToken)
        {
            return queryWithoutPayloadHandler.ExecuteQuery(new(), cancellationToken);
        }
    }

    private sealed class TestControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
    {
        public override string Name => nameof(TestControllerApplicationPart);

        public IEnumerable<TypeInfo> Types { get; } = new[] { typeof(TestHttpQueryController).GetTypeInfo() };
    }

    private sealed class TestControllerFeatureProvider : ControllerFeatureProvider
    {
        protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(TestHttpQueryController);
    }

    private sealed class DisposableActivity : IDisposable
    {
        private readonly IReadOnlyCollection<IDisposable> disposables;

        public DisposableActivity(Activity activity, params IDisposable[] disposables)
        {
            Activity = activity;
            this.disposables = disposables;
        }

        public Activity Activity { get; }

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
