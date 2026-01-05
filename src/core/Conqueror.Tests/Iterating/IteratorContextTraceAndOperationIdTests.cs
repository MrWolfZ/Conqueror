namespace Conqueror.Tests.Iterating;

using System.Diagnostics;
using System.Runtime.CompilerServices;

public sealed partial class IteratorContextTraceAndOperationIdTests
{
    private static int TestCaseCounter;

    [Test]
    [Combinatorial]
    public async Task GivenSetup_WhenExecutingHandler_OperationIdsAreCorrectlyAvailable(
        [Values(arg1: true, arg2: false)] bool hasCustomTraceId,
        [Values(arg1: true, arg2: false)] bool hasActivity,
        [Values(arg1: true, arg2: false)] bool sendNestedWithDifferentTransport
    )
    {
        var customTraceId = Guid.NewGuid().ToString();

        string? traceIdFromExecution = null;
        string? traceIdFromHandler = null;
        string? iteratorIdFromHandler = null;
        string? traceIdFromNestedIteratorHandler = null;
        string? iteratorIdFromNestedIteratorHandler = null;

        var services = new ServiceCollection();

        _ = services
            .AddIteratorHandlerDelegate(TestIterator.T, HandleTestIterator)
            .AddIteratorHandlerDelegate(NestedTestIterator.T, HandleNestedTestIterator);

        await using var serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true }
        );

        ConquerorContext? conquerorContext = null;

        if (hasCustomTraceId)
        {
            conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();
            conquerorContext.TraceId = customTraceId;
        }

        using var d = conquerorContext;

        var testCaseIdx = Interlocked.Increment(ref TestCaseCounter);
        using var activity = hasActivity
            ? StartActivity($"{nameof(IteratorContextTraceAndOperationIdTests)}{testCaseIdx}")
            : null;

        var handlerClient = serviceProvider
            .GetRequiredService<IIterators>()
            .For(TestIterator.T)
            .WithPipeline(p => p.Use(ExecuteMiddleware))
            .WithTransport(b => b.UseInProcess());

        await foreach (var item in handlerClient.Handle(new(), CancellationToken.None))
        {
            // Consume the iterator
            _ = item;
        }

        var expectedTraceId = (hasCustomTraceId, hasActivity) switch
        {
            (true, _) => customTraceId,
            (false, true) => activity!.TraceId,
            (false, false) => traceIdFromExecution,
        };

        Assert.Multiple(() =>
        {
            Assert.That(traceIdFromHandler, Is.EqualTo(expectedTraceId));
            Assert.That(traceIdFromNestedIteratorHandler, Is.EqualTo(expectedTraceId));

            Assert.That(iteratorIdFromHandler, Is.Not.Null);
            Assert.That(iteratorIdFromNestedIteratorHandler, Is.Not.EqualTo(iteratorIdFromHandler));
        });

        async IAsyncEnumerable<int> HandleTestIterator(
            TestIterator iterator,
            IServiceProvider p,
            [EnumeratorCancellation] CancellationToken ct
        )
        {
            _ = iterator;
            await Task.Yield();
            traceIdFromHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.TraceId;
            iteratorIdFromHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.IteratorId;

            var handler = p.GetRequiredService<IIterators>().For(NestedTestIterator.T);

            if (sendNestedWithDifferentTransport)
            {
                handler = handler.WithTransport(b => new TestIteratorClient<NestedTestIterator, int>(b.UseInProcess()));
            }

            await foreach (var item in handler.Handle(new(), ct))
            {
                // Consume the nested iterator
                _ = item;
            }

            yield return 42;
        }

        async IAsyncEnumerable<int> HandleNestedTestIterator(
            NestedTestIterator iterator,
            IServiceProvider p,
            [EnumeratorCancellation] CancellationToken ct
        )
        {
            _ = iterator;
            _ = ct;
            await Task.Yield();
            traceIdFromNestedIteratorHandler =
                p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.TraceId;
            iteratorIdFromNestedIteratorHandler =
                p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.IteratorId;

            yield return 42;
        }

        async IAsyncEnumerable<int> ExecuteMiddleware(IteratorMiddlewareContext<TestIterator, int> ctx)
        {
            traceIdFromExecution = ctx.ConquerorContext.TraceId;

            await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private static DisposableActivity StartActivity(string name)
    {
        var activitySource = new ActivitySource(name);

        var activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(activityListener);

        var activity = activitySource.StartActivity()!;

        return new DisposableActivity(activity.TraceId.ToString(), activitySource, activityListener, activity);
    }

    private sealed class DisposableActivity(string traceId, params IDisposable[] disposables) : IDisposable
    {
        private readonly IReadOnlyCollection<IDisposable> disposables = disposables;

        public string TraceId { get; } = traceId;

        public void Dispose()
        {
            foreach (var disposable in disposables.Reverse())
            {
                disposable.Dispose();
            }
        }
    }

    [Iterator<int>]
    private sealed partial record TestIterator;

    [Iterator<int>]
    private sealed partial record NestedTestIterator;

    private sealed class TestIteratorClient<TIterator, TItem>(IIteratorClient<TIterator, TItem> wrapped)
        : IIteratorClient<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public string TransportTypeName => "test";

        public IAsyncEnumerable<TItem> Execute(
            TIterator iterator,
            IServiceProvider serviceProvider,
            ConquerorContext conquerorContext,
            CancellationToken cancellationToken
        ) => wrapped.Execute(iterator, serviceProvider, conquerorContext, cancellationToken);
    }
}
