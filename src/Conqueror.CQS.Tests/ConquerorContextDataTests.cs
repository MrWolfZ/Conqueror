namespace Conqueror.CQS.Tests;

public sealed class ConquerorContextDataTests
{
    private const string TestKey = "TestKey";

    [Test]
    [TestCaseSource(nameof(GenerateTestCases))]
    public async Task GivenDataSetup_WhenExecutingHandler_DataIsCorrectlyAvailable(ConquerorContextDataTestCase testCase)
    {
        const string testStringValue = "TestValue";

        var testDataInstructions = new TestDataInstructions();
        var testObservations = new TestObservations();

        var dataToSetCol = testCase.DataDirection == DataDirection.Downstream ? testDataInstructions.DownstreamDataToSet : testDataInstructions.UpstreamDataToSet;
        var dataToRemoveCol = testCase.DataDirection == DataDirection.Downstream ? testDataInstructions.DownstreamDataToRemove : testDataInstructions.UpstreamDataToRemove;

        foreach (var (data, i) in testCase.TestData.Select((value, i) => (value, i)))
        {
            dataToSetCol.Add((TestKey, data.DataType == DataType.String ? testStringValue + i : new TestDataEntry(i), data.DataSettingLocation));

            if (data.DataRemovalLocation is not null)
            {
                dataToRemoveCol.Add((TestKey, data.DataRemovalLocation));
            }
        }

        var services = new ServiceCollection();

        _ = services.AddSingleton(testDataInstructions)
                    .AddSingleton(testObservations)
                    .AddSingleton<NestedTestClass>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(
                        async (_, p, _) =>
                        {
                            SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.HandlerPreNestedExecution);

                            await p.GetRequiredService<NestedTestClass>().Execute();

                            SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.HandlerPostNestedExecution);

                            return new();
                        },
                        pipeline => pipeline.Use<TestCommandMiddleware>())
                    .AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(
                        async (_, p, _) =>
                        {
                            SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.HandlerPreNestedExecution);

                            await p.GetRequiredService<NestedTestClass>().Execute();

                            SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.HandlerPostNestedExecution);

                            return new();
                        },
                        pipeline => pipeline.Use<TestQueryMiddleware>())
                    .AddConquerorCommandHandlerDelegate<NestedTestCommand, TestCommandResponse>(
                        (_, p, _) =>
                        {
                            SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.NestedCommandHandler);
                            return Task.FromResult<TestCommandResponse>(new());
                        })
                    .AddConquerorQueryHandlerDelegate<NestedTestQuery, TestQueryResponse>(
                        (_, p, _) =>
                        {
                            SetAndObserveContextData(p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, testDataInstructions, testObservations, Location.NestedQueryHandler);
                            return Task.FromResult<TestQueryResponse>(new());
                        });

        await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        SetAndObserveContextData(conquerorContext, testDataInstructions, testObservations, Location.PreExecution);

        if (testCase.RequestType == RequestType.Command)
        {
            _ = await serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(new());
        }

        if (testCase.RequestType == RequestType.Query)
        {
            _ = await serviceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(new());
        }

        SetAndObserveContextData(conquerorContext, testDataInstructions, testObservations, Location.PostExecution);

        var observedData = testCase.DataDirection == DataDirection.Downstream ? testObservations.ObservedDownstreamData : testObservations.ObservedUpstreamData;

        foreach (var (data, i) in testCase.TestData.Select((value, i) => (value, i)))
        {
            object value = data.DataType == DataType.String ? testStringValue + i : new TestDataEntry(i);

            // we assert on count equal to 2, because observed data should be added twice (once by enumeration and once by direct access)
            Assert.That(data.LocationsWhereDataShouldBeAccessible.All(l => observedData.Count(d => d.Value.Equals(value) && d.Location == l) == 2), Is.True);
            Assert.That(data.LocationsWhereDataShouldNotBeAccessible.All(l => !observedData.Any(d => d.Value.Equals(value) && d.Location == l)), Is.True);
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
            }
        }
    }

    private static IEnumerable<List<ConquerorContextDataTestCaseData>> GenerateDownstreamTestCaseData(string dataType)
    {
        yield return new()
        {
            new(dataType,
                Location.PreExecution,
                null,
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                },
                Array.Empty<string>()),
        };

        yield return new()
        {
            new(dataType,
                Location.MiddlewarePreExecution,
                null,
                new[]
                {
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.MiddlewarePostExecution,
                null,
                new[]
                {
                    Location.MiddlewarePostExecution,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.HandlerPreNestedExecution,
                null,
                new[]
                {
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.HandlerPostNestedExecution,
                null,
                new[]
                {
                    Location.MiddlewarePostExecution,
                    Location.HandlerPostNestedExecution,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.HandlerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.NestedClassPreExecution,
                null,
                new[]
                {
                    Location.MiddlewarePostExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.HandlerPreNestedExecution,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.NestedClassPostExecution,
                null,
                new[]
                {
                    Location.MiddlewarePostExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPostExecution,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.HandlerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.NestedCommandHandler,
                null,
                new[]
                {
                    Location.NestedCommandHandler,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.NestedQueryHandler,
                null,
                new[]
                {
                    Location.NestedQueryHandler,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedCommandHandler,
                }),
        };

        // overwrite tests

        foreach (var overWriteDataType in new[] { DataType.String, DataType.Object })
        {
            yield return new()
            {
                new(dataType,
                    Location.PreExecution,
                    null,
                    new[]
                    {
                        Location.PreExecution,
                        Location.PostExecution,
                    },
                    new[]
                    {
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPreNestedExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedCommandHandler,
                        Location.NestedQueryHandler,
                    }),

                new(overWriteDataType,
                    Location.MiddlewarePreExecution,
                    null,
                    new[]
                    {
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPreNestedExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedCommandHandler,
                        Location.NestedQueryHandler,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.PostExecution,
                    }),
            };

            yield return new()
            {
                new(dataType,
                    Location.PreExecution,
                    null,
                    new[]
                    {
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.MiddlewarePreExecution,
                    },
                    new[]
                    {
                        Location.MiddlewarePostExecution,
                        Location.HandlerPreNestedExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedCommandHandler,
                        Location.NestedQueryHandler,
                    }),

                new(overWriteDataType,
                    Location.HandlerPreNestedExecution,
                    null,
                    new[]
                    {
                        Location.MiddlewarePostExecution,
                        Location.HandlerPreNestedExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedCommandHandler,
                        Location.NestedQueryHandler,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.MiddlewarePreExecution,
                    }),
            };

            yield return new()
            {
                new(dataType,
                    Location.MiddlewarePreExecution,
                    null,
                    new[]
                    {
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPreNestedExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedQueryHandler,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.NestedCommandHandler,
                    }),

                new(overWriteDataType,
                    Location.NestedCommandHandler,
                    null,
                    new[]
                    {
                        Location.NestedCommandHandler,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPreNestedExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedQueryHandler,
                    }),
            };
        }

        // removal tests

        yield return new()
        {
            new(dataType,
                Location.PreExecution,
                Location.MiddlewarePreExecution,
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                },
                new[]
                {
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.PreExecution,
                Location.HandlerPreNestedExecution,
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                },
                new[]
                {
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.MiddlewarePreExecution,
                Location.NestedCommandHandler,
                new[]
                {
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedQueryHandler,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.NestedCommandHandler,
                }),
        };
    }

    private static IEnumerable<List<ConquerorContextDataTestCaseData>> GenerateUpstreamTestCaseData(string dataType)
    {
        yield return new()
        {
            new(dataType,
                Location.NestedCommandHandler,
                null,
                new[]
                {
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedCommandHandler,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.MiddlewarePreExecution,
                    Location.HandlerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.NestedQueryHandler,
                null,
                new[]
                {
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedQueryHandler,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.MiddlewarePreExecution,
                    Location.HandlerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedCommandHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.NestedClassPreExecution,
                null,
                new[]
                {
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.MiddlewarePreExecution,
                    Location.HandlerPreNestedExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.NestedClassPostExecution,
                null,
                new[]
                {
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPostExecution,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.MiddlewarePreExecution,
                    Location.HandlerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.HandlerPreNestedExecution,
                null,
                new[]
                {
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.MiddlewarePreExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.HandlerPostNestedExecution,
                null,
                new[]
                {
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPostNestedExecution,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.MiddlewarePreExecution,
                    Location.HandlerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.MiddlewarePreExecution,
                null,
                new[]
                {
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.MiddlewarePostExecution,
                null,
                new[]
                {
                    Location.PostExecution,
                    Location.MiddlewarePostExecution,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.MiddlewarePreExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                }),
        };

        // overwrite tests

        foreach (var overWriteDataType in new[] { DataType.String, DataType.Object })
        {
            yield return new()
            {
                new(dataType,
                    Location.NestedCommandHandler,
                    null,
                    new[]
                    {
                        Location.NestedCommandHandler,
                        Location.NestedClassPostExecution,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPreNestedExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedQueryHandler,
                    }),

                new(overWriteDataType,
                    Location.HandlerPostNestedExecution,
                    null,
                    new[]
                    {
                        Location.PostExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPostNestedExecution,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.MiddlewarePreExecution,
                        Location.HandlerPreNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedCommandHandler,
                        Location.NestedQueryHandler,
                    }),
            };

            yield return new()
            {
                new(dataType,
                    Location.NestedQueryHandler,
                    null,
                    new[]
                    {
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedQueryHandler,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPreNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedCommandHandler,
                    }),

                new(overWriteDataType,
                    Location.MiddlewarePostExecution,
                    null,
                    new[]
                    {
                        Location.PostExecution,
                        Location.MiddlewarePostExecution,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.MiddlewarePreExecution,
                        Location.HandlerPreNestedExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedCommandHandler,
                        Location.NestedQueryHandler,
                    }),
            };

            yield return new()
            {
                new(dataType,
                    Location.NestedClassPreExecution,
                    null,
                    new[]
                    {
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPreNestedExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedCommandHandler,
                        Location.NestedQueryHandler,
                    }),

                new(overWriteDataType,
                    Location.HandlerPostNestedExecution,
                    null,
                    new[]
                    {
                        Location.PostExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPostNestedExecution,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.MiddlewarePreExecution,
                        Location.HandlerPreNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedCommandHandler,
                        Location.NestedQueryHandler,
                    }),
            };

            yield return new()
            {
                new(dataType,
                    Location.PreExecution,
                    null,
                    new[]
                    {
                        Location.PreExecution,
                    },
                    new[]
                    {
                        Location.PostExecution,
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPreNestedExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedCommandHandler,
                        Location.NestedQueryHandler,
                    }),

                new(overWriteDataType,
                    Location.HandlerPreNestedExecution,
                    null,
                    new[]
                    {
                        Location.PostExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPreNestedExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedClassPostExecution,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.MiddlewarePreExecution,
                        Location.NestedCommandHandler,
                        Location.NestedQueryHandler,
                    }),
            };

            yield return new()
            {
                new(dataType,
                    Location.HandlerPreNestedExecution,
                    null,
                    new[]
                    {
                        Location.HandlerPreNestedExecution,
                        Location.NestedClassPreExecution,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.PostExecution,
                        Location.MiddlewarePreExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedCommandHandler,
                        Location.NestedQueryHandler,
                    }),

                new(overWriteDataType,
                    Location.NestedCommandHandler,
                    null,
                    new[]
                    {
                        Location.PostExecution,
                        Location.MiddlewarePostExecution,
                        Location.HandlerPostNestedExecution,
                        Location.NestedClassPostExecution,
                        Location.NestedCommandHandler,
                    },
                    new[]
                    {
                        Location.PreExecution,
                        Location.MiddlewarePreExecution,
                        Location.HandlerPreNestedExecution,
                        Location.NestedClassPreExecution,
                        Location.NestedQueryHandler,
                    }),
            };
        }

        // removal tests

        yield return new()
        {
            new(dataType,
                Location.NestedCommandHandler,
                Location.HandlerPostNestedExecution,
                new[]
                {
                    Location.NestedCommandHandler,
                    Location.NestedClassPostExecution,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedQueryHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.NestedQueryHandler,
                Location.MiddlewarePostExecution,
                new[]
                {
                    Location.HandlerPostNestedExecution,
                    Location.NestedClassPostExecution,
                    Location.NestedQueryHandler,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.NestedClassPreExecution,
                    Location.NestedCommandHandler,
                }),
        };

        yield return new()
        {
            new(dataType,
                Location.NestedClassPreExecution,
                Location.HandlerPostNestedExecution,
                new[]
                {
                    Location.NestedClassPreExecution,
                    Location.NestedClassPostExecution,
                },
                new[]
                {
                    Location.PreExecution,
                    Location.PostExecution,
                    Location.MiddlewarePreExecution,
                    Location.MiddlewarePostExecution,
                    Location.HandlerPreNestedExecution,
                    Location.HandlerPostNestedExecution,
                    Location.NestedCommandHandler,
                    Location.NestedQueryHandler,
                }),
        };
    }

    private static void SetAndObserveContextData(IConquerorContext ctx, TestDataInstructions testDataInstructions, TestObservations testObservations, string location)
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

        foreach (var (key, value, _) in ctx.DownstreamContextData)
        {
            testObservations.ObservedDownstreamData.Add((key, value, location));
        }

        if (ctx.DownstreamContextData.TryGet<object>(TestKey, out var downstreamValue))
        {
            testObservations.ObservedDownstreamData.Add((TestKey, downstreamValue, location));
        }

        foreach (var (key, value, _) in ctx.UpstreamContextData)
        {
            testObservations.ObservedUpstreamData.Add((key, value, location));
        }

        if (ctx.UpstreamContextData.TryGet<object>(TestKey, out var upstreamValue))
        {
            testObservations.ObservedUpstreamData.Add((TestKey, upstreamValue, location));
        }
    }

    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "must be public for test method signature")]
    public sealed record ConquerorContextDataTestCase(string RequestType, string DataDirection, List<ConquerorContextDataTestCaseData> TestData);

    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "must be public for test method signature")]
    public sealed record ConquerorContextDataTestCaseData(string DataType,
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
    }

    private static class Location
    {
        public const string PreExecution = nameof(PreExecution);
        public const string PostExecution = nameof(PostExecution);
        public const string MiddlewarePreExecution = nameof(MiddlewarePreExecution);
        public const string MiddlewarePostExecution = nameof(MiddlewarePostExecution);
        public const string HandlerPreNestedExecution = nameof(HandlerPreNestedExecution);
        public const string HandlerPostNestedExecution = nameof(HandlerPostNestedExecution);
        public const string NestedClassPreExecution = nameof(NestedClassPreExecution);
        public const string NestedClassPostExecution = nameof(NestedClassPostExecution);
        public const string NestedCommandHandler = nameof(NestedCommandHandler);
        public const string NestedQueryHandler = nameof(NestedQueryHandler);
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

    private sealed class TestCommandMiddleware : ICommandMiddleware
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestDataInstructions testDataInstructions;
        private readonly TestObservations testObservations;

        public TestCommandMiddleware(IConquerorContextAccessor conquerorContextAccessor, TestDataInstructions dataInstructions, TestObservations observations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            testDataInstructions = dataInstructions;
            testObservations = observations;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            await Task.Yield();

            SetAndObserveContextData(conquerorContextAccessor.ConquerorContext!, testDataInstructions, testObservations, Location.MiddlewarePreExecution);

            var response = await ctx.Next(ctx.Command, ctx.CancellationToken);

            SetAndObserveContextData(conquerorContextAccessor.ConquerorContext!, testDataInstructions, testObservations, Location.MiddlewarePostExecution);

            return response;
        }
    }

    private sealed class TestQueryMiddleware : IQueryMiddleware
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly TestDataInstructions testDataInstructions;
        private readonly TestObservations testObservations;

        public TestQueryMiddleware(IConquerorContextAccessor conquerorContextAccessor, TestDataInstructions dataInstructions, TestObservations observations)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            testDataInstructions = dataInstructions;
            testObservations = observations;
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();

            SetAndObserveContextData(conquerorContextAccessor.ConquerorContext!, testDataInstructions, testObservations, Location.MiddlewarePreExecution);

            var response = await ctx.Next(ctx.Query, ctx.CancellationToken);

            SetAndObserveContextData(conquerorContextAccessor.ConquerorContext!, testDataInstructions, testObservations, Location.MiddlewarePostExecution);

            return response;
        }
    }

    private sealed class NestedTestClass
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly ICommandHandler<NestedTestCommand, TestCommandResponse> nestedCommandHandler;
        private readonly IQueryHandler<NestedTestQuery, TestQueryResponse> nestedQueryHandler;
        private readonly TestDataInstructions testDataInstructions;
        private readonly TestObservations testObservations;

        public NestedTestClass(IConquerorContextAccessor conquerorContextAccessor,
                               TestObservations observations,
                               TestDataInstructions dataInstructions,
                               ICommandHandler<NestedTestCommand, TestCommandResponse> nestedCommandHandler,
                               IQueryHandler<NestedTestQuery, TestQueryResponse> nestedQueryHandler)
        {
            this.conquerorContextAccessor = conquerorContextAccessor;
            testObservations = observations;
            this.nestedCommandHandler = nestedCommandHandler;
            this.nestedQueryHandler = nestedQueryHandler;
            testDataInstructions = dataInstructions;
        }

        public async Task Execute()
        {
            SetAndObserveContextData(conquerorContextAccessor.ConquerorContext!, testDataInstructions, testObservations, Location.NestedClassPreExecution);

            _ = await nestedCommandHandler.ExecuteCommand(new());
            _ = await nestedQueryHandler.ExecuteQuery(new());

            SetAndObserveContextData(conquerorContextAccessor.ConquerorContext!, testDataInstructions, testObservations, Location.NestedClassPostExecution);
        }
    }

    private sealed class TestDataInstructions
    {
        public List<(string Key, object Value, string Location)> DownstreamDataToSet { get; } = new();

        public List<(string Key, string Location)> DownstreamDataToRemove { get; } = new();

        public List<(string Key, object Value, string Location)> UpstreamDataToSet { get; } = new();

        public List<(string Key, string Location)> UpstreamDataToRemove { get; } = new();
    }

    private sealed class TestObservations
    {
        public List<(string Key, object Value, string Location)> ObservedDownstreamData { get; } = new();

        public List<(string Key, object Value, string Location)> ObservedUpstreamData { get; } = new();
    }
}
