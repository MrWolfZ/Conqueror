// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

using System.ComponentModel;
using System.Reflection;

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

        var messageClients = provider.GetRequiredService<IMessageSenders>();

        var result = await messageClients.For(TestMessage.T)
                                         .WithPipeline(p => p.UseTest().UseTest())
                                         .WithTransport(b => b.UseInProcess())
                                         .Handle(new(10));

        Assert.That(result, Is.Not.Null);

        await messageClients.For(TestMessageWithoutResponse.T)
                            .WithPipeline(p => p.UseTest().UseTest())
                            .WithTransport(b => b.UseInProcess())
                            .Handle(new());
    }

    [Message<TestMessageResponse>]
    public sealed partial record TestMessage(int Payload);

    public sealed record TestMessageResponse;

    // generated
    public sealed partial record TestMessage : IMessage<TestMessage, TestMessageResponse>
    {
        public static MessageTypes<TestMessage, TestMessageResponse, IHandler> T => new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        static TestMessage? IMessage<TestMessage, TestMessageResponse>.EmptyInstance => null;

        static IEnumerable<ConstructorInfo> IMessage<TestMessage, TestMessageResponse>.PublicConstructors
            => typeof(TestMessage).GetConstructors(BindingFlags.Public);

        static IEnumerable<PropertyInfo> IMessage<TestMessage, TestMessageResponse>.PublicProperties
            => typeof(TestMessage).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        public interface IHandler : IMessageHandler<TestMessage, TestMessageResponse, IHandler, IHandler.Proxy, IPipeline, IPipeline.Proxy>
        {
            Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default);

            static Task<TestMessageResponse> IMessageHandler<TestMessage, TestMessageResponse, IHandler>.Invoke(IHandler handler, TestMessage message, CancellationToken cancellationToken)
                => handler.Handle(message, cancellationToken);

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Proxy : MessageHandlerProxy<TestMessage, TestMessageResponse, IHandler, Proxy>, IHandler;
        }

        public interface IPipeline : IMessagePipeline<TestMessage, TestMessageResponse>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Proxy : MessagePipelineProxy<TestMessage, TestMessageResponse>, IPipeline;
        }
    }

    private sealed partial class TestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return new();
        }

        public static void ConfigurePipeline(TestMessage.IPipeline pipeline) =>
            pipeline.UseTest().UseTest();

        static IEnumerable<IMessageHandlerTypesInjector> IMessageHandler.GetTypeInjectors()
        {
            yield return TestMessage.IHandler.CreateCoreTypesInjector<TestMessageHandler>();
        }
    }

    [Message]
    public sealed partial record TestMessageWithoutResponse;

    // generated
    public sealed partial record TestMessageWithoutResponse : IMessage<TestMessageWithoutResponse, UnitMessageResponse>
    {
        public static MessageTypes<TestMessageWithoutResponse, UnitMessageResponse, IHandler> T => new();

        [EditorBrowsable(EditorBrowsableState.Never)]
        static TestMessageWithoutResponse? IMessage<TestMessageWithoutResponse, UnitMessageResponse>.EmptyInstance => null;

        static IEnumerable<ConstructorInfo> IMessage<TestMessageWithoutResponse, UnitMessageResponse>.PublicConstructors
            => typeof(TestMessageWithoutResponse).GetConstructors(BindingFlags.Public);

        static IEnumerable<PropertyInfo> IMessage<TestMessageWithoutResponse, UnitMessageResponse>.PublicProperties
            => typeof(TestMessageWithoutResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        public interface IHandler : IMessageHandler<TestMessageWithoutResponse, UnitMessageResponse, IHandler, IHandler.Proxy, IPipeline, IPipeline.Proxy>
        {
            Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default);

            static async Task<UnitMessageResponse> IMessageHandler<TestMessageWithoutResponse, UnitMessageResponse, IHandler>.Invoke(IHandler handler, TestMessageWithoutResponse message, CancellationToken cancellationToken)
            {
                await handler.Handle(message, cancellationToken).ConfigureAwait(false);
                return UnitMessageResponse.Instance;
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Proxy : MessageHandlerProxy<TestMessageWithoutResponse, IHandler, Proxy>, IHandler;
        }

        public interface IPipeline : IMessagePipeline<TestMessageWithoutResponse, UnitMessageResponse>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Proxy : MessagePipelineProxy<TestMessageWithoutResponse, UnitMessageResponse>, IPipeline;
        }
    }

    private sealed partial class TestMessageWithoutResponseHandler : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        public static void ConfigurePipeline(TestMessageWithoutResponse.IPipeline pipeline) =>
            pipeline.UseTest().UseTest();

        static IEnumerable<IMessageHandlerTypesInjector> IMessageHandler.GetTypeInjectors()
        {
            yield return TestMessageWithoutResponse.IHandler.CreateCoreTypesInjector<TestMessageWithoutResponseHandler>();
        }
    }
}

public static class MessageTypeGenerationTestsPipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseTest<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Use(ctx => ctx.Next(ctx.Message, ctx.CancellationToken));
    }
}
