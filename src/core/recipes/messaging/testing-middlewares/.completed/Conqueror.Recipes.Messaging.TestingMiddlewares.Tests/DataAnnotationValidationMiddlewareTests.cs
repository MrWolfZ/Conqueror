namespace Conqueror.Recipes.Messaging.TestingMiddlewares.Tests;

[TestFixture]
public partial class DataAnnotationValidationMiddlewareTests
{
    [Test]
    public async Task GivenHandlerWithValidationAnnotations_WhenExecutingWithInvalidMessage_ValidationExceptionIsThrown()
    {
        await using var serviceProvider = BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(TestMessage.T);
        Assert.ThrowsAsync<ValidationException>(() => handler.Handle(new(-1)));
    }

    [Test]
    public async Task GivenHandlerWithValidationAnnotations_WhenExecutingWithValidMessage_NoExceptionIsThrown()
    {
        await using var serviceProvider = BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(TestMessage.T);
        Assert.DoesNotThrowAsync(() => handler.Handle(new(1)));
    }

    private static ServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection().AddMessageHandler<TestMessageHandler>()
                                      .BuildServiceProvider();
    }

    [Message<TestMessageResponse>]
    private partial record TestMessage(int Parameter)
    {
        [Range(1, int.MaxValue)]
        public int Parameter { get; } = Parameter;
    }

    private record TestMessageResponse(int Value);

    private partial class TestMessageHandler : TestMessage.IHandler
    {
        public static void ConfigurePipeline(TestMessage.IPipeline pipeline) =>
            pipeline.UseDataAnnotationValidation();

        // since we are only testing the input validation, the handler does not need to do anything
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TestMessageResponse(message.Parameter));
    }
}
