using System.Text.Json;

namespace Conqueror.CQS.Tests;

public sealed class ConquerorContextDataTests
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
            dataToSetCol.Add((TestKey, data.DataType == DataType.String ? stringValue + i : new TestDataEntry(i), data.DataSettingLocation));

            if (data.DataRemovalLocation is not null)
            {
                dataToRemoveCol.Add((TestKey, data.DataRemovalLocation));
            }
        }

        var services = new ServiceCollection();

        _ = services.AddSingleton(testDataInstructions)
                    .AddSingleton(testObservations)
                    .AddSingleton<NestedTestClass>()
                    .AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(async (_, p, _) =>
                    {
                        SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.HandlerPreNestedExecution);

                        await p.GetRequiredService<NestedTestClass>().Execute();

                        SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.HandlerPostNestedExecution);

                        return new();
                    }, pipeline =>
                    {
                        SetAndObserveContextData(pipeline.ConquerorContext, testDataInstructions, testObservations, Location.PipelineBuilder);

                        _ = pipeline.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(pipeline.ServiceProvider.GetRequiredService<TestDataInstructions>(),
                                                                                                     pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
                    })
                    .AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (_, p, _) =>
                    {
                        SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.HandlerPreNestedExecution);

                        await p.GetRequiredService<NestedTestClass>().Execute();

                        SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.HandlerPostNestedExecution);

                        return new();
                    }, pipeline =>
                    {
                        SetAndObserveContextData(pipeline.ConquerorContext, testDataInstructions, testObservations, Location.PipelineBuilder);

                        _ = pipeline.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(pipeline.ServiceProvider.GetRequiredService<TestDataInstructions>(),
                                                                                               pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
                    })
                    .AddConquerorCommandHandlerDelegate<NestedTestCommand, TestCommandResponse>((_, p, _) =>
                    {
                        SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.NestedCommandHandler);
                        return Task.FromResult<TestCommandResponse>(new());
                    })
                    .AddConquerorQueryHandlerDelegate<NestedTestQuery, TestQueryResponse>((_, p, _) =>
                    {
                        SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.NestedQueryHandler);
                        return Task.FromResult<TestQueryResponse>(new());
                    });

        await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        SetAndObserveContextData(conquerorContext, testDataInstructions, testObservations, Location.PreExecution);

        if (testCase.RequestType == RequestType.Command)
        {
            var handlerClient = serviceProvider.GetRequiredService<ICommandClientFactory>()
                                               .CreateCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b =>
                                               {
                                                   SetAndObserveContextData(b.ConquerorContext, testDataInstructions, testObservations, Location.TransportBuilder);
                                                   return b.UseInProcess();
                                               });

            _ = await handlerClient.WithPipeline(pipeline =>
            {
                SetAndObserveContextData(pipeline.ConquerorContext, testDataInstructions, testObservations, Location.ClientPipelineBuilder);
                _ = pipeline.Use(new TestClientCommandMiddleware<TestCommand, TestCommandResponse>(pipeline.ServiceProvider.GetRequiredService<TestDataInstructions>(),
                                                                                                   pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
            }).ExecuteCommand(new());
        }

        if (testCase.RequestType == RequestType.Query)
        {
            var handlerClient = serviceProvider.GetRequiredService<IQueryClientFactory>()
                                               .CreateQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b =>
                                               {
                                                   SetAndObserveContextData(b.ConquerorContext, testDataInstructions, testObservations, Location.TransportBuilder);
                                                   return b.UseInProcess();
                                               });

            _ = await handlerClient.WithPipeline(pipeline =>
            {
                SetAndObserveContextData(pipeline.ConquerorContext, testDataInstructions, testObservations, Location.ClientPipelineBuilder);
                _ = pipeline.Use(new TestClientQueryMiddleware<TestQuery, TestQueryResponse>(pipeline.ServiceProvider.GetRequiredService<TestDataInstructions>(),
                                                                                             pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
            }).ExecuteQuery(new());
        }

        SetAndObserveContextData(conquerorContext, testDataInstructions, testObservations, Location.PostExecution);

        var observedData = testCase.DataDirection switch
        {
            DataDirection.Downstream => testObservations.ObservedDownstreamData,
            DataDirection.Upstream => testObservations.ObservedUpstreamData,
            DataDirection.Bidirectional => testObservations.ObservedBidirectionalData,
            _ => throw new ArgumentOutOfRangeException(nameof(testCase.DataDirection)),
        };

        foreach (var (data, i) in testCase.TestData.Select((value, i) => (value, i)))
        {
            object value = data.DataType == DataType.String ? stringValue + i : new TestDataEntry(i);

            var errorMessage = $"test case:\n{JsonSerializer.Serialize(testCase, new JsonSerializerOptions { WriteIndented = true })}";

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
                Console.WriteLine(errorMessage);
                throw;
            }
        }
    }

    private static IEnumerable<ConquerorContextDataTestCase> GenerateTestCases()
    {
        foreach (var requestType in new[] { RequestType.Command, RequestType.Query })
        {
            foreach (var dataType in new[] { DataType.String, DataType.Object })
            {
                foreach (var testCaseData in GenerateDownstreamTestCaseData(dataType))
                {
                    yield return new(requestType, DataDirection.Downstream, testCaseData);
                }

                foreach (var testCaseData in GenerateUpstreamTestCaseData(dataType))
                {
                    yield return new(requestType, DataDirection.Upstream, testCaseData);
                }

                foreach (var testCaseData in GenerateBidirectionalTestCaseData(dataType))
                {
                    yield return new(requestType, DataDirection.Bidirectional, testCaseData);
                }
            }
        }
    }

    private static IEnumerable<List<ConquerorContextDataTestCaseData>> GenerateDownstreamTestCaseData(string dataType)
    {
        var executionOrder = ExecutionOrder.Order;
        var allLocations = executionOrder.Select(t => t.Location).ToList();

        // setting tests

        for (var i = 0; i < executionOrder.Length; i += 1)
        {
            var (contextDepth, depthInstance, location) = executionOrder[i];
            var whereDataShouldBeAccessible = executionOrder[i..].Where(t => t.ContextDepth > contextDepth
                                                                             || (t.ContextDepth == contextDepth && t.DepthInstance == depthInstance))
                                                                 .Select(t => t.Location)
                                                                 .ToList();

            yield return
            [
                new(dataType,
                    location,
                    null,
                    whereDataShouldBeAccessible,
                    allLocations.Except(whereDataShouldBeAccessible).ToList()),
            ];
        }

        // overwrite tests

        foreach (var overWriteDataType in new[] { DataType.String, DataType.Object })
        {
            for (var i = 0; i < executionOrder.Length - 1; i += 1)
            {
                var (initialContextDepth, initialDepthInstance, initialDataSettingLocation) = executionOrder[i];

                for (var j = i + 1; j < executionOrder.Length; j += 1)
                {
                    var (overwrittenContextDepth, overwrittenDepthInstance, overwrittenDataSettingLocation) = executionOrder[j];

                    var whereOverwrittenDataShouldBeAccessible = executionOrder[j..].Where(t => t.ContextDepth > overwrittenContextDepth
                                                                                                || (t.ContextDepth == overwrittenContextDepth && t.DepthInstance == overwrittenDepthInstance))
                                                                                    .Select(t => t.Location)
                                                                                    .ToList();

                    var whereInitialDataShouldBeAccessible = executionOrder[i..].Where(t => t.ContextDepth > initialContextDepth
                                                                                            || (t.ContextDepth == initialContextDepth && t.DepthInstance == initialDepthInstance))
                                                                                .Select(t => t.Location)
                                                                                .Except(whereOverwrittenDataShouldBeAccessible)
                                                                                .ToList();

                    yield return
                    [
                        new(dataType,
                            initialDataSettingLocation,
                            null,
                            whereInitialDataShouldBeAccessible,
                            allLocations.Except(whereInitialDataShouldBeAccessible).ToList()),

                        new(overWriteDataType,
                            overwrittenDataSettingLocation,
                            null,
                            whereOverwrittenDataShouldBeAccessible,
                            allLocations.Except(whereOverwrittenDataShouldBeAccessible).ToList()),
                    ];
                }
            }
        }

        // removal tests

        for (var i = 0; i < executionOrder.Length - 1; i += 1)
        {
            var (contextDepth, depthInstance, dataSettingLocation) = executionOrder[i];

            for (var j = i + 1; j < executionOrder.Length; j += 1)
            {
                var (removalContextDepth, removalDepthInstance, removalLocation) = executionOrder[j];

                var whereDataShouldBeRemoved = executionOrder[j..].Where(t => t.ContextDepth > removalContextDepth
                                                                              || (t.ContextDepth == removalContextDepth && t.DepthInstance == removalDepthInstance))
                                                                  .Select(t => t.Location)
                                                                  .ToList();

                var whereDataShouldBeAccessible = executionOrder[i..].Where(t => t.ContextDepth > contextDepth
                                                                                 || (t.ContextDepth == contextDepth && t.DepthInstance == depthInstance))
                                                                     .Select(t => t.Location)
                                                                     .Except(whereDataShouldBeRemoved)
                                                                     .ToList();

                yield return
                [
                    new(dataType,
                        dataSettingLocation,
                        removalLocation,
                        whereDataShouldBeAccessible,
                        allLocations.Except(whereDataShouldBeAccessible).ToList()),
                ];
            }
        }
    }

    private static IEnumerable<List<ConquerorContextDataTestCaseData>> GenerateUpstreamTestCaseData(string dataType)
    {
        var executionOrder = ExecutionOrder.Order;
        var allLocations = executionOrder.Select(t => t.Location).ToList();

        // setting tests

        for (var i = 0; i < executionOrder.Length; i += 1)
        {
            var (contextDepth, depthInstance, location) = executionOrder[i];
            var whereDataShouldBeAccessible = executionOrder[i..].Where(t => t.ContextDepth < contextDepth
                                                                             || (t.ContextDepth == contextDepth && t.DepthInstance == depthInstance))
                                                                 .Select(t => t.Location)
                                                                 .ToList();

            yield return
            [
                new(dataType,
                    location,
                    null,
                    whereDataShouldBeAccessible,
                    allLocations.Except(whereDataShouldBeAccessible).ToList()),
            ];
        }

        // overwrite tests

        foreach (var overWriteDataType in new[] { DataType.String, DataType.Object })
        {
            for (var i = 0; i < executionOrder.Length - 1; i += 1)
            {
                var (initialContextDepth, initialDepthInstance, initialDataSettingLocation) = executionOrder[i];

                for (var j = i + 1; j < executionOrder.Length; j += 1)
                {
                    var (overwrittenContextDepth, overwrittenDepthInstance, overwrittenDataSettingLocation) = executionOrder[j];

                    var whereOverwrittenDataShouldBeAccessible = executionOrder[j..].Where(t => t.ContextDepth < overwrittenContextDepth
                                                                                                || (t.ContextDepth == overwrittenContextDepth && t.DepthInstance == overwrittenDepthInstance))
                                                                                    .Select(t => t.Location)
                                                                                    .ToList();

                    var whereInitialDataShouldBeAccessible = executionOrder[i..].Where(t => t.ContextDepth < initialContextDepth
                                                                                            || (t.ContextDepth == initialContextDepth && t.DepthInstance == initialDepthInstance))
                                                                                .Select(t => t.Location)
                                                                                .Except(whereOverwrittenDataShouldBeAccessible)
                                                                                .ToList();

                    yield return
                    [
                        new(dataType,
                            initialDataSettingLocation,
                            null,
                            whereInitialDataShouldBeAccessible,
                            allLocations.Except(whereInitialDataShouldBeAccessible).ToList()),

                        new(overWriteDataType,
                            overwrittenDataSettingLocation,
                            null,
                            whereOverwrittenDataShouldBeAccessible,
                            allLocations.Except(whereOverwrittenDataShouldBeAccessible).ToList()),
                    ];
                }
            }
        }

        // removal tests

        for (var i = 0; i < executionOrder.Length - 1; i += 1)
        {
            var (settingContextDepth, depthInstance, dataSettingLocation) = executionOrder[i];

            for (var j = i + 1; j < executionOrder.Length; j += 1)
            {
                var (removalContextDepth, removalDepthInstance, removalLocation) = executionOrder[j];

                var whereDataShouldBeRemoved = executionOrder[j..].Where(t => (t.ContextDepth < removalContextDepth
                                                                               && (removalContextDepth < settingContextDepth
                                                                                   || (removalContextDepth == settingContextDepth && removalDepthInstance == depthInstance)))
                                                                              || (t.ContextDepth == removalContextDepth && t.DepthInstance == removalDepthInstance))
                                                                  .Select(t => t.Location)
                                                                  .ToList();

                var whereDataShouldBeAccessible = executionOrder[i..].Where(t => t.ContextDepth < settingContextDepth
                                                                                 || (t.ContextDepth == settingContextDepth && t.DepthInstance == depthInstance))
                                                                     .Select(t => t.Location)
                                                                     .Except(whereDataShouldBeRemoved)
                                                                     .ToList();

                yield return
                [
                    new(dataType,
                        dataSettingLocation,
                        removalLocation,
                        whereDataShouldBeAccessible,
                        allLocations.Except(whereDataShouldBeAccessible).ToList()),
                ];
            }
        }
    }

    private static IEnumerable<List<ConquerorContextDataTestCaseData>> GenerateBidirectionalTestCaseData(string dataType)
    {
        var executionOrder = ExecutionOrder.Order;
        var allLocations = executionOrder.Select(t => t.Location).ToList();

        // setting tests

        for (var i = 0; i < executionOrder.Length; i += 1)
        {
            var (_, _, location) = executionOrder[i];
            var whereDataShouldBeAccessible = executionOrder[i..].Select(t => t.Location).ToList();

            yield return
            [
                new(dataType,
                    location,
                    null,
                    whereDataShouldBeAccessible,
                    allLocations.Except(whereDataShouldBeAccessible).ToList()),
            ];
        }

        // overwrite tests

        foreach (var overWriteDataType in new[] { DataType.String, DataType.Object })
        {
            for (var i = 0; i < executionOrder.Length - 1; i += 1)
            {
                var (_, _, initialDataSettingLocation) = executionOrder[i];

                for (var j = i + 1; j < executionOrder.Length; j += 1)
                {
                    var (_, _, overwrittenDataSettingLocation) = executionOrder[j];

                    var whereOverwrittenDataShouldBeAccessible = executionOrder[j..].Select(t => t.Location).ToList();

                    var whereInitialDataShouldBeAccessible = executionOrder[i..].Select(t => t.Location)
                                                                                .Except(whereOverwrittenDataShouldBeAccessible)
                                                                                .ToList();

                    yield return
                    [
                        new(dataType,
                            initialDataSettingLocation,
                            null,
                            whereInitialDataShouldBeAccessible,
                            allLocations.Except(whereInitialDataShouldBeAccessible).ToList()),

                        new(overWriteDataType,
                            overwrittenDataSettingLocation,
                            null,
                            whereOverwrittenDataShouldBeAccessible,
                            allLocations.Except(whereOverwrittenDataShouldBeAccessible).ToList()),
                    ];
                }
            }
        }

        // removal tests

        for (var i = 0; i < executionOrder.Length - 1; i += 1)
        {
            var (_, _, dataSettingLocation) = executionOrder[i];

            for (var j = i + 1; j < executionOrder.Length; j += 1)
            {
                var (_, _, removalLocation) = executionOrder[j];

                var whereDataShouldBeRemoved = executionOrder[j..].Select(t => t.Location).ToList();

                var whereDataShouldBeAccessible = executionOrder[i..].Select(t => t.Location)
                                                                     .Except(whereDataShouldBeRemoved)
                                                                     .ToList();

                yield return
                [
                    new(dataType,
                        dataSettingLocation,
                        removalLocation,
                        whereDataShouldBeAccessible,
                        allLocations.Except(whereDataShouldBeAccessible).ToList()),
                ];
            }
        }
    }

    private static void SetAndObserveContextData(ConquerorContext ctx, TestDataInstructions testDataInstructions, TestObservations testObservations, string location)
    {
        foreach (var (key, value, _) in testDataInstructions.DownstreamDataToSet.Where(t => t.Location == location))
        {
            ctx.DownstreamContextData.Set(key, value);
        }

        foreach (var (key, _) in testDataInstructions.DownstreamDataToRemove.Where(t => t.Location == location))
        {
            _ = ctx.DownstreamContextData.Remove(key);
        }

        foreach (var (key, value, _) in testDataInstructions.UpstreamDataToSet.Where(t => t.Location == location))
        {
            ctx.UpstreamContextData.Set(key, value);
        }

        foreach (var (key, _) in testDataInstructions.UpstreamDataToRemove.Where(t => t.Location == location))
        {
            _ = ctx.UpstreamContextData.Remove(key);
        }

        foreach (var (key, value, _) in testDataInstructions.BidirectionalDataToSet.Where(t => t.Location == location))
        {
            ctx.ContextData.Set(key, value);
        }

        foreach (var (key, _) in testDataInstructions.BidirectionalDataToRemove.Where(t => t.Location == location))
        {
            _ = ctx.ContextData.Remove(key);
        }

        foreach (var (key, value, _) in ctx.DownstreamContextData)
        {
            testObservations.ObservedDownstreamData.Add((key, value, location));
        }

        if (ctx.DownstreamContextData.Get<object>(TestKey) is { } downstreamValue)
        {
            testObservations.ObservedDownstreamData.Add((TestKey, downstreamValue, location));
        }

        foreach (var (key, value, _) in ctx.UpstreamContextData)
        {
            testObservations.ObservedUpstreamData.Add((key, value, location));
        }

        if (ctx.UpstreamContextData.Get<object>(TestKey) is { } upstreamValue)
        {
            testObservations.ObservedUpstreamData.Add((TestKey, upstreamValue, location));
        }

        foreach (var (key, value, _) in ctx.ContextData)
        {
            testObservations.ObservedBidirectionalData.Add((key, value, location));
        }

        if (ctx.ContextData.Get<object>(TestKey) is { } bidirectionalValue)
        {
            testObservations.ObservedBidirectionalData.Add((TestKey, bidirectionalValue, location));
        }
    }

    [SuppressMessage("Critical Code Smell", "S3218:Inner class members should not shadow outer class \"static\" or type members", Justification = "The name makes sense and there is little risk of confusing a property and a class.")]
    public sealed record ConquerorContextDataTestCase(string RequestType, string DataDirection, List<ConquerorContextDataTestCaseData> TestData);

    [SuppressMessage("Critical Code Smell", "S3218:Inner class members should not shadow outer class \"static\" or type members", Justification = "The name makes sense and there is little risk of confusing a property and a class.")]
    public sealed record ConquerorContextDataTestCaseData(
        string DataType,
        string DataSettingLocation,
        string? DataRemovalLocation,
        IReadOnlyCollection<string> LocationsWhereDataShouldBeAccessible,
        IReadOnlyCollection<string> LocationsWhereDataShouldNotBeAccessible);

    private static class RequestType
    {
        public const string Command = nameof(Command);
        public const string Query = nameof(Query);
    }

    private static class DataDirection
    {
        public const string Downstream = nameof(Downstream);
        public const string Upstream = nameof(Upstream);
        public const string Bidirectional = nameof(Bidirectional);
    }

    private static class Location
    {
        public const string PreExecution = nameof(PreExecution);
        public const string PostExecution = nameof(PostExecution);
        public const string ClientPipelineBuilder = nameof(ClientPipelineBuilder);
        public const string TransportBuilder = nameof(TransportBuilder);
        public const string ClientMiddlewarePreExecution = nameof(ClientMiddlewarePreExecution);
        public const string ClientMiddlewarePostExecution = nameof(ClientMiddlewarePostExecution);
        public const string PipelineBuilder = nameof(PipelineBuilder);
        public const string MiddlewarePreExecution = nameof(MiddlewarePreExecution);
        public const string MiddlewarePostExecution = nameof(MiddlewarePostExecution);
        public const string HandlerPreNestedExecution = nameof(HandlerPreNestedExecution);
        public const string HandlerPostNestedExecution = nameof(HandlerPostNestedExecution);
        public const string NestedClassPreExecution = nameof(NestedClassPreExecution);
        public const string NestedClassPostExecution = nameof(NestedClassPostExecution);
        public const string NestedCommandHandler = nameof(NestedCommandHandler);
        public const string NestedQueryHandler = nameof(NestedQueryHandler);
    }

    private static class ExecutionOrder
    {
        public static (int ContextDepth, int DepthInstance, string Location)[] Order =>
        [
            (1, 1, Location.PreExecution),
            (2, 1, Location.TransportBuilder),
            (2, 1, Location.ClientPipelineBuilder),
            (2, 1, Location.ClientMiddlewarePreExecution),
            (3, 1, Location.PipelineBuilder),
            (3, 1, Location.MiddlewarePreExecution),
            (3, 1, Location.HandlerPreNestedExecution),
            (3, 1, Location.NestedClassPreExecution),
            (4, 1, Location.NestedCommandHandler),
            (4, 2, Location.NestedQueryHandler),
            (3, 1, Location.NestedClassPostExecution),
            (3, 1, Location.HandlerPostNestedExecution),
            (3, 1, Location.MiddlewarePostExecution),
            (2, 1, Location.ClientMiddlewarePostExecution),
            (1, 1, Location.PostExecution),
        ];
    }

    private static class DataType
    {
        public const string String = nameof(String);
        public const string Object = nameof(Object);
    }

    private sealed record TestDataEntry(int Value);

    private sealed record TestCommand;

    private sealed record TestCommandResponse;

    private sealed record NestedTestCommand;

    private sealed record TestQuery;

    private sealed record TestQueryResponse;

    private sealed record NestedTestQuery;

    private sealed class TestCommandMiddleware<TCommand, TResponse>(
        TestDataInstructions dataInstructions,
        TestObservations observations)
        : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();

            SetAndObserveContextData(ctx.ConquerorContext, dataInstructions, observations, Location.MiddlewarePreExecution);

            var response = await ctx.Next(ctx.Command, ctx.CancellationToken);

            SetAndObserveContextData(ctx.ConquerorContext, dataInstructions, observations, Location.MiddlewarePostExecution);

            return response;
        }
    }

    private sealed class TestClientCommandMiddleware<TCommand, TResponse>(
        TestDataInstructions dataInstructions,
        TestObservations observations)
        : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();

            SetAndObserveContextData(ctx.ConquerorContext, dataInstructions, observations, Location.ClientMiddlewarePreExecution);

            var response = await ctx.Next(ctx.Command, ctx.CancellationToken);

            SetAndObserveContextData(ctx.ConquerorContext, dataInstructions, observations, Location.ClientMiddlewarePostExecution);

            return response;
        }
    }

    private sealed class TestQueryMiddleware<TQuery, TResponse>(
        TestDataInstructions dataInstructions,
        TestObservations observations)
        : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
        {
            await Task.Yield();

            SetAndObserveContextData(ctx.ConquerorContext, dataInstructions, observations, Location.MiddlewarePreExecution);

            var response = await ctx.Next(ctx.Query, ctx.CancellationToken);

            SetAndObserveContextData(ctx.ConquerorContext, dataInstructions, observations, Location.MiddlewarePostExecution);

            return response;
        }
    }

    private sealed class TestClientQueryMiddleware<TQuery, TResponse>(
        TestDataInstructions dataInstructions,
        TestObservations observations)
        : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
        {
            await Task.Yield();

            SetAndObserveContextData(ctx.ConquerorContext, dataInstructions, observations, Location.ClientMiddlewarePreExecution);

            var response = await ctx.Next(ctx.Query, ctx.CancellationToken);

            SetAndObserveContextData(ctx.ConquerorContext, dataInstructions, observations, Location.ClientMiddlewarePostExecution);

            return response;
        }
    }

    private sealed class NestedTestClass(
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations,
        TestDataInstructions dataInstructions,
        ICommandHandler<NestedTestCommand, TestCommandResponse> nestedCommandHandler,
        IQueryHandler<NestedTestQuery, TestQueryResponse> nestedQueryHandler)
    {
        public async Task Execute()
        {
            SetAndObserveContextData(conquerorContextAccessor.ConquerorContext!, dataInstructions, observations, Location.NestedClassPreExecution);

            _ = await nestedCommandHandler.ExecuteCommand(new());
            _ = await nestedQueryHandler.ExecuteQuery(new());

            SetAndObserveContextData(conquerorContextAccessor.ConquerorContext!, dataInstructions, observations, Location.NestedClassPostExecution);
        }
    }

    private sealed class TestDataInstructions
    {
        public List<(string Key, object Value, string Location)> DownstreamDataToSet { get; } = [];

        public List<(string Key, string Location)> DownstreamDataToRemove { get; } = [];

        public List<(string Key, object Value, string Location)> UpstreamDataToSet { get; } = [];

        public List<(string Key, string Location)> UpstreamDataToRemove { get; } = [];

        public List<(string Key, object Value, string Location)> BidirectionalDataToSet { get; } = [];

        public List<(string Key, string Location)> BidirectionalDataToRemove { get; } = [];
    }

    private sealed class TestObservations
    {
        public List<(string Key, object Value, string Location)> ObservedDownstreamData { get; } = [];

        public List<(string Key, object Value, string Location)> ObservedUpstreamData { get; } = [];

        public List<(string Key, object Value, string Location)> ObservedBidirectionalData { get; } = [];
    }
}
