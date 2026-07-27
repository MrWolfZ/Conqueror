namespace Conqueror.Recipes.Messaging.TestingMiddlewares.Tests;

internal sealed class TestHost : IAsyncDisposable
{
    private readonly ServiceProvider serviceProvider;

    private TestHost(Action<IMessagePipeline<TestMessage, TestMessageResponse>> configurePipeline,
                     Func<TestMessage, TestMessageResponse> handleMessage)
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        // create the handler from a delegate so each test can control both the pipeline
        // configuration and what the handler does when it is executed
        services.AddMessageHandlerDelegate(TestMessage.T, (message, _) => handleMessage(message), configurePipeline);

        serviceProvider = services.BuildServiceProvider();
    }

    public IMessageSenders MessageSenders => serviceProvider.GetRequiredService<IMessageSenders>();

    public static TestHost Create(Action<IMessagePipeline<TestMessage, TestMessageResponse>> configurePipeline,
                                  Func<TestMessage, TestMessageResponse> handleMessage) =>
        new(configurePipeline, handleMessage);

    public ValueTask DisposeAsync() => serviceProvider.DisposeAsync();
}

[Message<TestMessageResponse>]
internal partial record TestMessage(int Parameter)
{
    [Range(1, int.MaxValue)]
    public int Parameter { get; } = Parameter;
}

internal record TestMessageResponse(int Value);
