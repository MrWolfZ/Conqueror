using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Conqueror.Streaming.Tests;

public sealed class ConquerorContextDataTests
{
    private const string TestKey = "TestKey";

    [Test]
    [TestCaseSource(nameof(GenerateTestCases))]
    [SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "parameter name makes sense here")]
    public async Task GivenDataSetup_WhenExecutingProducer_DataIsCorrectlyAvailable(ConquerorContextDataTestCase testCase)
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
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>(Producer,
                                                                                        pipeline =>
                                                                                        {
                                                                                            SetAndObserveContextData(pipeline.ServiceProvider.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.PipelineBuilder);

                                                                                            _ = pipeline.Use<TestStreamProducerMiddleware>();
                                                                                        })
                    .AddConquerorStreamProducerDelegate<NestedTestStreamingRequest, TestItem>((_, p, _) =>
                    {
                        SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.NestedStreamProducer);
                        return AsyncEnumerableHelper.Of(new TestItem());
                    });

        async IAsyncEnumerable<TestItem> Producer(TestStreamingRequest _, IServiceProvider p, [EnumeratorCancellation] CancellationToken _2)
        {
            SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.ProducerPreNestedExecution);

            await p.GetRequiredService<NestedTestClass>().Execute();

            SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.ProducerPostNestedExecution);

            yield return new();
            yield return new();
            yield return new();
        }

        await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        SetAndObserveContextData(conquerorContext, testDataInstructions, testObservations, Location.PreExecution);

        _ = await serviceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>().ExecuteRequest(new()).Drain();

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
        foreach (var dataType in new[] { DataType.String, DataType.Object })
        {
            foreach (var testCaseData in GenerateDownstreamTestCaseData(dataType))
            {
                yield return new(DataDirection.Downstream, testCaseData);
            }

            foreach (var testCaseData in GenerateUpstreamTestCaseData(dataType))
            {
                yield return new(DataDirection.Upstream, testCaseData);
            }

            foreach (var testCaseData in GenerateBidirectionalTestCaseData(dataType))
            {
                yield return new(DataDirection.Bidirectional, testCaseData);
            }
        }
    }

    private static IEnumerable<List<ConquerorContextDataTestCaseData>> GenerateDownstreamTestCaseData(string dataType)
    {
        yield return
        [
            new(dataType,
                Location.PreExecution,
                null,
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                Array.Empty<string>()),

        ];

        yield return
        [
            new(dataType,
                Location.PipelineBuilder,
                null,
                [
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PostExecution,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.MiddlewarePreExecution,
                null,
                [
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.MiddlewarePostExecution,
                null,
                [
                    Location.MiddlewarePostExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.ProducerPreNestedExecution,
                null,
                [
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.ProducerPostNestedExecution,
                null,
                [
                    Location.MiddlewarePostExecution,
                    Location.ProducerPostNestedExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.NestedClassPreExecution,
                null,
                [
                    Location.MiddlewarePostExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.NestedClassPostExecution,
                null,
                [
                    Location.MiddlewarePostExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPostExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.NestedStreamProducer,
                null,
                [
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                ]),
        ];

        // overwrite tests

        foreach (var overWriteDataType in new[] { DataType.String, DataType.Object })
        {
            yield return
            [
                new(dataType,
                    Location.PreExecution,
                    null,
                    [
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.PipelineBuilder,
                    ],
                    [
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.ProducerPreNestedExecution,
                        Location.ProducerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedStreamProducer,
                    ]),

                new(overWriteDataType,
                    Location.MiddlewarePreExecution,
                    null,
                    [
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.ProducerPreNestedExecution,
                        Location.ProducerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedStreamProducer,
                    ],
                    [
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.PipelineBuilder,
                    ]),
            ];

            yield return
            [
                new(dataType,
                    Location.PreExecution,
                    null,
                    [
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.PipelineBuilder,
                        Location.MiddlewarePreExecution,
                    ],
                    [
                        Location.MiddlewarePostExecution,
                        Location.ProducerPreNestedExecution,
                        Location.ProducerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedStreamProducer,
                    ]),

                new(overWriteDataType,
                    Location.ProducerPreNestedExecution,
                    null,
                    [
                        Location.MiddlewarePostExecution,
                        Location.ProducerPreNestedExecution,
                        Location.ProducerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedStreamProducer,
                    ],
                    [
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.PipelineBuilder,
                        Location.MiddlewarePreExecution,
                    ]),
            ];
        }

        // removal tests

        yield return
        [
            new(dataType,
                Location.PreExecution,
                Location.MiddlewarePreExecution,
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                ],
                [
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.PreExecution,
                Location.ProducerPreNestedExecution,
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                ],
                [
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ]),
        ];
    }

    private static IEnumerable<List<ConquerorContextDataTestCaseData>> GenerateUpstreamTestCaseData(string dataType)
    {
        yield return
        [
            new(dataType,
                Location.NestedStreamProducer,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                    Location.NestedClassPreExecution,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.NestedClassPreExecution,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.NestedClassPostExecution,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPostExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.ProducerPreNestedExecution,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.ProducerPostNestedExecution,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPostNestedExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.MiddlewarePreExecution,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.MiddlewarePostExecution,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        // overwrite tests

        foreach (var overWriteDataType in new[] { DataType.String, DataType.Object })
        {
            yield return
            [
                new(dataType,
                    Location.NestedStreamProducer,
                    null,
                    [
                        Location.ProducerPostNestedExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedStreamProducer,
                    ],
                    [
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.PipelineBuilder,
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.ProducerPreNestedExecution,
                        Location.NestedClassPreExecution,
                    ]),

                new(overWriteDataType,
                    Location.MiddlewarePostExecution,
                    null,
                    [
                        Location.PostExecution,
                        Location.MiddlewarePostExecution,
                    ],
                    [
                        Location.PreExecution,
                        Location.PipelineBuilder,
                        Location.MiddlewarePreExecution,
                        Location.ProducerPreNestedExecution,
                        Location.ProducerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedStreamProducer,
                    ]),
            ];

            yield return
            [
                new(dataType,
                    Location.NestedClassPreExecution,
                    null,
                    [
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                    ],
                    [
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.PipelineBuilder,
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.ProducerPreNestedExecution,
                        Location.ProducerPostNestedExecution,
                        Location.NestedStreamProducer,
                    ]),

                new(overWriteDataType,
                    Location.ProducerPostNestedExecution,
                    null,
                    [
                        Location.PostExecution,
                        Location.MiddlewarePostExecution,
                        Location.ProducerPostNestedExecution,
                    ],
                    [
                        Location.PreExecution,
                        Location.PipelineBuilder,
                        Location.MiddlewarePreExecution,
                        Location.ProducerPreNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedStreamProducer,
                    ]),
            ];

            yield return
            [
                new(dataType,
                    Location.PreExecution,
                    null,
                    [
                        Location.PreExecution,
                    ],
                    [
                        Location.PostExecution,
                        Location.PipelineBuilder,
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.ProducerPreNestedExecution,
                        Location.ProducerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedStreamProducer,
                    ]),

                new(overWriteDataType,
                    Location.ProducerPreNestedExecution,
                    null,
                    [
                        Location.PostExecution,
                        Location.MiddlewarePostExecution,
                        Location.ProducerPreNestedExecution,
                        Location.ProducerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                    ],
                    [
                        Location.PreExecution,
                        Location.PipelineBuilder,
                        Location.MiddlewarePreExecution,
                        Location.NestedStreamProducer,
                    ]),

            ];
        }

        // removal tests

        yield return
        [
            new(dataType,
                Location.NestedStreamProducer,
                Location.MiddlewarePostExecution,
                [
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.NestedClassPreExecution,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.NestedClassPreExecution,
                Location.ProducerPostNestedExecution,
                [
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedStreamProducer,
                ]),
        ];
    }

    private static IEnumerable<List<ConquerorContextDataTestCaseData>> GenerateBidirectionalTestCaseData(string dataType)
    {
        yield return
        [
            new(dataType,
                Location.PreExecution,
                null,
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                Array.Empty<string>()),

        ];

        yield return
        [
            new(dataType,
                Location.PipelineBuilder,
                null,
                [
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.MiddlewarePreExecution,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.MiddlewarePostExecution,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.ProducerPreNestedExecution,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.ProducerPostNestedExecution,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPostNestedExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.NestedClassPreExecution,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.NestedClassPostExecution,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPostExecution,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.NestedStreamProducer,
                null,
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.ProducerPreNestedExecution,
                    Location.NestedClassPreExecution,
                ]),
        ];

        // overwrite tests

        foreach (var overWriteDataType in new[] { DataType.String, DataType.Object })
        {
            yield return
            [
                new(dataType,
                    Location.PreExecution,
                    null,
                    [
                        Location.PreExecution,
                        Location.PipelineBuilder,
                    ],
                    [
                        Location.PostExecution,
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.ProducerPreNestedExecution,
                        Location.ProducerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedStreamProducer,
                    ]),

                new(overWriteDataType,
                    Location.MiddlewarePreExecution,
                    null,
                    [
                        Location.PostExecution,
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.ProducerPreNestedExecution,
                        Location.ProducerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedStreamProducer,
                    ],
                    [
                        Location.PreExecution,
                        Location.PipelineBuilder,
                    ]),
            ];

            yield return
            [
                new(dataType,
                    Location.PreExecution,
                    null,
                    [
                        Location.PreExecution,
                        Location.PipelineBuilder,
                        Location.MiddlewarePreExecution,
                    ],
                    [
                        Location.PostExecution,
                        Location.MiddlewarePostExecution,
                        Location.ProducerPreNestedExecution,
                        Location.ProducerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedStreamProducer,
                    ]),

                new(overWriteDataType,
                    Location.ProducerPreNestedExecution,
                    null,
                    [
                        Location.PostExecution,
                        Location.MiddlewarePostExecution,
                        Location.ProducerPreNestedExecution,
                        Location.ProducerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedStreamProducer,
                    ],
                    [
                        Location.PreExecution,
                        Location.PipelineBuilder,
                        Location.MiddlewarePreExecution,
                    ]),
            ];
        }

        // removal tests

        yield return
        [
            new(dataType,
                Location.PreExecution,
                Location.MiddlewarePreExecution,
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                ],
                [
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.PreExecution,
                Location.ProducerPreNestedExecution,
                [
                    Location.PreExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                ],
                [
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.NestedStreamProducer,
                Location.MiddlewarePostExecution,
                [
                    Location.ProducerPostNestedExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.NestedClassPreExecution,
                ]),
        ];

        yield return
        [
            new(dataType,
                Location.NestedClassPreExecution,
                Location.ProducerPostNestedExecution,
                [
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedStreamProducer,
                ],
                [
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.PipelineBuilder,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.ProducerPreNestedExecution,
                    Location.ProducerPostNestedExecution,
                ]),
        ];
    }

    private static void SetAndObserveContextData(ConquerorContext ctx, TestDataInstructions testDataInstructions, TestObservations testObservations, string location)
    {
        foreach (var (key, value, _) in testDataInstructions.DownstreamDataToSet.Where(t => t.Location == location))
        {
            ctx.DownstreamContextData.Set(key, value);
        }

        foreach (var (key, _) in testDataInstructions.DownstreamDataToRemove.Where(t => t.Location == location))
        {
            var wasRemoved = ctx.DownstreamContextData.Remove(key);

            Assert.That(wasRemoved, Is.True); // catch wrong test setup where data that wasn't set is removed
        }

        foreach (var (key, value, _) in testDataInstructions.UpstreamDataToSet.Where(t => t.Location == location))
        {
            ctx.UpstreamContextData.Set(key, value);
        }

        foreach (var (key, _) in testDataInstructions.UpstreamDataToRemove.Where(t => t.Location == location))
        {
            var wasRemoved = ctx.UpstreamContextData.Remove(key);

            Assert.That(wasRemoved, Is.True); // catch wrong test setup where data that wasn't set is removed
        }

        foreach (var (key, value, _) in testDataInstructions.BidirectionalDataToSet.Where(t => t.Location == location))
        {
            ctx.ContextData.Set(key, value);
        }

        foreach (var (key, _) in testDataInstructions.BidirectionalDataToRemove.Where(t => t.Location == location))
        {
            var wasRemoved = ctx.ContextData.Remove(key);

            Assert.That(wasRemoved, Is.True); // catch wrong test setup where data that wasn't set is removed
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
    public sealed record ConquerorContextDataTestCase(string DataDirection, List<ConquerorContextDataTestCaseData> TestData);

    [SuppressMessage("Critical Code Smell", "S3218:Inner class members should not shadow outer class \"static\" or type members", Justification = "The name makes sense and there is little risk of confusing a property and a class.")]
    public sealed record ConquerorContextDataTestCaseData(
        string DataType,
        string DataSettingLocation,
        string? DataRemovalLocation,
        IReadOnlyCollection<string> LocationsWhereDataShouldBeAccessible,
        IReadOnlyCollection<string> LocationsWhereDataShouldNotBeAccessible);

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
        public const string PipelineBuilder = nameof(PipelineBuilder);
        public const string MiddlewarePreExecution = nameof(MiddlewarePreExecution);
        public const string MiddlewarePostExecution = nameof(MiddlewarePostExecution);
        public const string ProducerPreNestedExecution = nameof(ProducerPreNestedExecution);
        public const string ProducerPostNestedExecution = nameof(ProducerPostNestedExecution);
        public const string NestedClassPreExecution = nameof(NestedClassPreExecution);
        public const string NestedClassPostExecution = nameof(NestedClassPostExecution);
        public const string NestedStreamProducer = nameof(NestedStreamProducer);
    }

    private static class DataType
    {
        public const string String = nameof(String);
        public const string Object = nameof(Object);
    }

    private sealed record TestDataEntry(int Value);

    private sealed record TestStreamingRequest;

    private sealed record TestItem;

    private sealed record NestedTestStreamingRequest;

    private sealed class TestStreamProducerMiddleware(
        TestDataInstructions dataInstructions,
        TestObservations observations)
        : IStreamProducerMiddleware
    {
        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            await Task.Yield();

            SetAndObserveContextData(ctx.ConquerorContext, dataInstructions, observations, Location.MiddlewarePreExecution);

            await foreach (var item in ctx.Next(ctx.Request, ctx.CancellationToken))
            {
                yield return item;
            }

            SetAndObserveContextData(ctx.ConquerorContext, dataInstructions, observations, Location.MiddlewarePostExecution);
        }
    }

    private sealed class NestedTestClass(
        IConquerorContextAccessor conquerorContextAccessor,
        TestObservations observations,
        TestDataInstructions dataInstructions,
        IStreamProducer<NestedTestStreamingRequest, TestItem> nestedStreamProducer)
    {
        public async Task Execute()
        {
            SetAndObserveContextData(conquerorContextAccessor.ConquerorContext!, dataInstructions, observations, Location.NestedClassPreExecution);

            _ = await nestedStreamProducer.ExecuteRequest(new()).Drain();

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
