namespace Conqueror.Tests.Messaging;

public sealed partial class MessageMiddlewareConfigurationTests
{
    [Test]
    public async Task GivenMiddlewareWithParameter_WhenPipelineConfigurationUpdatesParameter_TheMiddlewareExecutesWithUpdatedParameter()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorMessageHandler<TestMessageHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<TestMessage.IPipeline>>(pipeline =>
        {
            _ = pipeline.Use(new TestMessageMiddleware<TestMessage, TestMessageResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestMessageMiddleware<TestMessage, TestMessageResponse>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageClients>()
                              .For(TestMessage.T);

        _ = await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EqualTo(new[] { 20 }));
    }

    [Test]
    public async Task GivenMultipleMiddlewareOfSameType_WhenPipelineConfigurationRuns_AllMiddlewaresAreUpdated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorMessageHandler<TestMessageHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<TestMessage.IPipeline>>(pipeline =>
        {
            _ = pipeline.Use(new TestMessageMiddleware<TestMessage, TestMessageResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });
            _ = pipeline.Use(new TestMessageMiddleware<TestMessage, TestMessageResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 30 });
            _ = pipeline.Use(new TestMessageMiddleware<TestMessage, TestMessageResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 50 });

            _ = pipeline.Configure<TestMessageMiddleware<TestMessage, TestMessageResponse>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageClients>()
                              .For(TestMessage.T);

        _ = await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EqualTo(new[] { 20, 40, 60 }));
    }

    [Test]
    public async Task GivenMiddlewareWithBaseClass_WhenPipelineConfiguresBaseClass_TheMiddlewareIsConfigured()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorMessageHandler<TestMessageHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<TestMessage.IPipeline>>(pipeline =>
        {
            _ = pipeline.Use(new TestMessageMiddlewareSub<TestMessage, TestMessageResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestMessageMiddlewareBase<TestMessage, TestMessageResponse>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageClients>()
                              .For(TestMessage.T);

        _ = await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EqualTo(new[] { 20 }));
    }

    [Test]
    public async Task GivenUnusedMiddleware_ConfiguringMiddlewareThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorMessageHandler<TestMessageHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<TestMessage.IPipeline>>(pipeline => { _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestMessageMiddleware<TestMessage, TestMessageResponse>>(c => c.Parameter += 10)); });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IMessageClients>()
                              .For(TestMessage.T);

        _ = await handler.Handle(new(), CancellationToken.None);
    }

    [Message<TestMessageResponse>]
    private sealed partial record TestMessage;

    private sealed record TestMessageResponse;

    private sealed class TestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }

        public static void ConfigurePipeline(TestMessage.IPipeline pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<TestMessage.IPipeline>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestMessageMiddleware<TMessage, TResponse>(TestObservations observations) : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public int Parameter { get; set; }

        public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            return await ctx.Next(ctx.Message, ctx.CancellationToken);
        }
    }

    private sealed class TestMessageMiddlewareSub<TMessage, TResponse>(TestObservations observations) : TestMessageMiddlewareBase<TMessage, TResponse>(observations)
        where TMessage : class, IMessage<TMessage, TResponse>;

    private abstract class TestMessageMiddlewareBase<TMessage, TResponse>(TestObservations observations) : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public int Parameter { get; set; }

        public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            return await ctx.Next(ctx.Message, ctx.CancellationToken);
        }
    }

    private sealed class TestObservations
    {
        public List<int> Parameters { get; } = [];
    }
}
