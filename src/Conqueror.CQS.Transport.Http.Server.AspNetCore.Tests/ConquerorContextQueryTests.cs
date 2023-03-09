using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using Conqueror.CQS.Transport.Http.Common;
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
    };

    private static readonly Dictionary<string, string> InProcessContextData = new()
    {
        { "key7", "value1" },
        { "key8", "value2" },
    };

    private DisposableActivity? activity;

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenContextData_DataIsReturnedInHeader(string path, string? postContent = null)
    {
        Resolve<TestObservations>().ShouldAddUpstreamData = true;

        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        var response = postContent != null ? await HttpClient.PostAsync(path, content) : await HttpClient.GetAsync(path);
        await response.AssertSuccessStatusCode();

        var exists = response.Headers.TryGetValues(HttpConstants.ConquerorContextHeaderName, out var values);

        Assert.That(exists, Is.True);

        var receivedData = ContextValueFormatter.Parse(values!);

        CollectionAssert.AreEquivalent(ContextData, receivedData.Select(t => new KeyValuePair<string, string>(t.Key, t.Value)));
    }

    [TestCase("/api/queries/test?payload=10")]
    [TestCase("/api/queries/testQueryWithoutPayload")]
    [TestCase("/api/queries/testPost", "{\"payload\":10}")]
    [TestCase("/api/queries/testDelegate?payload=10")]
    [TestCase("/api/custom/queries/test?payload=10")]
    [TestCase("/api/custom/queries/testQueryWithoutPayload")]
    public async Task GivenConquerorContextRequestHeader_DataIsReceivedByHandler(string path, string? postContent = null)
    {
        using var content = new StringContent(postContent ?? string.Empty, null, MediaTypeNames.Application.Json);
        using var msg = new HttpRequestMessage
        {
            Method = postContent != null ? HttpMethod.Post : HttpMethod.Get,
            RequestUri = new(path, UriKind.Relative),
            Headers = { { HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(ContextData) } },
            Content = postContent != null ? content : null,
        };

        var response = await HttpClient.SendAsync(msg);
        await response.AssertSuccessStatusCode();

        var receivedContextData = Resolve<TestObservations>().ReceivedContextData;

        CollectionAssert.AreEquivalent(ContextData, receivedContextData?.Select(t => new KeyValuePair<string, string>(t.Key, (string)t.Value)) ?? Array.Empty<KeyValuePair<string, string>>());
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

        CollectionAssert.AreEquivalent(new[] { testTraceId }, receivedTraceIds);
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

        CollectionAssert.AreEquivalent(new[] { a.TraceId }, receivedTraceIds);
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

                        testObservations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
                        testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
                        testObservations.ReceivedContextData = conquerorContextAccessor.ConquerorContext?.DownstreamContextData;

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
        _ = app.UseEndpoints(b => b.MapControllers());
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
            testObservations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
            testObservations.ReceivedContextData = conquerorContextAccessor.ConquerorContext?.DownstreamContextData;

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
            testObservations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
            testObservations.ReceivedContextData = conquerorContextAccessor.ConquerorContext?.DownstreamContextData;

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
            testObservations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
            testObservations.ReceivedContextData = conquerorContextAccessor.ConquerorContext?.DownstreamContextData;

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
            testObservations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
            testObservations.ReceivedContextData = conquerorContextAccessor.ConquerorContext?.DownstreamContextData;

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
            testObservations.ReceivedQueryIds.Add(conquerorContextAccessor.ConquerorContext?.GetQueryId());
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
            testObservations.ReceivedContextData = conquerorContextAccessor.ConquerorContext?.DownstreamContextData;

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

            return Task.FromResult(new TestQueryResponse());
        }
    }

    public sealed class TestObservations
    {
        public List<string?> ReceivedQueryIds { get; } = new();

        public List<string?> ReceivedTraceIds { get; } = new();

        public bool ShouldAddUpstreamData { get; set; }

        public IConquerorContextData? ReceivedContextData { get; set; }
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
