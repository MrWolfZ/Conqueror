using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public abstract class StreamingRequestClientCustomInterfaceTests
{
    [Test]
    public async Task GivenCustomHandlerInterface_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<ITestStreamingRequestHandler>(services, b => b.ServiceProvider.GetRequiredService<TestStreamingRequestTransport>());

        _ = services.AddTransient<TestStreamingRequestTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        var request = new TestStreamingRequest();

        _ = await client.ExecuteRequest(request, CancellationToken.None).Drain();

        Assert.That(observations.Requests, Is.EquivalentTo(new[] { request }));
    }

    protected abstract void AddStreamingRequestClient<THandler>(IServiceCollection services,
                                                                Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                                Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
        where THandler : class, IStreamingRequestHandler;

    public sealed record TestStreamingRequest;

    public sealed record TestItem;

    // ReSharper disable once MemberCanBePrivate.Global (needs to be public for reflection)
    public interface ITestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>;

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
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class StreamingRequestClientCustomInterfaceWithSyncFactoryTests : StreamingRequestClientCustomInterfaceTests
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
public sealed class StreamingRequestClientCustomInterfaceWithAsyncFactoryTests : StreamingRequestClientCustomInterfaceTests
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
