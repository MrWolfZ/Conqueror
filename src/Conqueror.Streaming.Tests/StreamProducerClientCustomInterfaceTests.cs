using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public abstract class StreamProducerClientCustomInterfaceTests
{
    [Test]
    public async Task GivenCustomProducerInterface_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<ITestStreamProducer>(services, b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>());

        _ = services.AddTransient<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamProducer>();

        var request = new TestStreamingRequest();

        _ = await client.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    protected abstract void AddStreamingRequestClient<TProducer>(IServiceCollection services,
                                                                 Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                 Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
        where TProducer : class, IStreamProducer;

    public sealed record TestStreamingRequest;

    public sealed record TestItem;

    // ReSharper disable once MemberCanBePrivate.Global (needs to be public for reflection)
    public interface ITestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>;

    private sealed class TestStreamProducerTransport(TestObservations observations) : IStreamProducerTransportClient
    {
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
        public List<object> Requests { get; } = [];
    }
}

[TestFixture]
public sealed class StreamProducerClientCustomInterfaceWithSyncFactoryTests : StreamProducerClientCustomInterfaceTests
{
    protected override void AddStreamingRequestClient<TProducer>(IServiceCollection services,
                                                                 Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                 Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
    {
        _ = services.AddConquerorStreamProducerClient<TProducer>(transportClientFactory, configurePipeline);
    }
}

[TestFixture]
public sealed class StreamProducerClientCustomInterfaceWithAsyncFactoryTests : StreamProducerClientCustomInterfaceTests
{
    protected override void AddStreamingRequestClient<TProducer>(IServiceCollection services,
                                                                 Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                 Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
    {
        _ = services.AddConquerorStreamProducerClient<TProducer>(async b =>
                                                                 {
                                                                     await Task.Delay(1);
                                                                     return transportClientFactory(b);
                                                                 },
                                                                 configurePipeline);
    }
}
