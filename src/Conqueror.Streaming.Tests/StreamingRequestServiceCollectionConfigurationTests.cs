namespace Conqueror.Streaming.Tests;

[TestFixture]
public sealed class StreamingRequestServiceCollectionConfigurationTests
{
    [Test]
    public void GivenRegisteredHandlerType_AddingIdenticalHandlerDoesNotThrow()
    {
        var services = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        Assert.DoesNotThrow(() => services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>());
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingIdenticalHandlerOnlyKeepsOneRegistration()
    {
        var services = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                                              .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestStreamingRequestHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IStreamingRequestHandler<TestStreamingRequest, TestItem>)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingHandlerWithSameRequestAndItemTypesKeepsBothRegistrations()
    {
        var services = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                                              .AddConquerorStreamingRequestHandler<DuplicateTestStreamingRequestHandler>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestStreamingRequestHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(DuplicateTestStreamingRequestHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IStreamingRequestHandler<TestStreamingRequest, TestItem>)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingHandlerWithSameRequestTypeAndDifferentResponseTypeThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingRequestHandler<DuplicateTestStreamingRequestHandlerWithDifferentResponseType>());
    }

    [Test]
    public void GivenHandlerTypeWithInstanceFactory_AddedHandlerCanBeResolvedFromInterface()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandler(_ => new TestStreamingRequestHandler())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>());
    }

    [Test]
    public void GivenMiddlewareTypeWithInstanceFactory_AddedMiddlewareCanBeUsedInPipeline()
    {
        var provider = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithMiddleware>()
                                              .AddConquerorStreamingRequestMiddleware(_ => new TestStreamingRequestMiddleware())
                                              .BuildServiceProvider();

        Assert.DoesNotThrowAsync(() => provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>().ExecuteRequest(new(), CancellationToken.None).Drain());
    }

    private sealed record TestStreamingRequest;

    private sealed record TestItem;

    private sealed record TestItem2;

    private sealed class TestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestStreamingRequestHandlerWithDifferentResponseType : IStreamingRequestHandler<TestStreamingRequest, TestItem2>
    {
        public IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestStreamingRequestHandlerWithMiddleware : IStreamingRequestHandler<TestStreamingRequest, TestItem>, IConfigureStreamingRequestPipeline
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, CancellationToken cancellationToken = default) => AsyncEnumerableHelper.Of(new TestItem());

        public static void ConfigurePipeline(IStreamingRequestPipelineBuilder pipeline) => pipeline.Use<TestStreamingRequestMiddleware>();
    }

    private sealed class TestStreamingRequestMiddleware : IStreamingRequestMiddleware
    {
        public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            return ctx.Next(ctx.Request, ctx.CancellationToken);
        }
    }
}
