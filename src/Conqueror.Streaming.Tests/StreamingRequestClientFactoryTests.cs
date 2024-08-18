using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public abstract class StreamingRequestClientFactoryTests
{
    [Test]
    public async Task GivenPlainHandlerInterface_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamingRequestTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamingRequestClientFactory>();

        var client = CreateStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamingRequestTransport>());

        var request = new TestStreamingRequest();

        _ = await client.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    [Test]
    public async Task GivenCustomHandlerInterface_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamingRequestTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamingRequestClientFactory>();

        var client = CreateStreamingRequestClient<ITestStreamingRequestHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamingRequestTransport>());

        var request = new TestStreamingRequest();

        _ = await client.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    [Test]
    public async Task GivenPlainClientWithPipeline_PipelineIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddTransient<TestStreamingRequestTransport>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamingRequestClientFactory>();

        var client = CreateStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(clientFactory,
                                                                                                            b => b.ServiceProvider.GetRequiredService<TestStreamingRequestTransport>(),
                                                                                                            p => p.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new()));

        var request = new TestStreamingRequest();

        _ = await client.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamingRequestMiddleware) }));
    }

    [Test]
    public async Task GivenCustomClientWithPipeline_PipelineIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddTransient<TestStreamingRequestTransport>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamingRequestClientFactory>();

        var client = CreateStreamingRequestClient<ITestStreamingRequestHandler>(clientFactory,
                                                                                b => b.ServiceProvider.GetRequiredService<TestStreamingRequestTransport>(),
                                                                                p => p.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new()));

        var request = new TestStreamingRequest();

        _ = await client.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamingRequestMiddleware) }));
    }

    [Test]
    public void GivenCustomerHandlerInterfaceWithExtraMethods_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamingRequestTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamingRequestClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateStreamingRequestClient<ITestStreamingRequestHandlerWithExtraMethod>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamingRequestTransport>()));
    }

    [Test]
    public void GivenNonGenericStreamingRequestHandlerInterface_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamingRequestTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamingRequestClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateStreamingRequestClient<INonGenericStreamingRequestHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamingRequestTransport>()));
    }

    [Test]
    public void GivenConcreteStreamingRequestHandlerType_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamingRequestTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamingRequestClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateStreamingRequestClient<TestStreamingRequestHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamingRequestTransport>()));
    }

    [Test]
    public void GivenStreamingRequestHandlerInterfaceThatImplementsMultipleOtherPlainStreamingRequestHandlerInterfaces_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamingRequestTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamingRequestClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateStreamingRequestClient<ICombinedStreamingRequestHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamingRequestTransport>()));
    }

    [Test]
    public void GivenStreamingRequestHandlerInterfaceThatImplementsMultipleOtherCustomStreamingRequestHandlerInterfaces_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamingRequestTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamingRequestClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateStreamingRequestClient<ICombinedCustomStreamingRequestHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamingRequestTransport>()));
    }

    protected abstract THandler CreateStreamingRequestClient<THandler>(IStreamingRequestClientFactory clientFactory,
                                                                       Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                                       Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
        where THandler : class, IStreamingRequestHandler;

    public sealed record TestStreamingRequest;

    public sealed record TestItem;

    public sealed record TestStreamingRequest2;

    public interface ITestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
    }

    public interface ITestStreamingRequestHandler2 : IStreamingRequestHandler<TestStreamingRequest2, TestItem>
    {
    }

    public interface ITestStreamingRequestHandlerWithExtraMethod : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        void ExtraMethod();
    }

    public interface ICombinedStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>, IStreamingRequestHandler<TestStreamingRequest2, TestItem>
    {
    }

    public interface ICombinedCustomStreamingRequestHandler : ITestStreamingRequestHandler, ITestStreamingRequestHandler2
    {
    }

    public interface INonGenericStreamingRequestHandler : IStreamingRequestHandler
    {
        void SomeMethod();
    }

    private sealed class TestStreamingRequestHandler : ITestStreamingRequestHandler
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed record TestStreamingRequestMiddlewareConfiguration;

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

            await foreach (var item in ctx.Next(ctx.Request, ctx.CancellationToken))
            {
                yield return item;
            }
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
            observations.Requests.Add(request);

            yield return (TItem)(object)new TestItem();
        }
    }

    private sealed class TestObservations
    {
        public List<object> Requests { get; } = new();

        public List<Type> MiddlewareTypes { get; } = new();
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class StreamingRequestClientFactoryWithSyncFactoryTests : StreamingRequestClientFactoryTests
{
    protected override THandler CreateStreamingRequestClient<THandler>(IStreamingRequestClientFactory clientFactory,
                                                                       Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                                       Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
    {
        return clientFactory.CreateStreamingRequestClient<THandler>(transportClientFactory, configurePipeline);
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class StreamingRequestClientFactoryWithAsyncFactoryTests : StreamingRequestClientFactoryTests
{
    protected override THandler CreateStreamingRequestClient<THandler>(IStreamingRequestClientFactory clientFactory,
                                                                       Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                                       Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
    {
        return clientFactory.CreateStreamingRequestClient<THandler>(async b =>
                                                                    {
                                                                        await Task.Delay(1);
                                                                        return transportClientFactory(b);
                                                                    },
                                                                    configurePipeline);
    }
}
