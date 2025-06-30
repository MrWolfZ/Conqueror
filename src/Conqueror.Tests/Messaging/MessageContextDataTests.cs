using System.Text.Json;
using static Conqueror.Tests.ContextDataTestHelper;

namespace Conqueror.Tests.Messaging;

public sealed partial class MessageContextDataTests
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
                    .AddMessageHandlerDelegate(
                        TestMessage.T,
                        async (_, p, _) =>
                        {
                            SetAndObserveContextData(
                                p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!,
                                testDataInstructions,
                                testObservations,
                                Location.HandlerPreNestedExecution);

                            await p.GetRequiredService<NestedTestClass>().Execute();

                            SetAndObserveContextData(
                                p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!,
                                testDataInstructions,
                                testObservations,
                                Location.HandlerPostNestedExecution);

                            return new();
                        },
                        pipeline =>
                        {
                            _ = pipeline.Use(
                                new TestHandlerMessageMiddleware<TestMessage, TestMessageResponse>(
                                    pipeline.ServiceProvider.GetRequiredService<TestDataInstructions>(),
                                    pipeline.ServiceProvider.GetRequiredService<TestObservations>()));
                        })
                    .AddMessageHandlerDelegate(
                        NestedTestMessage.T,
                        (_, p, _) =>
                        {
                            SetAndObserveContextData(
                                p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!,
                                testDataInstructions,
                                testObservations,
                                Location.NestedMessageHandler);

                            return Task.FromResult<TestMessageResponse>(new());
                        });

        await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        SetAndObserveContextData(
            conquerorContext,
            testDataInstructions,
            testObservations,
            Location.PreExecution);

        var handlerClient = serviceProvider.GetRequiredService<IMessageSenders>()
                                           .For(TestMessage.T)
                                           .WithTransport(b =>
                                           {
                                               SetAndObserveContextData(
                                                   b.ConquerorContext,
                                                   testDataInstructions,
                                                   testObservations,
                                                   Location.TransportBuilder);

                                               return b.UseInProcess();
                                           });

        _ = await handlerClient.WithPipeline(pipeline =>
                               {
                                   _ = pipeline.Use(
                                       new TestSenderMessageMiddleware<TestMessage, TestMessageResponse>(
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

    private static IEnumerable<TestCaseData> GenerateTestCases() => GenerateContextDataTestCases(ExecutionOrder.Order);

    private static class Location
    {
        public const string PreExecution = nameof(PreExecution);
        public const string PostExecution = nameof(PostExecution);
        public const string TransportBuilder = nameof(TransportBuilder);
        public const string SenderMiddlewarePreExecution = nameof(SenderMiddlewarePreExecution);
        public const string SenderMiddlewarePostExecution = nameof(SenderMiddlewarePostExecution);
        public const string HandlerMiddlewarePreExecution = nameof(HandlerMiddlewarePreExecution);
        public const string HandlerMiddlewarePostExecution = nameof(HandlerMiddlewarePostExecution);
        public const string HandlerPreNestedExecution = nameof(HandlerPreNestedExecution);
        public const string HandlerPostNestedExecution = nameof(HandlerPostNestedExecution);
        public const string NestedClassPreExecution = nameof(NestedClassPreExecution);
        public const string NestedClassPostExecution = nameof(NestedClassPostExecution);
        public const string NestedMessageHandler = nameof(NestedMessageHandler);
    }

    private static class ExecutionOrder
    {
        public static ExecutionOrderItem[] Order =>
        [
            new(1, 1, Location.PreExecution),
            new(2, 1, Location.TransportBuilder),
            new(2, 1, Location.SenderMiddlewarePreExecution),
            new(3, 1, Location.HandlerMiddlewarePreExecution),
            new(3, 1, Location.HandlerPreNestedExecution),
            new(3, 1, Location.NestedClassPreExecution),
            new(4, 1, Location.NestedMessageHandler),
            new(3, 1, Location.NestedClassPostExecution),
            new(3, 1, Location.HandlerPostNestedExecution),
            new(3, 1, Location.HandlerMiddlewarePostExecution),
            new(2, 1, Location.SenderMiddlewarePostExecution),
            new(1, 1, Location.PostExecution),
        ];
    }

    private sealed record TestDataEntry(int Value);

    [Message<TestMessageResponse>]
    private sealed partial record TestMessage;

    private sealed record TestMessageResponse;

    [Message<TestMessageResponse>]
    private sealed partial record NestedTestMessage;

    private sealed class TestHandlerMessageMiddleware<TMessage, TResponse>(
        TestDataInstructions dataInstructions,
        TestObservations observations)
        : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            await Task.Yield();

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.HandlerMiddlewarePreExecution);

            var response = await ctx.Next(ctx.Message, ctx.CancellationToken);

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.HandlerMiddlewarePostExecution);

            return response;
        }
    }

    private sealed class TestSenderMessageMiddleware<TMessage, TResponse>(
        TestDataInstructions dataInstructions,
        TestObservations observations)
        : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            await Task.Yield();

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.SenderMiddlewarePreExecution);

            var response = await ctx.Next(ctx.Message, ctx.CancellationToken);

            SetAndObserveContextData(
                ctx.ConquerorContext,
                dataInstructions,
                observations,
                Location.SenderMiddlewarePostExecution);

            return response;
        }
    }

    private sealed class NestedTestClass(
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations,
        TestDataInstructions dataInstructions,
        IMessageSenders messageSenders)
    {
        public async Task Execute()
        {
            SetAndObserveContextData(
                conquerorContextAccessor.ConquerorContext!,
                dataInstructions,
                observations,
                Location.NestedClassPreExecution);

            _ = await messageSenders.For(NestedTestMessage.T).Handle(new());

            SetAndObserveContextData(
                conquerorContextAccessor.ConquerorContext!,
                dataInstructions,
                observations,
                Location.NestedClassPostExecution);
        }
    }
}
