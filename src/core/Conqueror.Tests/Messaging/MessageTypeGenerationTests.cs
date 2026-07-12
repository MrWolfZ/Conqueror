// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

// we simulate the generator output here

#pragma warning disable SA1302, CA1715

namespace Conqueror.Tests.Messaging;

[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1201:Elements should appear in the correct order",
    Justification = "we are emulating the output of the source generator"
)]
[SuppressMessage(
    "Roslynator",
    "RCS1018:Add/remove accessibility modifiers",
    Justification = "we are emulating the output of the source generator"
)]
public sealed partial class MessageTypeGenerationTests
{
    [Test]
    public async Task GivenMessageTypeWithExplicitImplementations_WhenUsingHandler_ItWorks()
    {
        var services = new ServiceCollection();
        var provider = services
            .AddMessageHandler<TestMessageHandler>()
            .AddMessageHandler<TestMessageWithoutResponseHandler>()
            .BuildServiceProvider();

        var messageClients = provider.GetRequiredService<IMessageSenders>();

        var result = await messageClients
            .For(TestMessage.T)
            .WithPipeline(p => p.UseTest().UseTest())
            .WithTransport(b => b.UseInProcess())
            .Handle(new(Payload: 10), CancellationToken.None);

        Assert.That(result, Is.Not.Null);

        await messageClients
            .For(TestMessageWithoutResponse.T)
            .WithPipeline(p => p.UseTest().UseTest())
            .WithTransport(b => b.UseInProcess())
            .Handle(new(), CancellationToken.None);
    }

    [Message<TestMessageResponse>]
    public sealed partial record TestMessage(int Payload);

    public sealed record TestMessageResponse;

    // generated
    public sealed partial record TestMessage : IMessage<TestMessage, TestMessageResponse>
    {
        public static MessageTypes<TestMessage, TestMessageResponse, IHandler> T => new();

        static IMessageHandlerTypesInjector IMessage<TestMessage, TestMessageResponse>.CoreTypesInjector { get; } =
            IHandler.CreateCoreTypesInjector();

        [EditorBrowsable(EditorBrowsableState.Never)]
        static TestMessage? IMessage<TestMessage, TestMessageResponse>.EmptyInstance => null;

        static IEnumerable<ConstructorInfo> IMessage<TestMessage, TestMessageResponse>.PublicConstructors =>
            typeof(TestMessage).GetConstructors(BindingFlags.Public);

        static IEnumerable<PropertyInfo> IMessage<TestMessage, TestMessageResponse>.PublicProperties =>
            typeof(TestMessage).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        static Task<TestMessageResponse> IMessage<TestMessage, TestMessageResponse>.InvokeHandler<TIHandler>(
            TIHandler handler,
            TestMessage message,
            CancellationToken cancellationToken
        ) => ((IHandler)handler).Handle(message, cancellationToken);

        public interface IHandler
            : IMessageHandler<TestMessage, TestMessageResponse, IHandler, IHandler.Proxy, IPipeline, IPipeline.Proxy>
        {
            Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default);

            [EditorBrowsable(EditorBrowsableState.Never)]
            sealed class Proxy : MessageHandlerProxy<TestMessage, TestMessageResponse, IHandler>, IHandler;
        }

        public interface IPipeline : IMessagePipeline<TestMessage, TestMessageResponse>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            sealed class Proxy : MessagePipelineProxy<TestMessage, TestMessageResponse>, IPipeline;
        }
    }

    private sealed partial class TestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(
            TestMessage message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.CompletedTask;

            return new TestMessageResponse();
        }

        public static void ConfigurePipeline(TestMessage.IPipeline pipeline) => pipeline.UseTest().UseTest();

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

        static IMessageHandlerTypesInjector IMessage<
            TestMessageWithoutResponse,
            UnitMessageResponse
        >.CoreTypesInjector { get; } = IHandler.CreateCoreTypesInjector();

        [EditorBrowsable(EditorBrowsableState.Never)]
        static TestMessageWithoutResponse? IMessage<TestMessageWithoutResponse, UnitMessageResponse>.EmptyInstance =>
            null;

        static IEnumerable<ConstructorInfo> IMessage<
            TestMessageWithoutResponse,
            UnitMessageResponse
        >.PublicConstructors => typeof(TestMessageWithoutResponse).GetConstructors(BindingFlags.Public);

        static IEnumerable<PropertyInfo> IMessage<TestMessageWithoutResponse, UnitMessageResponse>.PublicProperties =>
            typeof(TestMessageWithoutResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        static async Task<UnitMessageResponse> IMessage<
            TestMessageWithoutResponse,
            UnitMessageResponse
        >.InvokeHandler<TIHandler>(
            TIHandler handler,
            TestMessageWithoutResponse message,
            CancellationToken cancellationToken
        )
        {
            await ((IHandler)handler).Handle(message, cancellationToken).ConfigureAwait(false);

            return UnitMessageResponse.Instance;
        }

        public interface IHandler
            : IMessageHandler<
                TestMessageWithoutResponse,
                UnitMessageResponse,
                IHandler,
                IHandler.Proxy,
                IPipeline,
                IPipeline.Proxy
            >
        {
            Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default);

            [EditorBrowsable(EditorBrowsableState.Never)]
            sealed class Proxy : MessageHandlerProxy<TestMessageWithoutResponse, IHandler>, IHandler;
        }

        public interface IPipeline : IMessagePipeline<TestMessageWithoutResponse, UnitMessageResponse>
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            sealed class Proxy : MessagePipelineProxy<TestMessageWithoutResponse, UnitMessageResponse>, IPipeline;
        }
    }

    private sealed partial class TestMessageWithoutResponseHandler : TestMessageWithoutResponse.IHandler
    {
        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default) =>
            await Task.CompletedTask;

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
    public static IMessagePipeline<TMessage, TResponse> UseTest<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline
    )
        where TMessage : class, IMessage<TMessage, TResponse> =>
        pipeline.Use(ctx => ctx.Next(ctx.Message, ctx.CancellationToken));
}
