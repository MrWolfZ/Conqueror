namespace Conqueror.Streaming.Interactive.Tests;

[TestFixture]
public sealed class InteractiveStreamingServiceCollectionConfigurationTests
{
    [Test]
    public void GivenMultipleRegisteredIdenticalHandlerTypes_ConfiguringServiceCollectionDoesNotThrow()
    {
        var services = new ServiceCollection().AddConquerorInteractiveStreaming()
                                              .AddTransient<TestStreamingHandler>()
                                              .AddTransient<TestStreamingHandler>();

        Assert.DoesNotThrow(() => services.FinalizeConquerorRegistrations());
    }

    [Test]
    public void GivenMultipleRegisteredHandlerTypesForSameRequestAndItemTypes_ConfiguringServiceCollectionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().AddConquerorInteractiveStreaming()
                                              .AddTransient<TestStreamingHandler>()
                                              .AddTransient<DuplicateTestStreamingHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
    }

    [Test]
    public void GivenMultipleRegisteredHandlerTypesForSameRequestAndDifferentItemTypes_ConfiguringServiceCollectionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().AddConquerorInteractiveStreaming()
                                              .AddTransient<TestStreamingHandler>()
                                              .AddTransient<DuplicateTestStreamingHandlerWithDifferentItemType>();

        _ = Assert.Throws<InvalidOperationException>(() => services.FinalizeConquerorRegistrations());
    }

    [Test]
    public void GivenHandlerTypeWithInstanceFactory_ConfiguringServiceCollectionRecognizesHandler()
    {
        var provider = new ServiceCollection().AddConquerorInteractiveStreaming()
                                              .AddTransient(_ => new TestStreamingHandler())
                                              .FinalizeConquerorRegistrations()
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest, TestItem>>());
    }

    // TODO
    // [Test]
    // public void GivenMiddlewareTypeWithInstanceFactory_ConfiguringServiceCollectionRecognizesHandler()
    // {
    //     var provider = new ServiceCollection().AddConquerorInteractiveStreaming()
    //                                           .AddTransient<TestStreamingHandlerWithMiddleware>()
    //                                           .AddTransient(_ => new TestRequestMiddleware())
    //                                           .ConfigureConqueror()
    //                                           .BuildServiceProvider();
    //
    //     Assert.DoesNotThrow(() => provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest, TestItem>>().ExecuteRequest(new(), CancellationToken.None));
    // }

    private sealed record TestRequest;

    private sealed record TestItem;

    private sealed record TestItem2;

    private sealed class TestStreamingHandler : IInteractiveStreamingHandler<TestRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestStreamingHandler : IInteractiveStreamingHandler<TestRequest, TestItem>
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestStreamingHandlerWithDifferentItemType : IInteractiveStreamingHandler<TestRequest, TestItem2>
    {
        public IAsyncEnumerable<TestItem2> ExecuteRequest(TestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    // TODO
    // private sealed class TestStreamingHandlerWithMiddleware : IInteractiveStreamingHandler<TestRequest, TestItem>, IConfigureInteractiveStreamingPipeline
    // {
    //     public IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    //
    //     public static void ConfigurePipeline(IInteractiveStreamingPipelineBuilder pipeline) => pipeline.Use<TestStreamingMiddleware>();
    // }
    //
    // private sealed class TestStreamingMiddleware : IInteractiveStreamingMiddleware
    // {
    //     public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(InteractiveStreamingMiddlewareContext<TRequest, TItem> ctx)
    //         where TRequest : class
    //     {
    //         yield return ctx.Next(ctx.Request, ctx.CancellationToken);
    //     }
    // }
}
