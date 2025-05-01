using System.Text.Json;
using static Conqueror.Tests.ContextDataTestHelper;

namespace Conqueror.Tests.Signalling;

public sealed partial class SignalContextDataTests
{
    private const string TestKey = "TestKey";

    [Test]
    [TestCaseSource(nameof(GenerateTestCases))]
    [SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "parameter name makes sense here")]
    public async Task GivenDataSetup_WhenExecutingHandler_DataIsCorrectlyAvailable(ConquerorContextDataTestCase testCase)
    {
        const string stringValue = "TestValue";

        var testDataInstructions = new TestDataInstructions();
        var testObservations = new TestObservations();

        var dataToSetCol = testCase.DataDirection switch
        {
            DataDirection.Downstream => testDataInstructions.DownstreamDataToSet,
            DataDirection.Upstream => testDataInstructions.UpstreamDataToSet,
            DataDirection.Bidirectional => testDataInstructions.BidirectionalDataToSet,
            _ => throw new ArgumentOutOfRangeException(nameof(testCase.DataDirection)),
        };

        var dataToRemoveCol = testCase.DataDirection switch
        {
            DataDirection.Downstream => testDataInstructions.DownstreamDataToRemove,
            DataDirection.Upstream => testDataInstructions.UpstreamDataToRemove,
            DataDirection.Bidirectional => testDataInstructions.BidirectionalDataToRemove,
            _ => throw new ArgumentOutOfRangeException(nameof(testCase.DataDirection)),
        };

        foreach (var (data, i) in testCase.TestData.Select((value, i) => (value, i)))
        {
            dataToSetCol.Add((TestKey, data.DataType == ContextDataType.String ? stringValue + i : new TestDataEntry(i), data.DataSettingLocation));

            if (data.DataRemovalLocation is not null)
            {
                dataToRemoveCol.Add((TestKey, data.DataRemovalLocation));
            }
        }

        var services = new ServiceCollection();

        _ = services.AddSingleton(testDataInstructions)
                    .AddSingleton(testObservations)
                    .AddSingleton<NestedTestClass>()

                    // first handler
                    .AddSignalHandlerDelegate(
                        TestSignal.T,
                        async (_, p, _) =>
                        {
                            SetAndObserveContextData(
                                p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!,
                                testDataInstructions,
                                testObservations,
                                Location.Handler1PreNestedExecution);

                            await p.GetRequiredService<NestedTestClass>().Execute();

                            SetAndObserveContextData(
                                p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!,
                                testDataInstructions,
                                testObservations,
                                Location.Handler1PostNestedExecution);
                        },
                        pipeline =>
                        {
                            SetAndObserveContextData(
                                pipeline.ConquerorContext,
                                testDataInstructions,
                                testObservations,
                                Location.Handler1PipelineBuilder);

                            _ = pipeline.Use(
                                new TestSignalMiddleware<TestSignal>(
                                    pipeline.ServiceProvider.GetRequiredService<TestDataInstructions>(),
                                    pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
                        })

                    // second handler
                    .AddSignalHandlerDelegate(
                        TestSignal.T,
                        (_, p, _) =>
                        {
                            SetAndObserveContextData(
                                p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!,
                                testDataInstructions,
                                testObservations,
                                Location.Handler2Execution);
                        },
                        pipeline =>
                        {
                            SetAndObserveContextData(
                                pipeline.ConquerorContext,
                                testDataInstructions,
                                testObservations,
                                Location.Handler2PipelineBuilder);

                            _ = pipeline.Use(
                                new TestSignalMiddleware2<TestSignal>(
                                    pipeline.ServiceProvider.GetRequiredService<TestDataInstructions>(),
                                    pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
                        })
                    .AddSignalHandlerDelegate(
                        NestedTestSignal.T,
                        (_, p, _) =>
                        {
                            SetAndObserveContextData(
                                p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!,
                                testDataInstructions,
                                testObservations,
                                Location.NestedSignalHandler);
                        });

        await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        SetAndObserveContextData(
            conquerorContext,
            testDataInstructions,
            testObservations,
            Location.PreExecution);

        var handlerClient = serviceProvider.GetRequiredService<ISignalPublishers>()
                                           .For(TestSignal.T)
                                           .WithTransport(b =>
                                           {
                                               SetAndObserveContextData(
                                                   b.ConquerorContext,
                                                   testDataInstructions,
                                                   testObservations,
                                                   Location.TransportBuilder);

                                               return b.UseInProcessWithSequentialBroadcastingStrategy();
                                           });

        await handlerClient.WithPipeline(pipeline =>
                           {
                               SetAndObserveContextData(
                                   pipeline.ConquerorContext,
                                   testDataInstructions,
                                   testObservations,
                                   Location.PublisherPipelineBuilder);
                               _ = pipeline.Use(
                                   new TestPublisherSignalMiddleware<TestSignal>(
                                       pipeline.ServiceProvider.GetRequiredService<TestDataInstructions>(),
                                       pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
                           })
                           .Handle(new());

        SetAndObserveContextData(
            conquerorContext,
            testDataInstructions,
            testObservations,
            Location.PostExecution);

        var observedData = testCase.DataDirection switch
        {
            DataDirection.Downstream => testObservations.ObservedDownstreamData,
            DataDirection.Upstream => testObservations.ObservedUpstreamData,
            DataDirection.Bidirectional => testObservations.ObservedBidirectionalData,
            _ => throw new ArgumentOutOfRangeException(nameof(testCase.DataDirection)),
        };

        foreach (var (data, i) in testCase.TestData.Select((value, i) => (value, i)))
        {
            object value = data.DataType == ContextDataType.String ? stringValue + i : new TestDataEntry(i);

            var errorSignal = $"test case:\n{JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}";

            try
            {
                Assert.Multiple(() =>
                {
                    foreach (var location in data.LocationsWhereDataShouldBeAccessible)
                    {
                        // we assert on count equal to 2, because observed data should be added twice (once by enumeration and once by direct access)
                        Assert.That(observedData, Has.Exactly(2).Matches<(string Key, object Value, string Location)>(d => d.Value.Equals(value) && d.Location == location), () => $"location: {location}, value: {value}, observedData: [{string.Join(",", observedData)}]");
                    }

                    foreach (var location in data.LocationsWhereDataShouldNotBeAccessible)
                    {
                        Assert.That(observedData, Has.Exactly(0).Matches<(string Key, object Value, string Location)>(d => d.Value.Equals(value) && d.Location == location), () => $"location: {location}, value: {value}, observedData: [{string.Join(",", observedData)}]");
                    }
                });
            }
            catch (MultipleAssertException)
            {
                Console.WriteLine(errorSignal);

                throw;
            }
        }
    }

    private static IEnumerable<TestCaseData> GenerateTestCases() => GenerateContextDataTestCases(ExecutionOrder.Order);

    private static class Location
    {
        public const string PreExecution = nameof(PreExecution);
        public const string PostExecution = nameof(PostExecution);
        public const string PublisherPipelineBuilder = nameof(PublisherPipelineBuilder);
        public const string TransportBuilder = nameof(TransportBuilder);
        public const string PublisherMiddlewarePreExecution = nameof(PublisherMiddlewarePreExecution);
        public const string PublisherMiddlewarePostExecution = nameof(PublisherMiddlewarePostExecution);
        public const string Handler1PipelineBuilder = nameof(Handler1PipelineBuilder);
        public const string Handler1MiddlewarePreExecution = nameof(Handler1MiddlewarePreExecution);
        public const string Handler1MiddlewarePostExecution = nameof(Handler1MiddlewarePostExecution);
        public const string Handler1PreNestedExecution = nameof(Handler1PreNestedExecution);
        public const string Handler1PostNestedExecution = nameof(Handler1PostNestedExecution);
        public const string Handler2PipelineBuilder = nameof(Handler2PipelineBuilder);
        public const string Handler2MiddlewarePreExecution = nameof(Handler2MiddlewarePreExecution);
        public const string Handler2MiddlewarePostExecution = nameof(Handler2MiddlewarePostExecution);
        public const string Handler2Execution = nameof(Handler2Execution);
        public const string NestedClassPreExecution = nameof(NestedClassPreExecution);
        public const string NestedClassPostExecution = nameof(NestedClassPostExecution);
        public const string NestedSignalHandler = nameof(NestedSignalHandler);
    }

    private static class ExecutionOrder
    {
        public static ExecutionOrderItem[] Order =>
        [
            new(1, 1, Location.PreExecution),
            new(2, 1, Location.TransportBuilder),
            new(2, 1, Location.PublisherPipelineBuilder),
            new(2, 1, Location.PublisherMiddlewarePreExecution),
            new(3, 1, Location.Handler1PipelineBuilder),
            new(3, 1, Location.Handler1MiddlewarePreExecution),
            new(3, 1, Location.Handler1PreNestedExecution),
            new(3, 1, Location.NestedClassPreExecution),
            new(4, 1, Location.NestedSignalHandler),
            new(3, 1, Location.NestedClassPostExecution),
            new(3, 1, Location.Handler1PostNestedExecution),
            new(3, 1, Location.Handler1MiddlewarePostExecution),
            new(3, 2, Location.Handler2PipelineBuilder),
            new(3, 2, Location.Handler2MiddlewarePreExecution),
            new(3, 2, Location.Handler2Execution),
            new(3, 2, Location.Handler2MiddlewarePostExecution),
            new(2, 1, Location.PublisherMiddlewarePostExecution),
            new(1, 1, Location.PostExecution),
        ];
    }

    private sealed record TestDataEntry(int Value);

    [Signal]
    private sealed partial record TestSignal;

    [Signal]
    private sealed partial record NestedTestSignal;

    private sealed class TestSignalMiddleware<TSignal>(
        TestDataInstructions dataInstructions,
        TestObservations observations)
        : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
        {
            await Task.Yield();

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.Handler1MiddlewarePreExecution);

            await ctx.Next(ctx.Signal, ctx.CancellationToken);

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.Handler1MiddlewarePostExecution);
        }
    }

    private sealed class TestSignalMiddleware2<TSignal>(
        TestDataInstructions dataInstructions,
        TestObservations observations)
        : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
        {
            await Task.Yield();

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.Handler2MiddlewarePreExecution);

            await ctx.Next(ctx.Signal, ctx.CancellationToken);

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.Handler2MiddlewarePostExecution);
        }
    }

    private sealed class TestPublisherSignalMiddleware<TSignal>(
        TestDataInstructions dataInstructions,
        TestObservations observations)
        : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
        {
            await Task.Yield();

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.PublisherMiddlewarePreExecution);

            await ctx.Next(ctx.Signal, ctx.CancellationToken);

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.PublisherMiddlewarePostExecution);
        }
    }

    private sealed class NestedTestClass(
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations,
        TestDataInstructions dataInstructions,
        ISignalPublishers signalPublishers)
    {
        public async Task Execute()
        {
            SetAndObserveContextData(
                conquerorContextAccessor.ConquerorContext!,
                dataInstructions,
                observations,
                Location.NestedClassPreExecution);

            await signalPublishers.For(NestedTestSignal.T).Handle(new());

            SetAndObserveContextData(
                conquerorContextAccessor.ConquerorContext!,
                dataInstructions,
                observations,
                Location.NestedClassPostExecution);
        }
    }
}
