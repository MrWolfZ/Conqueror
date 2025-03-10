// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

using System.ComponentModel;

#pragma warning disable SA1302
#pragma warning disable CA1715

namespace Conqueror.Tests;

public sealed partial class MessageTypeGenerationTests
{
    [Test]
    public async Task GivenMessageTypeWithExplicitImplementations_WhenUsingHandler_ItWorks()
    {
        var services = new ServiceCollection();
        var provider = services.AddConqueror()
                               .AddConquerorMessageHandler<TestMessageHandler>()
                               .AddConquerorMessageHandler<TestMessageWithoutResponseHandler>()
                               .BuildServiceProvider();

        var messageClients = provider.GetRequiredService<IMessageClients>();

        var result = await messageClients.For<TestMessage.IHandler>()
                                         .WithPipeline(p => p.UseTest().UseTest())
                                         .WithTransport(b => b.UseInProcess())
                                         .Handle(new());

        Assert.That(result, Is.Not.Null);

        await messageClients.For<TestMessageWithoutResponse.IHandler>()
                            .WithPipeline(p => p.UseTest().UseTest())
                            .WithTransport(b => b.UseInProcess())
                            .Handle(new());
    }

    public sealed partial record TestMessage : IMessage<TestMessageResponse>;

    public sealed record TestMessageResponse;

    // generated
    public sealed partial record TestMessage
    {
        public interface IHandler : IGeneratedMessageHandler<TestMessage, TestMessageResponse, IPipeline>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            static THandlerInterface? IGeneratedMessageHandler.Create<THandlerInterface>(IMessageHandlerProxyFactory proxyFactory)
                where THandlerInterface : class
                => new Adapter(proxyFactory) as THandlerInterface;

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter(IMessageHandlerProxyFactory proxyFactory)
                : GeneratedMessageHandlerAdapter<TestMessage, TestMessageResponse>(proxyFactory.CreateProxy<TestMessage, TestMessageResponse>()),
                  IHandler;
        }

        public interface IPipeline : IGeneratedMessagePipeline<TestMessage, TestMessageResponse, IPipeline>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            static IPipeline IGeneratedMessagePipeline<TestMessage, TestMessageResponse, IPipeline>.Create(
                IMessagePipeline<TestMessage, TestMessageResponse> wrapped)
                => new Adapter(wrapped);

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter(IMessagePipeline<TestMessage, TestMessageResponse> wrapped)
                : GeneratedMessagePipelineAdapter<TestMessage, TestMessageResponse>(wrapped),
                  IPipeline;
        }
    }

    private sealed class TestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return new();
        }

        public static void ConfigurePipeline(TestMessage.IPipeline pipeline) =>
            pipeline.UseTest().UseTest();
    }

    public sealed partial record TestMessageWithoutResponse : IMessage;

    // generated
    public sealed partial record TestMessageWithoutResponse
    {
        public interface IHandler : IGeneratedMessageHandler<TestMessageWithoutResponse, IPipeline>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            static THandlerInterface? IGeneratedMessageHandler.Create<THandlerInterface>(IMessageHandlerProxyFactory proxyFactory)
                where THandlerInterface : class
                => new Adapter(proxyFactory) as THandlerInterface;

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter(IMessageHandlerProxyFactory proxyFactory)
                : GeneratedMessageHandlerAdapter<TestMessageWithoutResponse>(proxyFactory.CreateProxy<TestMessageWithoutResponse, UnitMessageResponse>()),
                  IHandler;
        }

        public interface IPipeline : IGeneratedMessagePipeline<TestMessageWithoutResponse, UnitMessageResponse, IPipeline>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            static IPipeline IGeneratedMessagePipeline<TestMessageWithoutResponse, UnitMessageResponse, IPipeline>.Create(
                IMessagePipeline<TestMessageWithoutResponse, UnitMessageResponse> wrapped)
                => new Adapter(wrapped);

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter(IMessagePipeline<TestMessageWithoutResponse, UnitMessageResponse> wrapped)
                : GeneratedMessagePipelineAdapter<TestMessageWithoutResponse, UnitMessageResponse>(wrapped),
                  IPipeline;
        }
    }

    private sealed class TestMessageWithoutResponseHandler : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        public static void ConfigurePipeline(TestMessageWithoutResponse.IPipeline pipeline) =>
            pipeline.UseTest().UseTest();
    }
}

public static class MessageTypeGenerationTestsPipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseTest<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TResponse>
    {
        return pipeline.Use(ctx => ctx.Next(ctx.Message, ctx.CancellationToken));
    }
}
