using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public abstract class StreamProducerClientFunctionalityTests
{
    [Test]
    public async Task GivenStreamingRequest_TransportReceivesRequest()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services, b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>());

        _ = services.AddTransient<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        _ = await client.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    [Test]
    public async Task GivenCancellationToken_TransportReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services, b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>());

        _ = services.AddTransient<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        using var tokenSource = new CancellationTokenSource();

        _ = await client.ExecuteRequest(new(10), tokenSource.Token).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenCancellationTokenViaAsyncEnumerableExtension_TransportReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services, b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>());

        _ = services.AddTransient<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        using var tokenSource = new CancellationTokenSource();

        _ = await client.ExecuteRequest(new(10)).WithCancellation(tokenSource.Token).Drain();

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenStreamingRequest_TransportReturnsResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services, b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>());

        _ = services.AddTransient<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var request = new TestStreamingRequest(10);

        var response = await client.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(response, Is.EquivalentTo(new[] { new TestItem(request.Payload + 1), new TestItem(request.Payload + 2), new TestItem(request.Payload + 3) }));
    }

    [Test]
    public async Task GivenScopedFactory_TransportIsResolvedOnSameScope()
    {
        var seenInstances = new List<TestStreamProducerTransport>();

        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services, b =>
        {
            var transport = b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>();
            seenInstances.Add(transport);
            return transport;
        });

        _ = services.AddScoped<TestStreamProducerTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var client1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var client2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var client3 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await client1.ExecuteRequest(new(10), CancellationToken.None).Drain();
        _ = await client2.ExecuteRequest(new(10), CancellationToken.None).Drain();
        _ = await client3.ExecuteRequest(new(10), CancellationToken.None).Drain();

        Assert.That(seenInstances, Has.Count.EqualTo(3));
        Assert.That(seenInstances[1], Is.SameAs(seenInstances[0]));
        Assert.That(seenInstances[2], Is.Not.SameAs(seenInstances[0]));
    }

    [Test]
    public void GivenExceptionInTransport_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services, b => b.ServiceProvider.GetRequiredService<ThrowingTestStreamProducerTransport>());

        _ = services.AddTransient<ThrowingTestStreamProducerTransport>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => producer.ExecuteRequest(new(10), CancellationToken.None).Drain());

        Assert.That(thrownException, Is.SameAs(exception));
    }

    protected abstract void AddStreamingRequestClient<TProducer>(IServiceCollection services,
                                                                 Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                 Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
        where TProducer : class, IStreamProducer;

    private sealed record TestStreamingRequest(int Payload);

    private sealed record TestItem(int Payload);

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
            observations.CancellationTokens.Add(cancellationToken);

            var req = (TestStreamingRequest)(object)request;
            yield return (TItem)(object)new TestItem(req.Payload + 1);
            yield return (TItem)(object)new TestItem(req.Payload + 2);
            yield return (TItem)(object)new TestItem(req.Payload + 3);
        }
    }

    private sealed class ThrowingTestStreamProducerTransport : IStreamProducerTransportClient
    {
        private readonly Exception exception;

        public ThrowingTestStreamProducerTransport(Exception exception)
        {
            this.exception = exception;
        }

        public async IAsyncEnumerable<TItem> ExecuteRequest<TRequest, TItem>(TRequest request,
                                                                             IServiceProvider serviceProvider,
                                                                             [EnumeratorCancellation] CancellationToken cancellationToken)
            where TRequest : class
        {
            await Task.Yield();

            if (request != null)
            {
                throw exception;
            }

            yield break;
        }
    }

    private sealed class TestObservations
    {
        public List<object> Requests { get; } = new();

        public List<CancellationToken> CancellationTokens { get; } = new();
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class StreamProducerClientFunctionalityWithSyncFactoryTests : StreamProducerClientFunctionalityTests
{
    protected override void AddStreamingRequestClient<TProducer>(IServiceCollection services,
                                                                 Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                 Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
    {
        _ = services.AddConquerorStreamProducerClient<TProducer>(transportClientFactory, configurePipeline);
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class StreamProducerClientFunctionalityWithAsyncFactoryTests : StreamProducerClientFunctionalityTests
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
