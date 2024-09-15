namespace Conqueror.Streaming.Tests;

public sealed class StreamConsumerMiddlewareFunctionalityTests
{
    [Test]
    public async Task GivenConsumerWithNoConsumerMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithoutMiddlewares>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.ItemsFromMiddlewares, Is.Empty);
        Assert.That(observations.MiddlewareTypes, Is.Empty);
    }

    [Test]
    public async Task GivenConsumerWithSingleAppliedConsumerMiddleware_MiddlewareIsCalledForEachItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithSingleMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(new[] { item }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamConsumerMiddleware) }));
    }

    [Test]
    public async Task GivenConsumerWithSingleAppliedConsumerMiddlewareWithParameter_MiddlewareIsCalledWithConfiguration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithSingleMiddlewareWithParameter>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer.HandleItem(new(10));

        Assert.That(observations.ConfigurationFromMiddlewares, Is.EquivalentTo(new[] { new TestStreamConsumerMiddlewareConfiguration { Parameter = 10 } }));
    }

    [Test]
    public async Task GivenConsumerWithMultipleAppliedConsumerMiddlewares_MiddlewaresAreCalledForEachItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithMultipleMiddlewares>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(new[] { item, item }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamConsumerMiddleware), typeof(TestStreamConsumerMiddleware2) }));
    }

    [Test]
    public async Task GivenConsumerWithSameMiddlewareAppliedMultipleTimes_MiddlewareIsCalledForEachItemMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithoutMiddlewares>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamConsumerMiddleware2>()
                                    .Use<TestStreamConsumerMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(new[] { item, item }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamConsumerMiddleware2), typeof(TestStreamConsumerMiddleware2) }));
    }

    [Test]
    public async Task GivenConsumerWithAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithoutMiddlewares>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new())
                                    .Use<TestStreamConsumerMiddleware2>()
                                    .Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new())
                                    .Without<TestStreamConsumerMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(new[] { item, item }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamConsumerMiddleware), typeof(TestStreamConsumerMiddleware) }));
    }

    [Test]
    public async Task GivenConsumerWithAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithoutMiddlewares>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamConsumerMiddleware2>()
                                    .Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new())
                                    .Use<TestStreamConsumerMiddleware2>()
                                    .Without<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>();
                    });

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(new[] { item, item }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamConsumerMiddleware2), typeof(TestStreamConsumerMiddleware2) }));
    }

    [Test]
    public async Task GivenConsumerWithMultipleAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithoutMiddlewares>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new())
                                    .Use<TestStreamConsumerMiddleware2>()
                                    .Use<TestStreamConsumerMiddleware2>()
                                    .Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new())
                                    .Without<TestStreamConsumerMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(new[] { item, item }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamConsumerMiddleware), typeof(TestStreamConsumerMiddleware) }));
    }

    [Test]
    public async Task GivenConsumerWithMultipleAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithoutMiddlewares>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamConsumerMiddleware2>()
                                    .Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new())
                                    .Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new())
                                    .Use<TestStreamConsumerMiddleware2>()
                                    .Without<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>();
                    });

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(new[] { item, item }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamConsumerMiddleware2), typeof(TestStreamConsumerMiddleware2) }));
    }

    [Test]
    public async Task GivenPipelineWithExistingMiddleware_WhenAddingSameMiddlewareAgainAfterRemovingPreviousMiddleware_MiddlewareIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithoutMiddlewares>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamConsumerMiddleware2>()
                                    .Without<TestStreamConsumerMiddleware2>()
                                    .Use<TestStreamConsumerMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(new[] { item }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamConsumerMiddleware2) }));
    }

    [Test]
    public async Task GivenPipelineWithExistingMiddlewareWithConfiguration_WhenAddingSameMiddlewareAgainAfterRemovingPreviousMiddleware_MiddlewareIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithoutMiddlewares>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new())
                                    .Without<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>()
                                    .Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new());
                    });

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(new[] { item }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamConsumerMiddleware) }));
    }

    [Test]
    public async Task GivenConsumerWithRetryMiddleware_MiddlewaresAreCalledMultipleTimesForEachItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithRetryMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerRetryMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(new[] { item, item, item, item, item }));
        Assert.That(observations.MiddlewareTypes,
                    Is.EquivalentTo(new[]
                    {
                        typeof(TestStreamConsumerRetryMiddleware), typeof(TestStreamConsumerMiddleware), typeof(TestStreamConsumerMiddleware2), typeof(TestStreamConsumerMiddleware), typeof(TestStreamConsumerMiddleware2),
                    }));
    }

    [Test]
    public async Task GivenCancellationToken_MiddlewaresReceiveCancellationTokenWhenCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithMultipleMiddlewares>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();
        using var tokenSource = new CancellationTokenSource();

        await consumer.HandleItem(new(10), tokenSource.Token);

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(new[] { tokenSource.Token, tokenSource.Token }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithMultipleMutatingMiddlewares>()
                    .AddConquerorStreamConsumerMiddleware<MutatingTestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<MutatingTestStreamConsumerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer.HandleItem(new(0));

        var item1 = new TestItem(0);
        var item2 = new TestItem(1);
        var item3 = new TestItem(3);

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(new[] { item1, item2 }));
        Assert.That(observations.ItemsFromConsumers, Is.EquivalentTo(new[] { item3 }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false) } };

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithMultipleMutatingMiddlewares>()
                    .AddConquerorStreamConsumerMiddleware<MutatingTestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<MutatingTestStreamConsumerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer.HandleItem(new(0), tokens.CancellationTokens[0]);

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromConsumers, Is.EquivalentTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public async Task GivenPipelineThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var observedInstances = new List<TestService>();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithoutMiddlewares>()
                    .AddScoped<TestService>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline => observedInstances.Add(pipeline.ServiceProvider.GetRequiredService<TestService>()));

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer2 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer1.HandleItem(new(10));
        await consumer2.HandleItem(new(10));
        await consumer3.HandleItem(new(10));

        Assert.That(observedInstances, Has.Count.EqualTo(3));
        Assert.That(observedInstances[1], Is.Not.SameAs(observedInstances[0]));
        Assert.That(observedInstances[2], Is.SameAs(observedInstances[0]));
    }

    [Test]
    public void GivenPipelineThatUsesUnregisteredMiddleware_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithoutMiddlewares>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline => pipeline.Use<TestStreamConsumerMiddleware2>());

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => consumer.HandleItem(new(10)));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestStreamConsumerMiddleware2)));
    }

    [Test]
    public void GivenPipelineThatUsesUnregisteredMiddlewareWithConfiguration_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithoutMiddlewares>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline => pipeline.Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new()));

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => consumer.HandleItem(new(10)));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestStreamConsumerMiddleware)));
    }

    [Test]
    public void GivenMiddlewareThatThrows_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithThrowingMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<ThrowingTestStreamConsumerMiddleware>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => consumer.HandleItem(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenConsumerDelegateWithSingleAppliedMiddleware_MiddlewareIsCalledForEachItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumerFactory>().Create<TestItem>(async (item, p, cancellationToken) =>
                                                                                                      {
                                                                                                          await Task.Yield();
                                                                                                          var obs = p.GetRequiredService<TestObservations>();
                                                                                                          obs.ItemsFromConsumers.Add(item);
                                                                                                          obs.CancellationTokensFromConsumers.Add(cancellationToken);
                                                                                                      },
                                                                                                      pipeline => pipeline.Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new()));

        var item = new TestItem(10);

        await consumer.HandleItem(item);

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(new[] { item }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamConsumerMiddleware) }));
    }

    [Test]
    public void InvalidMiddlewares()
    {
        Assert.That(() => new ServiceCollection().AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddlewareWithMultipleInterfaces>(), Throws.ArgumentException);
        Assert.That(() => new ServiceCollection().AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddlewareWithMultipleInterfaces>(_ => new()), Throws.ArgumentException);
        Assert.That(() => new ServiceCollection().AddConquerorStreamConsumerMiddleware(new TestStreamConsumerMiddlewareWithMultipleInterfaces()), Throws.ArgumentException);
    }

    private sealed record TestItem(int Payload);

    private sealed class TestStreamConsumerWithSingleMiddleware(TestObservations observations) : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.ItemsFromConsumers.Add(item);
            observations.CancellationTokensFromConsumers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new());
        }
    }

    private sealed class TestStreamConsumerWithSingleMiddlewareWithParameter(TestObservations observations) : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.ItemsFromConsumers.Add(item);
            observations.CancellationTokensFromConsumers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new() { Parameter = 10 });
        }
    }

    private sealed class TestStreamConsumerWithMultipleMiddlewares(TestObservations observations) : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.ItemsFromConsumers.Add(item);
            observations.CancellationTokensFromConsumers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new())
                        .Use<TestStreamConsumerMiddleware2>();
        }
    }

    private sealed class TestStreamConsumerWithoutMiddlewares(TestObservations observations) : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.ItemsFromConsumers.Add(item);
            observations.CancellationTokensFromConsumers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IStreamConsumerPipelineBuilder>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestStreamConsumerWithRetryMiddleware(TestObservations observations) : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.ItemsFromConsumers.Add(item);
            observations.CancellationTokensFromConsumers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamConsumerRetryMiddleware>()
                        .Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new())
                        .Use<TestStreamConsumerMiddleware2>();
        }
    }

    private sealed class TestStreamConsumerWithMultipleMutatingMiddlewares(TestObservations observations) : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.ItemsFromConsumers.Add(item);
            observations.CancellationTokensFromConsumers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<MutatingTestStreamConsumerMiddleware>()
                        .Use<MutatingTestStreamConsumerMiddleware2>();
        }
    }

    private sealed class TestStreamConsumerWithThrowingMiddleware : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<ThrowingTestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new());
        }
    }

    private sealed record TestStreamConsumerMiddlewareConfiguration
    {
        public int Parameter { get; set; }
    }

    private sealed class TestStreamConsumerMiddleware(TestObservations observations) : IStreamConsumerMiddleware<TestStreamConsumerMiddlewareConfiguration>
    {
        public async Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem, TestStreamConsumerMiddlewareConfiguration> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.ItemsFromMiddlewares.Add(ctx.Item);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.ConfigurationFromMiddlewares.Add(ctx.Configuration);

            await ctx.Next(ctx.Item, ctx.CancellationToken);
        }
    }

    private sealed class TestStreamConsumerMiddleware2(TestObservations observations) : IStreamConsumerMiddleware
    {
        public async Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.ItemsFromMiddlewares.Add(ctx.Item);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            await ctx.Next(ctx.Item, ctx.CancellationToken);
        }
    }

    private sealed class TestStreamConsumerRetryMiddleware(TestObservations observations) : IStreamConsumerMiddleware
    {
        public async Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.ItemsFromMiddlewares.Add(ctx.Item);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            await ctx.Next(ctx.Item, ctx.CancellationToken);
            await ctx.Next(ctx.Item, ctx.CancellationToken);
        }
    }

    private sealed class MutatingTestStreamConsumerMiddleware(
        TestObservations observations,
        CancellationTokensToUse cancellationTokensToUse) : IStreamConsumerMiddleware
    {
        public async Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.ItemsFromMiddlewares.Add(ctx.Item);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            var item = ctx.Item;

            if (item is TestItem i)
            {
                item = (TItem)(object)new TestItem(i.Payload + 1);
            }

            await ctx.Next(item, cancellationTokensToUse.CancellationTokens[1]);
        }
    }

    private sealed class MutatingTestStreamConsumerMiddleware2(
        TestObservations observations,
        CancellationTokensToUse cancellationTokensToUse) : IStreamConsumerMiddleware
    {
        public async Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.ItemsFromMiddlewares.Add(ctx.Item);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            var item = ctx.Item;

            if (item is TestItem i)
            {
                item = (TItem)(object)new TestItem(i.Payload + 2);
            }

            await ctx.Next(item, cancellationTokensToUse.CancellationTokens[2]);
        }
    }

    private sealed class ThrowingTestStreamConsumerMiddleware(Exception exception) : IStreamConsumerMiddleware<TestStreamConsumerMiddlewareConfiguration>
    {
        public async Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem, TestStreamConsumerMiddlewareConfiguration> ctx)
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class TestStreamConsumerMiddlewareWithMultipleInterfaces : IStreamConsumerMiddleware<TestStreamConsumerMiddlewareConfiguration>,
                                                                              IStreamConsumerMiddleware
    {
        public Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem> ctx)
            =>
                throw new InvalidOperationException("this middleware should never be called");

        public Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem, TestStreamConsumerMiddlewareConfiguration> ctx)
            =>
                throw new InvalidOperationException("this middleware should never be called");
    }

    private sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = [];

        public List<object> ItemsFromConsumers { get; } = [];

        public List<object?> ItemsFromMiddlewares { get; } = [];

        public List<CancellationToken> CancellationTokensFromConsumers { get; } = [];

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = [];

        public List<object> ConfigurationFromMiddlewares { get; } = [];
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = [];
    }

    private sealed class TestService;
}
