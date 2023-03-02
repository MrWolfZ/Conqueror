using System.Diagnostics;
using Microsoft.AspNetCore.Builder;

namespace Conqueror.CQS.Transport.Http.Client.Tests;

[TestFixture]
[NonParallelizable]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
public class ConquerorContextQueryTests : TestBase
{
    private static readonly Dictionary<string, string> ContextItems = new()
    {
        { "key1", "value1" },
        { "key2", "value2" },
        { "keyWith,Comma", "value" },
        { "key4", "valueWith,Comma" },
        { "keyWith=Equals", "value" },
        { "key6", "valueWith=Equals" },
    };

    [Test]
    public async Task GivenManuallyCreatedContextOnClientAndContextItemsInHandler_ItemsAreReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddItems = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ContextItems, context.Items);
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientAndContextItemsInPostHandler_ItemsAreReturnedInClientContext()
    {
        Resolve<TestObservations>().ShouldAddItems = true;

        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ContextItems, context.Items);
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInHandler()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
        context.AddOrReplaceItems(ContextItems);

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

        CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInHandlerAcrossMultipleInvocations()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
        context.Items.Add(ContextItems.First());

        var observations = Resolve<TestObservations>();

        observations.ShouldAddItems = true;

        var allReceivedKeys = new List<string>();

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
        observations.ReceivedContextItems.Clear();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
        observations.ReceivedContextItems.Clear();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
        observations.ReceivedContextItems.Clear();

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextItems.Count * 2 + 1));
    }

    [Test]
    public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInPostHandler()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
        context.AddOrReplaceItems(ContextItems);

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

        CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInPostHandlerAcrossMultipleInvocations()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
        context.Items.Add(ContextItems.First());

        var observations = Resolve<TestObservations>();

        observations.ShouldAddItems = true;

        var allReceivedKeys = new List<string>();

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
        observations.ReceivedContextItems.Clear();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
        observations.ReceivedContextItems.Clear();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
        observations.ReceivedContextItems.Clear();

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextItems.Count * 2 + 1));
    }

    [Test]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1407:Arithmetic expressions should declare precedence", Justification = "conflicts with formatting rules")]
    public async Task GivenManuallyCreatedContextOnClientWithItems_ContextIsReceivedInDifferentHandlersAcrossMultipleInvocations()
    {
        using var context = ResolveOnClient<IConquerorContextAccessor>().GetOrCreate();
        context.Items.Add(ContextItems.First());

        var observations = Resolve<TestObservations>();

        observations.ShouldAddItems = true;

        var allReceivedKeys = new List<string>();

        var handler1 = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();
        var handler2 = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler1.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
        observations.ReceivedContextItems.Clear();

        _ = await handler2.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
        observations.ReceivedContextItems.Clear();

        _ = await handler1.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        allReceivedKeys.AddRange(observations.ReceivedContextItems.Keys);
        observations.ReceivedContextItems.Clear();

        Assert.That(allReceivedKeys, Has.Count.EqualTo(ContextItems.Count * 2 + 1));
    }

    [Test]
    public async Task GivenContextItemsInHandler_ContextIsReceivedInOuterHandler()
    {
        Resolve<TestObservations>().ShouldAddItems = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestQuery, OuterTestQueryResponse>>();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var receivedContextItems = ResolveOnClient<TestObservations>().ReceivedOuterContextItems;

        CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
    }

    [Test]
    public async Task GivenContextItemsInPostHandler_ContextIsReceivedInOuterHandler()
    {
        Resolve<TestObservations>().ShouldAddItems = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestPostQuery, OuterTestQueryResponse>>();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var receivedContextItems = ResolveOnClient<TestObservations>().ReceivedOuterContextItems;

        CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
    }

    [Test]
    public async Task GivenContextItemsInOuterHandler_ContextIsReceivedInHandler()
    {
        ResolveOnClient<TestObservations>().ShouldAddOuterItems = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestQuery, OuterTestQueryResponse>>();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

        CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
    }

    [Test]
    public async Task GivenContextItemsInOuterHandler_ContextIsReceivedInPostHandler()
    {
        ResolveOnClient<TestObservations>().ShouldAddOuterItems = true;

        var handler = ResolveOnClient<IQueryHandler<OuterTestPostQuery, OuterTestQueryResponse>>();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);

        var receivedContextItems = Resolve<TestObservations>().ReceivedContextItems;

        CollectionAssert.AreEquivalent(ContextItems, receivedContextItems);
    }

    [Test]
    public async Task GivenQuery_SameQueryIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedQueryIds, Resolve<TestObservations>().ReceivedQueryIds);
    }

    [Test]
    public async Task GivenPostQuery_SameQueryIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedQueryIds, Resolve<TestObservations>().ReceivedQueryIds);
    }

    [Test]
    public async Task GivenQueryWithoutActiveClientSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
    }

    [Test]
    public async Task GivenPostQueryWithoutActiveClientSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
    }

    [Test]
    public async Task GivenQueryWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler()
    {
        using var activity = StartActivity(nameof(GivenQueryWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler));

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
        Assert.That(Resolve<TestObservations>().ReceivedTraceIds.FirstOrDefault(), Is.EqualTo(activity.TraceId));
    }

    [Test]
    public async Task GivenPostQueryWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler()
    {
        using var activity = StartActivity(nameof(GivenPostQueryWithActiveClientSideActivity_ActivityTraceIdIsObservedInTransportClientAndHandler));

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
        Assert.That(Resolve<TestObservations>().ReceivedTraceIds.FirstOrDefault(), Is.EqualTo(activity.TraceId));
    }

    [Test]
    public async Task GivenQueryWithoutActiveClientSideActivityWithActiveServerSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        using var listener = StartActivityListener("Microsoft.AspNetCore");

        var handler = ResolveOnClient<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
    }

    [Test]
    public async Task GivenPostQueryWithoutActiveClientSideActivityWithActiveServerSideActivity_SameTraceIdIsObservedInTransportClientAndHandler()
    {
        using var listener = StartActivityListener("Microsoft.AspNetCore");

        var handler = ResolveOnClient<IQueryHandler<TestPostQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new() { Payload = 10 }, CancellationToken.None);

        CollectionAssert.AreEquivalent(ResolveOnClient<TestObservations>().ReceivedTraceIds, Resolve<TestObservations>().ReceivedTraceIds);
    }

    protected override void ConfigureServerServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorCQSHttpControllers();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddConquerorQueryHandler<TestPostQueryHandler>()
                    .AddSingleton<TestObservations>();
    }

    protected override void ConfigureClientServices(IServiceCollection services)
    {
        _ = services.AddConquerorCQSHttpClientServices(o =>
        {
            _ = o.UseHttpClient(HttpClient);

            o.JsonSerializerOptions = new()
            {
                PropertyNameCaseInsensitive = true,
            };
        });

        var baseAddress = new Uri("http://localhost");

        _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b => new WrapperQueryTransportClient(b.UseHttp(baseAddress),
                                                                                                                               b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>(),
                                                                                                                               b.ServiceProvider.GetRequiredService<IQueryContextAccessor>(),
                                                                                                                               b.ServiceProvider.GetRequiredService<TestObservations>()))
                    .AddConquerorQueryClient<IQueryHandler<TestPostQuery, TestQueryResponse>>(b => new WrapperQueryTransportClient(b.UseHttp(baseAddress),
                                                                                                                                   b.ServiceProvider.GetRequiredService<IConquerorContextAccessor>(),
                                                                                                                                   b.ServiceProvider.GetRequiredService<IQueryContextAccessor>(),
                                                                                                                                   b.ServiceProvider.GetRequiredService<TestObservations>()));

        _ = services.AddConquerorQueryHandler<OuterTestQueryHandler>()
                    .AddConquerorQueryHandler<OuterTestPostQueryHandler>()
                    .AddSingleton<TestObservations>();
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.Use(async (_, next) =>
        {
            // prevent leaking of client-side activity to server
            Activity.Current = null;

            await next();
        });

        _ = app.UseRouting();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    private static DisposableActivity StartActivity(string name)
    {
        var activitySource = new ActivitySource(name);

        var activityListener = StartActivityListener();

        var activity = activitySource.StartActivity()!;
        return new(activity.TraceId.ToString(), activitySource, activityListener, activity);
    }

    private static IDisposable StartActivityListener(string? activityName = null)
    {
        var activityListener = new ActivityListener
        {
            ShouldListenTo = activity => activityName == null || activity.Name == activityName,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(activityListener);

        return activityListener;
    }

    [HttpQuery]
    public sealed record TestQuery
    {
        public int Payload { get; init; }
    }

    public sealed record TestQueryResponse
    {
        public int Payload { get; init; }
    }

    [HttpQuery(UsePost = true)]
    public sealed record TestPostQuery
    {
        public int Payload { get; init; }
    }

    public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly IQueryContextAccessor queryContextAccessor;
        private readonly TestObservations testObservations;

        public TestQueryHandler(IConquerorContextAccessor conquerorContextAccessor, IQueryContextAccessor queryContextAccessor, TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.queryContextAccessor = queryContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            testObservations.ReceivedQueryIds.Add(queryContextAccessor.QueryContext?.QueryId);
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
            testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

            if (testObservations.ShouldAddItems)
            {
                conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
            }

            return Task.FromResult(new TestQueryResponse());
        }
    }

    public sealed class TestPostQueryHandler : IQueryHandler<TestPostQuery, TestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly IQueryContextAccessor queryContextAccessor;
        private readonly TestObservations testObservations;

        public TestPostQueryHandler(IConquerorContextAccessor conquerorContextAccessor, IQueryContextAccessor queryContextAccessor, TestObservations testObservations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.queryContextAccessor = queryContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TestQueryResponse> ExecuteQuery(TestPostQuery query, CancellationToken cancellationToken = default)
        {
            testObservations.ReceivedQueryIds.Add(queryContextAccessor.QueryContext?.QueryId);
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);
            testObservations.ReceivedContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);

            if (testObservations.ShouldAddItems)
            {
                conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
            }

            return Task.FromResult(new TestQueryResponse());
        }
    }

    public sealed record OuterTestQuery;

    public sealed record OuterTestQueryResponse;

    public sealed class OuterTestQueryHandler : IQueryHandler<OuterTestQuery, OuterTestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly IQueryHandler<TestQuery, TestQueryResponse> nestedHandler;
        private readonly IQueryContextAccessor queryContextAccessor;
        private readonly TestObservations testObservations;

        public OuterTestQueryHandler(IConquerorContextAccessor conquerorContextAccessor,
                                     IQueryContextAccessor queryContextAccessor,
                                     TestObservations testObservations,
                                     IQueryHandler<TestQuery, TestQueryResponse> nestedHandler)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.queryContextAccessor = queryContextAccessor;
            this.testObservations = testObservations;
            this.nestedHandler = nestedHandler;
        }

        public async Task<OuterTestQueryResponse> ExecuteQuery(OuterTestQuery query, CancellationToken cancellationToken = default)
        {
            testObservations.ReceivedQueryIds.Add(queryContextAccessor.QueryContext?.QueryId);
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

            if (testObservations.ShouldAddOuterItems)
            {
                conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
            }

            _ = await nestedHandler.ExecuteQuery(new(), cancellationToken);
            testObservations.ReceivedOuterContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);
            return new();
        }
    }

    public sealed record OuterTestPostQuery;

    public sealed class OuterTestPostQueryHandler : IQueryHandler<OuterTestPostQuery, OuterTestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly IQueryHandler<TestPostQuery, TestQueryResponse> nestedHandler;
        private readonly IQueryContextAccessor queryContextAccessor;
        private readonly TestObservations testObservations;

        public OuterTestPostQueryHandler(IConquerorContextAccessor conquerorContextAccessor,
                                         IQueryContextAccessor queryContextAccessor,
                                         TestObservations testObservations,
                                         IQueryHandler<TestPostQuery, TestQueryResponse> nestedHandler)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.queryContextAccessor = queryContextAccessor;
            this.testObservations = testObservations;
            this.nestedHandler = nestedHandler;
        }

        public async Task<OuterTestQueryResponse> ExecuteQuery(OuterTestPostQuery query, CancellationToken cancellationToken = default)
        {
            testObservations.ReceivedQueryIds.Add(queryContextAccessor.QueryContext?.QueryId);
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

            if (testObservations.ShouldAddOuterItems)
            {
                conquerorContextAccessor.ConquerorContext?.AddOrReplaceItems(ContextItems);
            }

            _ = await nestedHandler.ExecuteQuery(new(), cancellationToken);
            testObservations.ReceivedOuterContextItems.AddOrReplaceRange(conquerorContextAccessor.ConquerorContext!.Items);
            return new();
        }
    }

    public sealed class TestObservations
    {
        public List<string?> ReceivedQueryIds { get; } = new();

        public List<string?> ReceivedTraceIds { get; } = new();

        public bool ShouldAddItems { get; set; }

        public bool ShouldAddOuterItems { get; set; }

        public IDictionary<string, string> ReceivedContextItems { get; } = new Dictionary<string, string>();

        public IDictionary<string, string> ReceivedOuterContextItems { get; } = new Dictionary<string, string>();
    }

    private sealed class WrapperQueryTransportClient : IQueryTransportClient
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly IQueryContextAccessor queryContextAccessor;
        private readonly TestObservations testObservations;
        private readonly IQueryTransportClient wrapped;

        public WrapperQueryTransportClient(IQueryTransportClient wrapped,
                                           IConquerorContextAccessor conquerorContextAccessor,
                                           IQueryContextAccessor queryContextAccessor,
                                           TestObservations testObservations)
        {
            this.wrapped = wrapped;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.queryContextAccessor = queryContextAccessor;
            this.testObservations = testObservations;
        }

        public Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
            where TQuery : class
        {
            testObservations.ReceivedQueryIds.Add(queryContextAccessor.QueryContext?.QueryId);
            testObservations.ReceivedTraceIds.Add(conquerorContextAccessor.ConquerorContext?.TraceId);

            return wrapped.ExecuteQuery<TQuery, TResponse>(query, cancellationToken);
        }
    }

    private sealed class DisposableActivity : IDisposable
    {
        private readonly IReadOnlyCollection<IDisposable> disposables;

        public DisposableActivity(string traceId, params IDisposable[] disposables)
        {
            TraceId = traceId;
            this.disposables = disposables;
        }

        public string TraceId { get; }

        public void Dispose()
        {
            foreach (var disposable in disposables.Reverse())
            {
                disposable.Dispose();
            }
        }
    }
}
