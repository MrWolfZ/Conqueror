namespace Conqueror.Streaming.Tests;

[TestFixture]
public sealed class StreamingServiceCollectionConfigurationTests
{
    [Test]
    public void GivenMultipleRegisteredIdenticalHandlerTypes_ConfiguringServiceCollectionDoesNotThrow()
    {
        var services = new ServiceCollection().AddConquerorStreaming()
                                              .AddTransient<TestStreamingHandler>()
                                              .AddTransient<TestStreamingHandler>();

        Assert.DoesNotThrow(() => services.FinalizeConquerorRegistrations());
    }

    [Test]
    public void GivenMultipleRegisteredHandlerTypesForSameRequestAndItemTypes_ConfiguringServiceCollectionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().AddConquerorStreaming()
                                              .AddTransient<TestStreamingHandler>()
                                              .AddTransient<DuplicateTestStreamingHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
    }

    [Test]
    public void GivenMultipleRegisteredHandlerTypesForSameRequestAndDifferentItemTypes_ConfiguringServiceCollectionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().AddConquerorStreaming()
                                              .AddTransient<TestStreamingHandler>()
                                              .AddTransient<DuplicateTestStreamingHandlerWithDifferentItemType>();

        _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
    }

    [Test]
    public void GivenHandlerTypeWithInstanceFactory_ConfiguringServiceCollectionRecognizesHandler()
    {
        var provider = new ServiceCollection().AddConquerorStreaming()
                                              .AddTransient(_ => new TestStreamingHandler())
                                              .FinalizeConquerorRegistrations()
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingHandler<TestRequest, TestItem>>());
    }

    // TODO
    // [Test]
    // public void GivenMiddlewareTypeWithInstanceFactory_ConfiguringServiceCollectionRecognizesHandler()
    // {
    //     var provider = new ServiceCollection().AddConquerorStreaming()
    //                                           .AddTransient<TestStreamingHandlerWithMiddleware>()
    //                                           .AddTransient(_ => new TestRequestMiddleware())
    //                                           .ConfigureConqueror()
    //                                           .BuildServiceProvider();
    //
    //     Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingHandler<TestRequest, TestItem>>().ExecuteRequest(new(), CancellationToken.None));
    // }

    private sealed record TestRequest;

    private sealed record TestItem;

    private sealed record TestItem2;

    private sealed class TestStreamingHandler : IStreamingHandler<TestRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestStreamingHandler : IStreamingHandler<TestRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestStreamingHandlerWithDifferentItemType : IStreamingHandler<TestRequest, TestItem2>
    {
        public IAsyncEnumerable<TestItem2> ExecuteRequest(TestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    // TODO
    // private sealed class TestStreamingHandlerWithMiddleware : IStreamingHandler<TestRequest, TestItem>, IConfigureStreamingPipeline
    // {
    //     public IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    //
    //     public static void ConfigurePipeline(IStreamingPipelineBuilder pipeline) => pipeline.Use<TestStreamingMiddleware>();
    // }
    //
    // private sealed class TestStreamingMiddleware : IStreamingMiddleware
    // {
    //     public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingMiddlewareContext<TRequest, TItem> ctx)
    //         where TRequest : class
    //     {
    //         yield return ctx.Next(ctx.Request, ctx.CancellationToken);
    //     }
    // }
}
