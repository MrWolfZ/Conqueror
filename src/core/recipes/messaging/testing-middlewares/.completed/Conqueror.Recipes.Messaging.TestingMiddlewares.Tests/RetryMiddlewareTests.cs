namespace Conqueror.Recipes.Messaging.TestingMiddlewares.Tests;

[TestFixture]
public partial class RetryMiddlewareTests
{
    [Test]
    public async Task GivenHandlerThatThrowsOnceWithDefaultConfiguration_WhenExecutingMessage_NoExceptionIsThrown()
    {
        var executionCount = 0;

        await using var serviceProvider = BuildServiceProvider(msg =>
        {
            executionCount += 1;

            if (executionCount > 1)
            {
                return new TestMessageResponse(msg.Parameter);
            }

            throw new InvalidOperationException("test exception");
        });

        var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(TestMessage.T);
        Assert.DoesNotThrowAsync(() => handler.Handle(new(1)));
    }

    [Test]
    public async Task GivenHandlerThatContinuouslyThrowsWithDefaultConfiguration_WhenExecutingMessage_ExceptionIsThrown()
    {
        var expectedException = new InvalidOperationException("test exception");
        await using var serviceProvider = BuildServiceProvider(_ => throw expectedException);

        var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(TestMessage.T);
        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new(1)));
        Assert.That(thrownException, Is.SameAs(expectedException));
    }

    [Test]
    public async Task GivenHandlerThatThrowsThreeTimesWithCustomRetryAttemptLimitOfThree_WhenExecutingMessage_NoExceptionIsThrown()
    {
        var executionCount = 0;

        await using var serviceProvider = BuildServiceProvider(msg =>
        {
            executionCount += 1;

            if (executionCount > 3)
            {
                return new TestMessageResponse(msg.Parameter);
            }

            throw new InvalidOperationException("test exception");
        }, pipeline => pipeline.UseRetry(retryAttemptLimit: 3));

        var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(TestMessage.T);
        Assert.DoesNotThrowAsync(() => handler.Handle(new(1)));
    }

    [Test]
    public async Task GivenHandlerThatContinuouslyThrowsWithCustomRetryAttemptLimitOfThree_WhenExecutingMessage_ExceptionIsThrown()
    {
        var expectedException = new InvalidOperationException("test exception");
        await using var serviceProvider = BuildServiceProvider(_ => throw expectedException, pipeline => pipeline.UseRetry(retryAttemptLimit: 3));

        var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(TestMessage.T);
        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new(1)));
        Assert.That(thrownException, Is.SameAs(expectedException));
    }

    private static ServiceProvider BuildServiceProvider(Func<TestMessage, TestMessageResponse> handleMessage,
                                                        Action<IMessagePipeline<TestMessage, TestMessageResponse>>? configurePipeline = null)
    {
        return new ServiceCollection()

               // create a handler from a delegate
               .AddMessageHandlerDelegate(TestMessage.T,
                                          (message, _) => handleMessage(message),
                                          configurePipeline ?? (pipeline => pipeline.UseRetry()))

               // add the retry middleware's default configuration
               .AddSingleton(new RetryMiddlewareConfiguration { RetryAttemptLimit = 1 })
               .BuildServiceProvider();
    }

    [Message<TestMessageResponse>]
    private partial record TestMessage(int Parameter);

    private record TestMessageResponse(int Value);
}
