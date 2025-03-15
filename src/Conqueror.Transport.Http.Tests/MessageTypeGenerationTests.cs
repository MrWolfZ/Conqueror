// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming

using System.ComponentModel;

#pragma warning disable SA1302
#pragma warning disable CA1715

namespace Conqueror.Transport.Http.Tests;

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

    [HttpMessage]
    public sealed partial record TestMessage : IMessage<TestMessageResponse>;

    public sealed record TestMessageResponse;

    // generated http
    public sealed partial record TestMessage : IHttpMessage<TestMessage>
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TResult CreateWithMessageTypes<TResult>(IHttpMessageTypesInjectionFactory<TResult> factory)
            => factory.Create<TestMessage, TestMessageResponse>();
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

    [HttpMessage]
    public sealed partial record TestMessageWithoutResponse : IMessage;

    // generated http
    public sealed partial record TestMessageWithoutResponse : IHttpMessage<TestMessageWithoutResponse>
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TResult CreateWithMessageTypes<TResult>(IHttpMessageTypesInjectionFactory<TResult> factory)
            => factory.Create<TestMessageWithoutResponse, UnitMessageResponse>();
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

    public interface ICustomHttpMessage<TMessage> : IHttpMessage<TMessage>
        where TMessage : IHttpMessage<TMessage>
    {
        static string IHttpMessage<TMessage>.Name => "test";
    }

    public sealed partial record TestMessageWithCustomInterface : IMessage<TestMessageResponse>, ICustomHttpMessage<TestMessageWithCustomInterface>;

    // generated http
    public sealed partial record TestMessageWithCustomInterface : IHttpMessage<TestMessage>
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TResult CreateWithMessageTypes<TResult>(IHttpMessageTypesInjectionFactory<TResult> factory)
            => factory.Create<TestMessageWithCustomInterface, TestMessageResponse>();
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
