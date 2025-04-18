// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

using System.ComponentModel;

#pragma warning disable SA1302
#pragma warning disable CA1715

namespace Conqueror.Tests.Messaging;

public sealed partial class MessageTypeGenerationTests
{
    [Test]
    public async Task GivenMessageTypeWithExplicitImplementations_WhenUsingHandler_ItWorks()
    {
        var services = new ServiceCollection();
        var provider = services.AddConqueror()
                               .AddMessageHandler<TestMessageHandler>()
                               .AddMessageHandler<TestMessageWithoutResponseHandler>()
                               .BuildServiceProvider();

        var messageClients = provider.GetRequiredService<IMessageClients>();

        var result = await messageClients.For(TestMessage.T)
                                         .WithPipeline(p => p.UseTest().UseTest())
                                         .WithTransport(b => b.UseInProcess())
                                         .AsIHandler()
                                         .Handle(new(10));

        Assert.That(result, Is.Not.Null);

        await messageClients.For(TestMessageWithoutResponse.T)
                            .WithPipeline(p => p.UseTest().UseTest())
                            .WithTransport(b => b.UseInProcess())
                            .AsIHandler()
                            .Handle(new());
    }

    public sealed partial record TestMessage(int Payload);

    public sealed record TestMessageResponse;

    // generated
    public sealed partial record TestMessage : IMessage<TestMessage, TestMessageResponse>
    {
        public static MessageTypes<TestMessage, TestMessageResponse> T => MessageTypes<TestMessage, TestMessageResponse>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static TestMessage? IMessage<TestMessage, TestMessageResponse>.EmptyInstance => null;

        public static IDefaultMessageTypesInjector DefaultTypeInjector
            => DefaultMessageTypesInjector<TestMessage, TestMessageResponse, IHandler, IHandler.Adapter, IPipeline, IPipeline.Adapter>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessage, TestMessageResponse>.TypeInjectors
            => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessage>();

        public interface IHandler : IGeneratedMessageHandler<TestMessage, TestMessageResponse, IPipeline>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter : GeneratedMessageHandlerAdapter<TestMessage, TestMessageResponse>, IHandler;
        }

        public interface IPipeline : IMessagePipeline<TestMessage, TestMessageResponse>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter : GeneratedMessagePipelineAdapter<TestMessage, TestMessageResponse>, IPipeline;
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

    public sealed partial record TestMessageWithoutResponse;

    // generated
    public sealed partial record TestMessageWithoutResponse : IMessage<TestMessageWithoutResponse, UnitMessageResponse>
    {
        public static MessageTypes<TestMessageWithoutResponse, UnitMessageResponse> T => MessageTypes<TestMessageWithoutResponse, UnitMessageResponse>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static TestMessageWithoutResponse IMessage<TestMessageWithoutResponse, UnitMessageResponse>.EmptyInstance => new();

        public static IDefaultMessageTypesInjector DefaultTypeInjector
            => DefaultMessageTypesInjector<TestMessageWithoutResponse, IHandler, IHandler.Adapter, IPipeline, IPipeline.Adapter>.Default;

        [EditorBrowsable(EditorBrowsableState.Never)]
        static IReadOnlyCollection<IMessageTypesInjector> IMessage<TestMessageWithoutResponse, UnitMessageResponse>.TypeInjectors
            => IMessageTypesInjector.GetTypeInjectorsForMessageType<TestMessageWithoutResponse>();

        public interface IHandler : IGeneratedMessageHandler<TestMessageWithoutResponse, IPipeline>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter : GeneratedMessageHandlerAdapter<TestMessageWithoutResponse>, IHandler;
        }

        public interface IPipeline : IMessagePipeline<TestMessageWithoutResponse, UnitMessageResponse>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Adapter : GeneratedMessagePipelineAdapter<TestMessageWithoutResponse, UnitMessageResponse>, IPipeline;
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

public static class TestMessageHandlerExtensions_2440209043122230735
{
    public static MessageTypeGenerationTests.TestMessage.IHandler AsIHandler(this IMessageHandler<MessageTypeGenerationTests.TestMessage, MessageTypeGenerationTests.TestMessageResponse> handler)
        => handler.AsIHandler<MessageTypeGenerationTests.TestMessage, MessageTypeGenerationTests.TestMessageResponse, MessageTypeGenerationTests.TestMessage.IHandler>();
}

public static class TestMessageWithoutResponseHandlerExtensions_2440209043122230735
{
    public static MessageTypeGenerationTests.TestMessageWithoutResponse.IHandler AsIHandler(this IMessageHandler<MessageTypeGenerationTests.TestMessageWithoutResponse> handler)
        => handler.AsIHandler<MessageTypeGenerationTests.TestMessageWithoutResponse, MessageTypeGenerationTests.TestMessageWithoutResponse.IHandler>();
}

public static class MessageTypeGenerationTestsPipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseTest<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Use(ctx => ctx.Next(ctx.Message, ctx.CancellationToken));
    }
}
