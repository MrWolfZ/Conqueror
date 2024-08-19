namespace Conqueror.Streaming.Tests;

[TestFixture]
public sealed class StreamProducerServiceCollectionConfigurationTests
{
    [Test]
    public void GivenRegisteredProducerType_AddingIdenticalProducerDoesNotThrow()
    {
        var services = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducer>();

        Assert.DoesNotThrow(() => services.AddConquerorStreamProducer<TestStreamProducer>());
    }

    [Test]
    public void GivenRegisteredProducerType_AddingIdenticalProducerOnlyKeepsOneRegistration()
    {
        var services = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducer>()
                                              .AddConquerorStreamProducer<TestStreamProducer>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestStreamProducer)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IStreamProducer<TestStreamingRequest, TestItem>)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredProducerType_AddingProducerWithSameRequestAndItemTypesKeepsBothRegistrations()
    {
        var services = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducer>()
                                              .AddConquerorStreamProducer<DuplicateTestStreamProducer>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestStreamProducer)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(DuplicateTestStreamProducer)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IStreamProducer<TestStreamingRequest, TestItem>)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredProducerType_AddingProducerWithSameRequestTypeAndDifferentResponseTypeThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducer>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamProducer<DuplicateTestStreamProducerWithDifferentResponseType>());
    }

    [Test]
    public void GivenProducerTypeWithInstanceFactory_AddedProducerCanBeResolvedFromInterface()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducer(_ => new TestStreamProducer())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenMiddlewareTypeWithInstanceFactory_AddedMiddlewareCanBeUsedInPipeline()
    {
        var provider = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducerWithMiddleware>()
                                              .AddConquerorStreamProducerMiddleware(_ => new TestStreamProducerMiddleware())
                                              .BuildServiceProvider();

        Assert.DoesNotThrowAsync(() => provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>().ExecuteRequest(new(), CancellationToken.None).Drain());
    }

    private sealed record TestStreamingRequest;

    private sealed record TestItem;

    private sealed record TestItem2;

    private sealed class TestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestStreamProducerWithDifferentResponseType : IStreamProducer<TestStreamingRequest, TestItem2>
    {
        public IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestStreamProducerWithMiddleware : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Of(new TestItem());

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline) => pipeline.Use<TestStreamProducerMiddleware>();
    }

    private sealed class TestStreamProducerMiddleware : IStreamProducerMiddleware
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            return ctx.Next(ctx.Request, ctx.CancellationToken);
        }
    }
}
