using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class StreamProducerMiddlewareFunctionalityTests
{
    [Test]
    public async Task GivenProducerWithNoProducerMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithoutMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.Empty);
        Assert.That(observations.MiddlewareTypes, Is.Empty);
    }

    [Test]
    public async Task GivenProducerWithSingleAppliedProducerMiddleware_MiddlewareIsCalledWithRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithSingleMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamProducerMiddleware) }));
    }

    [Test]
    public async Task GivenProducerWithSingleAppliedProducerMiddlewareWithParameter_MiddlewareIsCalledWithConfiguration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithSingleMiddlewareWithParameter>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer.ExecuteRequest(new(10), CancellationToken.None).Drain();

        Assert.That(observations.ConfigurationFromMiddlewares, Is.EquivalentTo(new[] { new TestStreamProducerMiddlewareConfiguration { Parameter = 10 } }));
    }

    [Test]
    public async Task GivenProducerWithMultipleAppliedProducerMiddlewares_MiddlewaresAreCalledWithRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithMultipleMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamProducerMiddleware), typeof(TestStreamProducerMiddleware2) }));
    }

    [Test]
    public async Task GivenProducerWithSameMiddlewareAppliedMultipleTimes_MiddlewareIsCalledWithRequestMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithoutMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamProducerMiddleware2>()
                                    .Use<TestStreamProducerMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamProducerMiddleware2), typeof(TestStreamProducerMiddleware2) }));
    }

    [Test]
    public async Task GivenProducerWithAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithoutMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new())
                                    .Use<TestStreamProducerMiddleware2>()
                                    .Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new())
                                    .Without<TestStreamProducerMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamProducerMiddleware), typeof(TestStreamProducerMiddleware) }));
    }

    [Test]
    public async Task GivenProducerWithAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithoutMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamProducerMiddleware2>()
                                    .Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new())
                                    .Use<TestStreamProducerMiddleware2>()
                                    .Without<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>();
                    });

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamProducerMiddleware2), typeof(TestStreamProducerMiddleware2) }));
    }

    [Test]
    public async Task GivenProducerWithMultipleAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithoutMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new())
                                    .Use<TestStreamProducerMiddleware2>()
                                    .Use<TestStreamProducerMiddleware2>()
                                    .Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new())
                                    .Without<TestStreamProducerMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamProducerMiddleware), typeof(TestStreamProducerMiddleware) }));
    }

    [Test]
    public async Task GivenProducerWithMultipleAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithoutMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamProducerMiddleware2>()
                                    .Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new())
                                    .Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new())
                                    .Use<TestStreamProducerMiddleware2>()
                                    .Without<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>();
                    });

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamProducerMiddleware2), typeof(TestStreamProducerMiddleware2) }));
    }

    [Test]
    public async Task GivenPipelineWithExistingMiddleware_WhenAddingSameMiddlewareAgainAfterRemovingPreviousMiddleware_MiddlewareIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithoutMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamProducerMiddleware2>()
                                    .Without<TestStreamProducerMiddleware2>()
                                    .Use<TestStreamProducerMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamProducerMiddleware2) }));
    }

    [Test]
    public async Task GivenPipelineWithExistingMiddlewareWithConfiguration_WhenAddingSameMiddlewareAgainAfterRemovingPreviousMiddleware_MiddlewareIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithoutMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new())
                                    .Without<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>()
                                    .Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new());
                    });

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamProducerMiddleware) }));
    }

    [Test]
    public async Task GivenProducerWithRetryMiddleware_MiddlewaresAreCalledMultipleTimesWithRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithRetryMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerRetryMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request, request, request, request }));
        Assert.That(observations.MiddlewareTypes,
                    Is.EquivalentTo(new[]
                    {
                        typeof(TestStreamProducerRetryMiddleware), typeof(TestStreamProducerMiddleware), typeof(TestStreamProducerMiddleware2), typeof(TestStreamProducerMiddleware), typeof(TestStreamProducerMiddleware2),
                    }));
    }

    [Test]
    public async Task GivenProducerWithPipelineConfigurationMethodWithoutPipelineConfigurationInterface_MiddlewaresAreNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithPipelineConfigurationWithoutPipelineConfigurationInterface>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.Empty);
        Assert.That(observations.MiddlewareTypes, Is.Empty);
    }

    [Test]
    public async Task GivenCancellationToken_MiddlewaresReceiveCancellationTokenWhenCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithMultipleMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await producer.ExecuteRequest(new(10), tokenSource.Token).Drain();

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(new[] { tokenSource.Token, tokenSource.Token }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithMultipleMutatingMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<MutatingTestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<MutatingTestStreamProducerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer.ExecuteRequest(new(0), CancellationToken.None).Drain();

        var request1 = new TestStreamingRequest(0);
        var request2 = new TestStreamingRequest(1);
        var request3 = new TestStreamingRequest(3);

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request1, request2 }));
        Assert.That(observations.RequestsFromProducers, Is.EquivalentTo(new[] { request3 }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithMultipleMutatingMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<MutatingTestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<MutatingTestStreamProducerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var response = await producer.ExecuteRequest(new(0), CancellationToken.None).Drain();

        // items are enumerated one-by-one, so for each item
        // the modified items are observed first before the
        // next item from the source is seen
        var expectedItemsFromMiddleware = new[]
        {
            new TestItem(0),
            new TestItem(1),

            new TestItem(10),
            new TestItem(11),

            new TestItem(20),
            new TestItem(21),
        };

        var expectedFinalItems = new[]
        {
            new TestItem(3),
            new TestItem(13),
            new TestItem(23),
        };

        Assert.That(observations.ItemsFromMiddlewares, Is.EquivalentTo(expectedItemsFromMiddleware));
        Assert.That(response, Is.EquivalentTo(expectedFinalItems));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheCancellationTokens()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false) } };

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithMultipleMutatingMiddlewares>()
                    .AddConquerorStreamProducerMiddleware<MutatingTestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<MutatingTestStreamProducerMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer.ExecuteRequest(new(0), tokens.CancellationTokens[0]).Drain();

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromProducers, Is.EquivalentTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public async Task GivenPipelineThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var observedInstances = new List<TestService>();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithoutMiddlewares>()
                    .AddScoped<TestService>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline => observedInstances.Add(pipeline.ServiceProvider.GetRequiredService<TestService>()));

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(10), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(10), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(10), CancellationToken.None).Drain();

        Assert.That(observedInstances, Has.Count.EqualTo(3));
        Assert.That(observedInstances[1], Is.Not.SameAs(observedInstances[0]));
        Assert.That(observedInstances[2], Is.SameAs(observedInstances[0]));
    }

    [Test]
    public void GivenPipelineThatUsesUnregisteredMiddleware_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithoutMiddlewares>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline => pipeline.Use<TestStreamProducerMiddleware2>());

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => producer.ExecuteRequest(new(10), CancellationToken.None).Drain());

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestStreamProducerMiddleware2)));
    }

    [Test]
    public void GivenPipelineThatUsesUnregisteredMiddlewareWithConfiguration_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithoutMiddlewares>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline => pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new()));

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => producer.ExecuteRequest(new(10), CancellationToken.None).Drain());

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestStreamProducerMiddleware)));
    }

    [Test]
    public void GivenMiddlewareThatThrows_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithThrowingMiddleware>()
                    .AddConquerorStreamProducerMiddleware<ThrowingTestStreamProducerMiddleware>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => producer.ExecuteRequest(new(10), CancellationToken.None).Drain());

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenProducerDelegateWithSingleAppliedMiddleware_MiddlewareIsCalledWithRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducerDelegate<TestStreamingRequest, TestItem>((request, p, cancellationToken) =>
                                                                                        {
                                                                                            var obs = p.GetRequiredService<TestObservations>();
                                                                                            obs.RequestsFromProducers.Add(request);
                                                                                            obs.CancellationTokensFromProducers.Add(cancellationToken);
                                                                                            return AsyncEnumerableHelper.Of(new TestItem(request.Payload + 1));
                                                                                        },
                                                                                        pipeline => pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new()))
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await producer.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamProducerMiddleware) }));
    }

    [Test]
    public void InvalidMiddlewares()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorStreamProducerMiddleware<TestStreamProducerMiddlewareWithMultipleInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorStreamProducerMiddleware<TestStreamProducerMiddlewareWithMultipleInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorStreamProducerMiddleware(new TestStreamProducerMiddlewareWithMultipleInterfaces()));
    }

    private sealed record TestStreamingRequest(int Payload);

    private sealed record TestItem(int Payload);

    private sealed class TestStreamProducerWithSingleMiddleware(TestObservations observations) : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.RequestsFromProducers.Add(request);
            observations.CancellationTokensFromProducers.Add(cancellationToken);
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new());
        }
    }

    private sealed class TestStreamProducerWithSingleMiddlewareWithParameter(TestObservations observations) : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.RequestsFromProducers.Add(request);
            observations.CancellationTokensFromProducers.Add(cancellationToken);
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new() { Parameter = 10 });
        }
    }

    private sealed class TestStreamProducerWithMultipleMiddlewares(TestObservations observations) : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.RequestsFromProducers.Add(request);
            observations.CancellationTokensFromProducers.Add(cancellationToken);
            yield return new(0);
            yield return new(1);
            yield return new(2);
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new())
                        .Use<TestStreamProducerMiddleware2>();
        }
    }

    private sealed class TestStreamProducerWithoutMiddlewares(TestObservations observations) : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.RequestsFromProducers.Add(request);
            observations.CancellationTokensFromProducers.Add(cancellationToken);
            yield return new(0);
            yield return new(1);
            yield return new(2);
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IStreamProducerPipelineBuilder>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestStreamProducerWithRetryMiddleware(TestObservations observations) : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.RequestsFromProducers.Add(request);
            observations.CancellationTokensFromProducers.Add(cancellationToken);
            yield return new(0);
            yield return new(1);
            yield return new(2);
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamProducerRetryMiddleware>()
                        .Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new())
                        .Use<TestStreamProducerMiddleware2>();
        }
    }

    private sealed class TestStreamProducerWithMultipleMutatingMiddlewares(TestObservations observations) : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.RequestsFromProducers.Add(request);
            observations.CancellationTokensFromProducers.Add(cancellationToken);
            yield return new(0);
            yield return new(10);
            yield return new(20);
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<MutatingTestStreamProducerMiddleware>()
                        .Use<MutatingTestStreamProducerMiddleware2>();
        }
    }

    private sealed class TestStreamProducerWithPipelineConfigurationWithoutPipelineConfigurationInterface : IStreamProducer<TestStreamingRequest, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new(0);
            yield return new(1);
            yield return new(2);
        }

        // ReSharper disable once UnusedMember.Local
        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new() { Parameter = 10 });
        }
    }

    private sealed class TestStreamProducerWithThrowingMiddleware : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new(0);
            yield return new(1);
            yield return new(2);
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<ThrowingTestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new());
        }
    }

    private sealed record TestStreamProducerMiddlewareConfiguration
    {
        public int Parameter { get; set; }
    }

    private sealed class TestStreamProducerMiddleware(TestObservations observations) : IStreamProducerMiddleware<TestStreamProducerMiddlewareConfiguration>
    {
        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem, TestStreamProducerMiddlewareConfiguration> ctx)
            where TRequest : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.RequestsFromMiddlewares.Add(ctx.Request);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.ConfigurationFromMiddlewares.Add(ctx.Configuration);

            await foreach (var item in ctx.Next(ctx.Request, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class TestStreamProducerMiddleware2(TestObservations observations) : IStreamProducerMiddleware
    {
        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.RequestsFromMiddlewares.Add(ctx.Request);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            await foreach (var item in ctx.Next(ctx.Request, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class TestStreamProducerRetryMiddleware(TestObservations observations) : IStreamProducerMiddleware
    {
        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.RequestsFromMiddlewares.Add(ctx.Request);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            await foreach (var item in ctx.Next(ctx.Request, ctx.CancellationToken))
            {
                // discard items from first attempt
                _ = item;
            }

            await foreach (var item in ctx.Next(ctx.Request, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class MutatingTestStreamProducerMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : IStreamProducerMiddleware
    {
        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.RequestsFromMiddlewares.Add(ctx.Request);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            var request = ctx.Request;

            if (request is TestStreamingRequest r)
            {
                request = (TRequest)(object)new TestStreamingRequest(r.Payload + 1);
            }

            await foreach (var item in ctx.Next(request, cancellationTokensToUse.CancellationTokens[1]))
            {
                observations.ItemsFromMiddlewares.Add(item!);

                if (item is TestItem i)
                {
                    yield return (TItem)(object)new TestItem(i.Payload + 2);
                }
                else
                {
                    yield return item;
                }
            }
        }
    }

    private sealed class MutatingTestStreamProducerMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : IStreamProducerMiddleware
    {
        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.RequestsFromMiddlewares.Add(ctx.Request);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            var request = ctx.Request;

            if (request is TestStreamingRequest r)
            {
                request = (TRequest)(object)new TestStreamingRequest(r.Payload + 2);
            }

            await foreach (var item in ctx.Next(request, cancellationTokensToUse.CancellationTokens[2]))
            {
                observations.ItemsFromMiddlewares.Add(item!);

                if (item is TestItem i)
                {
                    yield return (TItem)(object)new TestItem(i.Payload + 1);
                }
                else
                {
                    yield return item;
                }
            }
        }
    }

    private sealed class ThrowingTestStreamProducerMiddleware(Exception exception) : IStreamProducerMiddleware<TestStreamProducerMiddlewareConfiguration>
    {
        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem, TestStreamProducerMiddlewareConfiguration> ctx)
            where TRequest : class
        {
            await Task.Yield();

            // should never match
            if (ctx.Request is string)
            {
                yield break;
            }

            throw exception;
        }
    }

    private sealed class TestStreamProducerMiddlewareWithMultipleInterfaces : IStreamProducerMiddleware<TestStreamProducerMiddlewareConfiguration>,
                                                                              IStreamProducerMiddleware
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class =>
            throw new InvalidOperationException("this middleware should never be called");

        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem, TestStreamProducerMiddlewareConfiguration> ctx)
            where TRequest : class =>
            throw new InvalidOperationException("this middleware should never be called");
    }

    private sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = [];

        public List<object> RequestsFromProducers { get; } = [];

        public List<object> RequestsFromMiddlewares { get; } = [];

        public List<object> ItemsFromMiddlewares { get; } = [];

        public List<CancellationToken> CancellationTokensFromProducers { get; } = [];

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = [];

        public List<object> ConfigurationFromMiddlewares { get; } = [];
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = [];
    }

    private sealed class TestService
    {
    }
}
