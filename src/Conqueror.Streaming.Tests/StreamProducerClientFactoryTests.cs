using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public abstract class StreamProducerClientFactoryTests
{
    [Test]
    public async Task GivenPlainProducerInterface_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamProducerClientFactory>();

        var client = CreateStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>());

        var request = new TestStreamingRequest();

        _ = await client.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    [Test]
    public async Task GivenCustomProducerInterface_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamProducerClientFactory>();

        var client = CreateStreamingRequestClient<ITestStreamProducer>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>());

        var request = new TestStreamingRequest();

        _ = await client.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    [Test]
    public async Task GivenPlainClientWithPipeline_PipelineIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddTransient<TestStreamProducerTransport>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamProducerClientFactory>();

        var client = CreateStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(clientFactory,
                                                                                                   b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>(),
                                                                                                   p => p.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new()));

        var request = new TestStreamingRequest();

        _ = await client.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamProducerMiddleware) }));
    }

    [Test]
    public async Task GivenCustomClientWithPipeline_PipelineIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddTransient<TestStreamProducerTransport>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamProducerClientFactory>();

        var client = CreateStreamingRequestClient<ITestStreamProducer>(clientFactory,
                                                                       b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>(),
                                                                       p => p.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(new()));

        var request = new TestStreamingRequest();

        _ = await client.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestStreamProducerMiddleware) }));
    }

    [Test]
    public void GivenCustomerProducerInterfaceWithExtraMethods_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamProducerClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateStreamingRequestClient<ITestStreamProducerWithExtraMethod>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>()));
    }

    [Test]
    public void GivenNonGenericStreamProducerInterface_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamProducerClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateStreamingRequestClient<INonGenericStreamProducer>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>()));
    }

    [Test]
    public void GivenConcreteStreamingRequestProducerType_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamProducerClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateStreamingRequestClient<TestStreamProducer>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>()));
    }

    [Test]
    public void GivenStreamProducerInterfaceThatImplementsMultipleOtherPlainStreamProducerInterfaces_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamProducerClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateStreamingRequestClient<ICombinedStreamProducer>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>()));
    }

    [Test]
    public void GivenStreamProducerInterfaceThatImplementsMultipleOtherCustomStreamProducerInterfaces_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreaming()
                    .AddTransient<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IStreamProducerClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateStreamingRequestClient<ICombinedCustomStreamProducer>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>()));
    }

    protected abstract TProducer CreateStreamingRequestClient<TProducer>(IStreamProducerClientFactory clientFactory,
                                                                         Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                         Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
        where TProducer : class, IStreamProducer;

    public sealed record TestStreamingRequest;

    public sealed record TestItem;

    public sealed record TestStreamingRequest2;

    public interface ITestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
    {
    }

    public interface ITestStreamProducer2 : IStreamProducer<TestStreamingRequest2, TestItem>
    {
    }

    public interface ITestStreamProducerWithExtraMethod : IStreamProducer<TestStreamingRequest, TestItem>
    {
        void ExtraMethod();
    }

    public interface ICombinedStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>, IStreamProducer<TestStreamingRequest2, TestItem>
    {
    }

    public interface ICombinedCustomStreamProducer : ITestStreamProducer, ITestStreamProducer2
    {
    }

    public interface INonGenericStreamProducer : IStreamProducer
    {
        void SomeMethod();
    }

    private sealed class TestStreamProducer : ITestStreamProducer
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed record TestStreamProducerMiddlewareConfiguration;

    private sealed class TestStreamProducerMiddleware : IStreamProducerMiddleware<TestStreamProducerMiddlewareConfiguration>
    {
        private readonly TestObservations observations;

        public TestStreamProducerMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem, TestStreamProducerMiddlewareConfiguration> ctx)
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

    private sealed class TestStreamProducerTransport : IStreamProducerTransportClient
    {
        private readonly TestObservations observations;

        public TestStreamProducerTransport(TestObservations observations)
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
public sealed class StreamProducerClientFactoryWithSyncFactoryTests : StreamProducerClientFactoryTests
{
    protected override TProducer CreateStreamingRequestClient<TProducer>(IStreamProducerClientFactory clientFactory,
                                                                         Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                         Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
    {
        return clientFactory.CreateStreamProducerClient<TProducer>(transportClientFactory, configurePipeline);
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class StreamProducerClientFactoryWithAsyncFactoryTests : StreamProducerClientFactoryTests
{
    protected override TProducer CreateStreamingRequestClient<TProducer>(IStreamProducerClientFactory clientFactory,
                                                                         Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                         Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
    {
        return clientFactory.CreateStreamProducerClient<TProducer>(async b =>
                                                                   {
                                                                       await Task.Delay(1);
                                                                       return transportClientFactory(b);
                                                                   },
                                                                   configurePipeline);
    }
}
