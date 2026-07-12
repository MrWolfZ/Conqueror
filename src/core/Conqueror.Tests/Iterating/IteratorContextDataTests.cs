namespace Conqueror.Tests.Iterating;

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using static ContextDataTestHelper;

internal sealed partial class IteratorContextDataTests
{
    private const string TestKey = "TestKey";

    [Test]
    [TestCaseSource(nameof(GenerateTestCases))]
    [SuppressMessage(
        "Usage",
        "CA2208:Instantiate argument exceptions correctly",
        Justification = "parameter name makes sense here"
    )]
    public async Task GivenDataSetup_WhenExecutingHandler_DataIsCorrectlyAvailable(
        ConquerorContextDataTestCase testCase
    )
    {
        const string stringValue = "TestValue";

        var testDataInstructions = new TestDataInstructions();
        var testObservations = new TestObservations();

        var dataToSetCol = testCase.DataDirection switch
        {
            DataDirection.Downstream => testDataInstructions.DownstreamDataToSet,
            DataDirection.Upstream => testDataInstructions.UpstreamDataToSet,
            DataDirection.Bidirectional => testDataInstructions.BidirectionalDataToSet,
            _ => throw new ArgumentOutOfRangeException(nameof(testCase), testCase.DataDirection),
        };

        var dataToRemoveCol = testCase.DataDirection switch
        {
            DataDirection.Downstream => testDataInstructions.DownstreamDataToRemove,
            DataDirection.Upstream => testDataInstructions.UpstreamDataToRemove,
            DataDirection.Bidirectional => testDataInstructions.BidirectionalDataToRemove,
            _ => throw new ArgumentOutOfRangeException(nameof(testCase), testCase.DataDirection),
        };

        foreach (var (data, i) in testCase.TestData.Select((value, i) => (value, i)))
        {
            dataToSetCol.Add(
                (
                    TestKey,
                    string.Equals(data.DataType, ContextDataType.String, StringComparison.OrdinalIgnoreCase)
                        ? $"{stringValue}{i}"
                        : new TestDataEntry(i),
                    data.DataSettingLocation
                )
            );

            if (data.DataRemovalLocation is not null)
            {
                dataToRemoveCol.Add((TestKey, data.DataRemovalLocation));
            }
        }

        var services = new ServiceCollection();

        _ = services
            .AddSingleton(testDataInstructions)
            .AddSingleton(testObservations)
            .AddSingleton<NestedTestClass>()
            .AddIteratorHandler<TestIteratorHandler>()
            .AddIteratorHandler<NestedTestIteratorHandler>();

        await using var serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true }
        );

        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        SetAndObserveContextData(conquerorContext, testDataInstructions, testObservations, Location.PreExecution);

        var handlerClient = serviceProvider
            .GetRequiredService<IIterators>()
            .For(TestIterator.T)
            .WithTransport(b => b.UseInProcess());

        _ = await ConsumeAll(
            handlerClient
                .WithPipeline(pipeline =>
                {
                    _ = pipeline.Use(
                        new TestClientIteratorMiddleware<TestIterator, TestIteratorItem>(
                            pipeline.ServiceProvider.GetRequiredService<TestDataInstructions>(),
                            pipeline.ServiceProvider.GetRequiredService<TestObservations>()
                        )
                    );
                })
                .Handle(new(), CancellationToken.None)
        );

        SetAndObserveContextData(conquerorContext, testDataInstructions, testObservations, Location.PostExecution);

        var observedData = testCase.DataDirection switch
        {
            DataDirection.Downstream => testObservations.ObservedDownstreamData,
            DataDirection.Upstream => testObservations.ObservedUpstreamData,
            DataDirection.Bidirectional => testObservations.ObservedBidirectionalData,
            _ => throw new ArgumentOutOfRangeException(nameof(testCase), testCase.DataDirection),
        };

        foreach (var (data, i) in testCase.TestData.Select((value, i) => (value, i)))
        {
            object value = string.Equals(data.DataType, ContextDataType.String, StringComparison.OrdinalIgnoreCase)
                ? $"{stringValue}{i}"
                : new TestDataEntry(i);

            var errorMessage =
                $"test case:\n{JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}";

            try
            {
                Assert.Multiple(() =>
                {
                    foreach (var location in data.LocationsWhereDataShouldBeAccessible)
                    {
                        // we assert on count equal to 2, because observed data should be added twice (once by enumeration and once by direct access)
                        Assert.That(
                            observedData,
                            Has.Exactly(expectedCount: 2)
                                .Matches<(string Key, object Value, string Location)>(d =>
                                    d.Value.Equals(value)
                                    && string.Equals(d.Location, location, StringComparison.OrdinalIgnoreCase)
                                ),
                            () =>
                                $"location: {location}, value: {value}, observedData: [{string.Join(',', observedData)}]"
                        );
                    }

                    foreach (var location in data.LocationsWhereDataShouldNotBeAccessible)
                    {
                        Assert.That(
                            observedData,
                            Has.Exactly(expectedCount: 0)
                                .Matches<(string Key, object Value, string Location)>(d =>
                                    d.Value.Equals(value)
                                    && string.Equals(d.Location, location, StringComparison.OrdinalIgnoreCase)
                                ),
                            () =>
                                $"location: {location}, value: {value}, observedData: [{string.Join(',', observedData)}]"
                        );
                    }
                });
            }
            catch (MultipleAssertException)
            {
                Console.WriteLine(errorMessage);

                throw;
            }
        }
    }

    [Test]
    public async Task GivenIterator_WhenPerformingMultiTurnIteration_ThenContextDataIsPropagatedPerCallToMoveNextAndPerItem()
    {
        const string testDownStreamKey = "TestDownStreamKey";
        const string testUpStreamKey = "TestUpStreamKey";
        const string stringValue = "TestValue";

        var services = new ServiceCollection();

        static async IAsyncEnumerable<TestIteratorItem> HandlerFn(
            TestIterator _,
            IServiceProvider p,
            [EnumeratorCancellation] CancellationToken cancellationToken
        )
        {
            await Task.Yield();

            cancellationToken.ThrowIfCancellationRequested();

            var conquerorContext = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!;

            Assert.That(
                conquerorContext.InProcessData.Get<string>(
                    testDownStreamKey,
                    ConquerorContextDataFlowDirection.Downstream
                ),
                Is.EqualTo(stringValue + 1)
            );

            conquerorContext.InProcessData.Set(
                testUpStreamKey,
                stringValue + 1,
                ConquerorContextDataFlowDirection.Upstream
            );

            yield return new TestIteratorItem();

            Assert.That(
                conquerorContext.InProcessData.Get<string>(
                    testDownStreamKey,
                    ConquerorContextDataFlowDirection.Downstream
                ),
                Is.EqualTo(stringValue + 2)
            );

            conquerorContext.InProcessData.Set(
                testUpStreamKey,
                stringValue + 2,
                ConquerorContextDataFlowDirection.Upstream
            );

            yield return new TestIteratorItem();
        }

        _ = services.AddIteratorHandlerDelegate(TestIterator.T, HandlerFn);

        await using var serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true }
        );

        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        var handlerClient = serviceProvider
            .GetRequiredService<IIterators>()
            .For(TestIterator.T)
            .WithTransport(b => b.UseInProcess());

        var enumerable = handlerClient.Handle(new(), CancellationToken.None);

        await using var enumerator = enumerable.GetAsyncEnumerator(CancellationToken.None);

        conquerorContext.InProcessData.Set(
            testDownStreamKey,
            stringValue + 1,
            ConquerorContextDataFlowDirection.Downstream
        );

        Assert.That(await enumerator.MoveNextAsync(), Is.True);

        Assert.That(
            conquerorContext.InProcessData.Get<string>(testUpStreamKey, ConquerorContextDataFlowDirection.Upstream),
            Is.EqualTo(stringValue + 1)
        );

        conquerorContext.InProcessData.Set(
            testDownStreamKey,
            stringValue + 2,
            ConquerorContextDataFlowDirection.Downstream
        );

        Assert.That(await enumerator.MoveNextAsync(), Is.True);

        Assert.That(
            conquerorContext.InProcessData.Get<string>(testUpStreamKey, ConquerorContextDataFlowDirection.Upstream),
            Is.EqualTo(stringValue + 2)
        );

        Assert.That(await enumerator.MoveNextAsync(), Is.False);
    }

    private static async Task<List<T>> ConsumeAll<T>(IAsyncEnumerable<T> enumerable)
    {
        var items = new List<T>();

        await foreach (var item in enumerable.WithCancellation(CancellationToken.None).ConfigureAwait(false))
        {
            items.Add(item);
        }

        return items;
    }

    private static IEnumerable<TestCaseData> GenerateTestCases() => GenerateContextDataTestCases(ExecutionOrder.Order);

    private static class Location
    {
        public const string PreExecution = nameof(PreExecution);
        public const string PostExecution = nameof(PostExecution);
        public const string ClientMiddlewarePreExecution = nameof(ClientMiddlewarePreExecution);
        public const string ClientMiddlewarePostExecution = nameof(ClientMiddlewarePostExecution);
        public const string HandlerMiddlewarePreExecution = nameof(HandlerMiddlewarePreExecution);
        public const string HandlerMiddlewarePostExecution = nameof(HandlerMiddlewarePostExecution);
        public const string HandlerPreNestedExecution = nameof(HandlerPreNestedExecution);
        public const string HandlerPostNestedExecution = nameof(HandlerPostNestedExecution);
        public const string HandlerPostFirstItemYielded = nameof(HandlerPostFirstItemYielded);
        public const string NestedClassPreExecution = nameof(NestedClassPreExecution);
        public const string NestedClassPostExecution = nameof(NestedClassPostExecution);
        public const string NestedIteratorHandler = nameof(NestedIteratorHandler);
    }

    private static class ExecutionOrder
    {
        public static ExecutionOrderItem[] Order =>
        [
            new(ContextDepth: 1, DepthInstance: 1, Location.PreExecution),
            new(ContextDepth: 2, DepthInstance: 1, Location.ClientMiddlewarePreExecution),
            new(ContextDepth: 3, DepthInstance: 1, Location.HandlerMiddlewarePreExecution),
            new(ContextDepth: 3, DepthInstance: 1, Location.HandlerPreNestedExecution),
            new(ContextDepth: 3, DepthInstance: 1, Location.NestedClassPreExecution),
            new(ContextDepth: 4, DepthInstance: 1, Location.NestedIteratorHandler),
            new(ContextDepth: 3, DepthInstance: 1, Location.NestedClassPostExecution),
            new(ContextDepth: 3, DepthInstance: 1, Location.HandlerPostNestedExecution),
            new(ContextDepth: 3, DepthInstance: 1, Location.HandlerPostFirstItemYielded),
            new(ContextDepth: 3, DepthInstance: 1, Location.HandlerMiddlewarePostExecution),
            new(ContextDepth: 2, DepthInstance: 1, Location.ClientMiddlewarePostExecution),
            new(ContextDepth: 1, DepthInstance: 1, Location.PostExecution),
        ];
    }

    private sealed record TestDataEntry(int Value);

    [Iterator<TestIteratorItem>]
    private sealed partial record TestIterator;

    private sealed record TestIteratorItem;

    [Iterator<TestIteratorItem>]
    private sealed partial record NestedTestIterator;

    private sealed partial class TestIteratorHandler(
        IConquerorContextAccessor conquerorContextAccessor,
        TestDataInstructions testDataInstructions,
        TestObservations testObservations,
        NestedTestClass nestedTestClass
    ) : TestIterator.IHandler
    {
        public static void ConfigurePipeline(TestIterator.IPipeline pipeline) =>
            pipeline.Use(
                new TestHandlerIteratorMiddleware<TestIterator, TestIteratorItem>(
                    pipeline.ServiceProvider.GetRequiredService<TestDataInstructions>(),
                    pipeline.ServiceProvider.GetRequiredService<TestObservations>()
                )
            );

        public async IAsyncEnumerable<TestIteratorItem> Handle(
            TestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            SetAndObserveContextData(
                conquerorContextAccessor.ConquerorContext!,
                testDataInstructions,
                testObservations,
                Location.HandlerPreNestedExecution
            );

            await nestedTestClass.Execute();

            SetAndObserveContextData(
                conquerorContextAccessor.ConquerorContext!,
                testDataInstructions,
                testObservations,
                Location.HandlerPostNestedExecution
            );

            yield return new();

            SetAndObserveContextData(
                conquerorContextAccessor.ConquerorContext!,
                testDataInstructions,
                testObservations,
                Location.HandlerPostFirstItemYielded
            );

            yield return new();
        }
    }

    private sealed partial class NestedTestIteratorHandler(
        IConquerorContextAccessor conquerorContextAccessor,
        TestDataInstructions testDataInstructions,
        TestObservations testObservations
    ) : NestedTestIterator.IHandler
    {
        public async IAsyncEnumerable<TestIteratorItem> Handle(
            NestedTestIterator iterator,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            SetAndObserveContextData(
                conquerorContextAccessor.ConquerorContext!,
                testDataInstructions,
                testObservations,
                Location.NestedIteratorHandler
            );

            yield return new();
        }
    }

    private sealed class TestHandlerIteratorMiddleware<TIterator, TItem>(
        TestDataInstructions dataInstructions,
        TestObservations observations
    ) : IIteratorMiddleware<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public async IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx)
        {
            await Task.Yield();

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.HandlerMiddlewarePreExecution
            );

            await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
            {
                yield return item;
            }

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.HandlerMiddlewarePostExecution
            );
        }
    }

    private sealed class TestClientIteratorMiddleware<TIterator, TItem>(
        TestDataInstructions dataInstructions,
        TestObservations observations
    ) : IIteratorMiddleware<TIterator, TItem>
        where TIterator : class, IIterator<TIterator, TItem>
    {
        public async IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx)
        {
            await Task.Yield();

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.ClientMiddlewarePreExecution
            );

            await foreach (var item in ctx.Next(ctx.Iterator, ctx.CancellationToken))
            {
                yield return item;
            }

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.ClientMiddlewarePostExecution
            );
        }
    }

    private sealed class NestedTestClass(
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations,
        TestDataInstructions dataInstructions,
        IIterators iterators
    )
    {
        public async Task Execute()
        {
            SetAndObserveContextData(
                conquerorContextAccessor.ConquerorContext!,
                dataInstructions,
                observations,
                Location.NestedClassPreExecution
            );

            _ = await ConsumeItems(iterators.For(NestedTestIterator.T).Handle(new(), CancellationToken.None));

            SetAndObserveContextData(
                conquerorContextAccessor.ConquerorContext!,
                dataInstructions,
                observations,
                Location.NestedClassPostExecution
            );
        }

        private static async Task<List<T>> ConsumeItems<T>(IAsyncEnumerable<T> enumerable)
        {
            var items = new List<T>();

            await foreach (var item in enumerable.WithCancellation(CancellationToken.None))
            {
                items.Add(item);
            }

            return items;
        }
    }

    private static class AsyncEnumerableHelper
    {
        public static async IAsyncEnumerable<TItem> Of<TItem>(params TItem[] items)
        {
            await Task.Yield();

            foreach (var item in items)
            {
                yield return item;
            }
        }
    }
}
