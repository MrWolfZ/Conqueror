namespace Conqueror.Tests;

public abstract class MessageClientsTests
{
    [Test]
    public void GivenMessageAndResponseType_WhenCreatingClient_ItWorks()
    {
        var provider = new ServiceCollection().AddConqueror().BuildServiceProvider();

        var messageClients = provider.GetRequiredService<IMessageClients>();

        Assert.DoesNotThrow(() => CreateClient(messageClients, TestMessage.T, b => b.UseInProcess()));
    }

    [Test]
    public void GivenMessageAndResponseType_WhenCreatingClientDirectly_ItWorks()
    {
        var provider = new ServiceCollection().AddConqueror().BuildServiceProvider();

        var messageClients = provider.GetRequiredService<IMessageClients>();

        Assert.DoesNotThrow(() => messageClients.For(TestMessage.T).Handle(new()));
        Assert.DoesNotThrow(() => messageClients.For(new TestMessage().M).Handle(new()));
    }

    [Test]
    public void GivenMessageTypeWithoutResponse_WhenCreatingClient_ItWorks()
    {
        var provider = new ServiceCollection().AddConqueror().BuildServiceProvider();

        var messageClients = provider.GetRequiredService<IMessageClients>();

        Assert.DoesNotThrow(() => CreateClient(messageClients, TestMessageWithoutResponse.T, b => b.UseInProcess()));
    }

    protected abstract IMessageHandler<TMessage, TResponse> CreateClient<TMessage, TResponse>(IMessageClients messageClients,
                                                                                              MessageTypes<TMessage, TResponse> messageTypes,
                                                                                              Func<IMessageTransportClientBuilder, IMessageTransportClient> transportClientFactory)
        where TMessage : class, IMessage<TResponse>;

    protected abstract IMessageHandler<TMessage> CreateClient<TMessage>(IMessageClients messageClients,
                                                                        MessageTypes<TMessage> messageTypes,
                                                                        Func<IMessageTransportClientBuilder, IMessageTransportClient> transportClientFactory)
        where TMessage : class, IMessage;

    public sealed record TestMessage : IMessage<TestMessageResponse>
    {
        public static MessageTypes<TestMessage, TestMessageResponse> T { get; } = new();

        public MessageTypes<TestMessage, TestMessageResponse> M => T;
    }

    public sealed record TestMessageResponse;

    public sealed record TestMessageWithoutResponse : IMessage
    {
        public static MessageTypes<TestMessageWithoutResponse> T { get; } = new();

        public MessageTypes<TestMessageWithoutResponse> M => T;
    }

    private sealed class TestMessageHandler : IMessageHandler<TestMessage, TestMessageResponse>
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestMessageWithoutResponseHandler : IMessageHandler<TestMessageWithoutResponse>
    {
        public Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

[TestFixture]
public sealed class MessageClientsWithoutFactoryTests : MessageClientsTests
{
    protected override IMessageHandler<TMessage, TResponse> CreateClient<TMessage, TResponse>(IMessageClients messageClients,
                                                                                              MessageTypes<TMessage, TResponse> messageTypes,
                                                                                              Func<IMessageTransportClientBuilder, IMessageTransportClient> transportClientFactory)
    {
        return messageClients.For(messageTypes);
    }

    protected override IMessageHandler<TMessage> CreateClient<TMessage>(IMessageClients messageClients,
                                                                        MessageTypes<TMessage> messageTypes,
                                                                        Func<IMessageTransportClientBuilder, IMessageTransportClient> transportClientFactory)
    {
        return messageClients.For(messageTypes);
    }
}

[TestFixture]
public sealed class MessageClientsWithSyncFactoryTests : MessageClientsTests
{
    protected override IMessageHandler<TMessage, TResponse> CreateClient<TMessage, TResponse>(IMessageClients messageClients,
                                                                                              MessageTypes<TMessage, TResponse> messageTypes,
                                                                                              Func<IMessageTransportClientBuilder, IMessageTransportClient> transportClientFactory)
    {
        return messageClients.For(messageTypes, transportClientFactory);
    }

    protected override IMessageHandler<TMessage> CreateClient<TMessage>(IMessageClients messageClients,
                                                                        MessageTypes<TMessage> messageTypes,
                                                                        Func<IMessageTransportClientBuilder, IMessageTransportClient> transportClientFactory)
    {
        return messageClients.For(messageTypes, transportClientFactory);
    }
}

[TestFixture]
public sealed class MessageClientsWithAsyncFactoryTests : MessageClientsTests
{
    protected override IMessageHandler<TMessage, TResponse> CreateClient<TMessage, TResponse>(IMessageClients messageClients,
                                                                                              MessageTypes<TMessage, TResponse> messageTypes,
                                                                                              Func<IMessageTransportClientBuilder, IMessageTransportClient> transportClientFactory)
    {
        return messageClients.For(messageTypes, async b =>
        {
            await Task.Delay(1);
            return transportClientFactory(b);
        });
    }

    protected override IMessageHandler<TMessage> CreateClient<TMessage>(IMessageClients messageClients,
                                                                        MessageTypes<TMessage> messageTypes,
                                                                        Func<IMessageTransportClientBuilder, IMessageTransportClient> transportClientFactory)
    {
        return messageClients.For(messageTypes, async b =>
        {
            await Task.Delay(1);
            return transportClientFactory(b);
        });
    }
}
