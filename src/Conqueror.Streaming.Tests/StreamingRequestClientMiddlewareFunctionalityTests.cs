using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public abstract class StreamingRequestClientMiddlewareFunctionalityTests
{
    [Test]
    public async Task GivenClientWithNoMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services, CreateTransport);

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.Empty);
        Assert.That(observations.MiddlewareTypes, Is.Empty);
    }

    [Test]
    public async Task GivenClientWithSingleAppliedMiddleware_MiddlewareIsCalledWithRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new()));

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamingRequestMiddleware) }));
    }

    [Test]
    public async Task GivenClientWithSingleAppliedMiddlewareWithParameter_MiddlewareIsCalledWithConfiguration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new() { Parameter = 10 }));

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler.ExecuteRequest(new(10), CancellationToken.None).Drain();

        Assert.That(observations.ConfigurationFromMiddlewares, Is.EquivalentTo(new[] { new TestStreamingRequestMiddlewareConfiguration { Parameter = 10 } }));
    }

    [Test]
    public async Task GivenClientWithMultipleAppliedMiddlewares_MiddlewaresAreCalledWithRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new())
                                                                                                  .Use<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamingRequestMiddleware), typeof(TestStreamingRequestMiddleware2) }));
    }

    [Test]
    public async Task GivenClientWithSameMiddlewareAppliedMultipleTimes_MiddlewareIsCalledWithRequestMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware2>()
                                                                                                  .Use<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamingRequestMiddleware2), typeof(TestStreamingRequestMiddleware2) }));
    }

    [Test]
    public async Task GivenClientWithAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new())
                                                                                                  .Use<TestStreamingRequestMiddleware2>()
                                                                                                  .Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new())
                                                                                                  .Without<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamingRequestMiddleware), typeof(TestStreamingRequestMiddleware) }));
    }

    [Test]
    public async Task GivenClientWithAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware2>()
                                                                                                  .Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new())
                                                                                                  .Use<TestStreamingRequestMiddleware2>()
                                                                                                  .Without<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamingRequestMiddleware2), typeof(TestStreamingRequestMiddleware2) }));
    }

    [Test]
    public async Task GivenClientWithMultipleAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new())
                                                                                                  .Use<TestStreamingRequestMiddleware2>()
                                                                                                  .Use<TestStreamingRequestMiddleware2>()
                                                                                                  .Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new())
                                                                                                  .Without<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamingRequestMiddleware), typeof(TestStreamingRequestMiddleware) }));
    }

    [Test]
    public async Task GivenClientWithMultipleAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware2>()
                                                                                                  .Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new())
                                                                                                  .Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new())
                                                                                                  .Use<TestStreamingRequestMiddleware2>()
                                                                                                  .Without<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamingRequestMiddleware2), typeof(TestStreamingRequestMiddleware2) }));
    }

    [Test]
    public async Task GivenPipelineWithExistingMiddleware_WhenAddingSameMiddlewareAgainAfterRemovingPreviousMiddleware_MiddlewareIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware2>()
                                                                                                  .Without<TestStreamingRequestMiddleware2>()
                                                                                                  .Use<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamingRequestMiddleware2) }));
    }

    [Test]
    public async Task GivenPipelineWithExistingMiddlewareWithConfiguration_WhenAddingSameMiddlewareAgainAfterRemovingPreviousMiddleware_MiddlewareIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new())
                                                                                                  .Without<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>()
                                                                                                  .Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new()));

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamingRequestMiddleware) }));
    }

    [Test]
    public async Task GivenClientWithRetryMiddleware_MiddlewaresAreCalledMultipleTimesWithRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestRetryMiddleware>()
                                                                                                  .Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new())
                                                                                                  .Use<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestRetryMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await handler.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request, request, request, request, request }));
        Assert.That(observations.MiddlewareTypes,
                    Is.EquivalentTo(new[]
                    {
                        typeof(TestStreamingRequestRetryMiddleware), typeof(TestStreamingRequestMiddleware), typeof(TestStreamingRequestMiddleware2), typeof(TestStreamingRequestMiddleware), typeof(TestStreamingRequestMiddleware2),
                    }));
    }

    [Test]
    public async Task GivenCancellationToken_MiddlewaresReceiveCancellationTokenWhenCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new())
                                                                                                  .Use<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.ExecuteRequest(new(10), tokenSource.Token).Drain();

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(new[] { tokenSource.Token, tokenSource.Token }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<MutatingTestStreamingRequestMiddleware>()
                                                                                                  .Use<MutatingTestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<MutatingTestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<MutatingTestStreamingRequestMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler.ExecuteRequest(new(0), CancellationToken.None).Drain();

        var request1 = new TestStreamingRequest(0);
        var request2 = new TestStreamingRequest(1);
        var request3 = new TestStreamingRequest(3);

        Assert.That(observations.RequestsFromMiddlewares, Is.EquivalentTo(new[] { request1, request2 }));
        Assert.That(observations.RequestsFromTransports, Is.EquivalentTo(new[] { request3 }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<MutatingTestStreamingRequestMiddleware>()
                                                                                                  .Use<MutatingTestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<MutatingTestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<MutatingTestStreamingRequestMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var response = await handler.ExecuteRequest(new(0), CancellationToken.None).Drain();

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

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<MutatingTestStreamingRequestMiddleware>()
                                                                                                  .Use<MutatingTestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<MutatingTestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<MutatingTestStreamingRequestMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler.ExecuteRequest(new(0), tokens.CancellationTokens[0]).Drain();

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromTransports, Is.EquivalentTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public async Task GivenPipelineThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var observedInstances = new List<TestService>();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => observedInstances.Add(p.ServiceProvider.GetRequiredService<TestService>()));

        _ = services.AddScoped<TestService>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler1.ExecuteRequest(new(10), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(10), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(10), CancellationToken.None).Drain();

        Assert.That(observedInstances, Has.Count.EqualTo(3));
        Assert.That(observedInstances[1], Is.Not.SameAs(observedInstances[0]));
        Assert.That(observedInstances[2], Is.SameAs(observedInstances[0]));
    }

    [Test]
    public void GivenPipelineThatUsesUnregisteredMiddleware_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware2>());

        _ = services.AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteRequest(new(10), CancellationToken.None).Drain());

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestStreamingRequestMiddleware2)));
    }

    [Test]
    public void GivenPipelineThatUsesUnregisteredMiddlewareWithConfiguration_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new()));

        _ = services.AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteRequest(new(10), CancellationToken.None).Drain());

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestStreamingRequestMiddleware)));
    }

    [Test]
    public void GivenMiddlewareThatThrows_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<ThrowingTestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new()));

        _ = services.AddConquerorStreamingRequestMiddleware<ThrowingTestStreamingRequestMiddleware>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteRequest(new(10), CancellationToken.None).Drain());

        Assert.That(thrownException, Is.SameAs(exception));
    }

    protected abstract void AddStreamingRequestClient<THandler>(IServiceCollection services,
                                                                Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                                Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
        where THandler : class, IStreamingRequestHandler;

    private static IStreamingRequestTransportClient CreateTransport(IStreamingRequestTransportClientBuilder builder)
    {
        return new TestStreamingRequestTransport(builder.ServiceProvider.GetRequiredService<TestObservations>());
    }

    private sealed record TestStreamingRequest(int Payload);

    private sealed record TestItem(int Payload);

    private sealed record TestStreamingRequestMiddlewareConfiguration
    {
        public int Parameter { get; set; }
    }

    private sealed class TestStreamingRequestMiddleware : IStreamingRequestMiddleware<TestStreamingRequestMiddlewareConfiguration>
    {
        private readonly TestObservations observations;

        public TestStreamingRequestMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem, TestStreamingRequestMiddlewareConfiguration> ctx)
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

    private sealed class TestStreamingRequestMiddleware2 : IStreamingRequestMiddleware
    {
        private readonly TestObservations observations;

        public TestStreamingRequestMiddleware2(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem> ctx)
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

    private sealed class TestStreamingRequestRetryMiddleware : IStreamingRequestMiddleware
    {
        private readonly TestObservations observations;

        public TestStreamingRequestRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem> ctx)
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

    private sealed class MutatingTestStreamingRequestMiddleware : IStreamingRequestMiddleware
    {
        private readonly CancellationTokensToUse cancellationTokensToUse;
        private readonly TestObservations observations;

        public MutatingTestStreamingRequestMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        {
            this.observations = observations;
            this.cancellationTokensToUse = cancellationTokensToUse;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem> ctx)
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

    private sealed class MutatingTestStreamingRequestMiddleware2 : IStreamingRequestMiddleware
    {
        private readonly CancellationTokensToUse cancellationTokensToUse;
        private readonly TestObservations observations;

        public MutatingTestStreamingRequestMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        {
            this.observations = observations;
            this.cancellationTokensToUse = cancellationTokensToUse;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem> ctx)
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

    private sealed class ThrowingTestStreamingRequestMiddleware : IStreamingRequestMiddleware<TestStreamingRequestMiddlewareConfiguration>
    {
        private readonly Exception exception;

        public ThrowingTestStreamingRequestMiddleware(Exception exception)
        {
            this.exception = exception;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem, TestStreamingRequestMiddlewareConfiguration> ctx)
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

    private sealed class TestStreamingRequestTransport : IStreamingRequestTransportClient
    {
        private readonly TestObservations observations;

        public TestStreamingRequestTransport(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> ExecuteRequest<TRequest, TItem>(TRequest request,
                                                                             IServiceProvider serviceProvider,
                                                                             [EnumeratorCancellation] CancellationToken cancellationToken)
            where TRequest : class
        {
            await Task.Yield();

            observations.RequestsFromTransports.Add(request);
            observations.CancellationTokensFromTransports.Add(cancellationToken);

            yield return (TItem)(object)new TestItem(0);
            yield return (TItem)(object)new TestItem(10);
            yield return (TItem)(object)new TestItem(20);
        }
    }

    private sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = new();

        public List<object> RequestsFromTransports { get; } = new();

        public List<object> RequestsFromMiddlewares { get; } = new();

        public List<object> ItemsFromMiddlewares { get; } = new();

        public List<CancellationToken> CancellationTokensFromTransports { get; } = new();

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = new();

        public List<object> ConfigurationFromMiddlewares { get; } = new();
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = new();
    }

    private sealed class TestService
    {
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class StreamingRequestClientMiddlewareFunctionalityWithSyncFactoryTests : StreamingRequestClientMiddlewareFunctionalityTests
{
    protected override void AddStreamingRequestClient<THandler>(IServiceCollection services,
                                                                Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                                Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
    {
        _ = services.AddConquerorStreamingRequestClient<THandler>(transportClientFactory, configurePipeline);
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class StreamingRequestClientMiddlewareFunctionalityWithAsyncFactoryTests : StreamingRequestClientMiddlewareFunctionalityTests
{
    protected override void AddStreamingRequestClient<THandler>(IServiceCollection services,
                                                                Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                                Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
    {
        _ = services.AddConquerorStreamingRequestClient<THandler>(async b =>
                                                                  {
                                                                      await Task.Delay(1);
                                                                      return transportClientFactory(b);
                                                                  },
                                                                  configurePipeline);
    }
}
