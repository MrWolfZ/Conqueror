namespace Conqueror.Recipes.Messaging.TestingMiddlewares.Tests;

[TestFixture]
public class DefaultMessagePipelineTests
{
    [Test]
    public async Task GivenHandlerWithDefaultPipeline_WhenExecutingWithInvalidMessage_ValidationExceptionIsThrown()
    {
        await using var host = TestHost.Create(pipeline => pipeline.UseDefault(),
            message => new TestMessageResponse(message.Parameter));

        var handler = host.MessageSenders.For(TestMessage.T);
        Assert.ThrowsAsync<ValidationException>(() => handler.Handle(new(-1)));
    }

    [Test]
    public async Task GivenHandlerThatThrowsOnceWithDefaultPipeline_WhenExecutingMessage_NoExceptionIsThrown()
    {
        var executionCount = 0;

        await using var host = TestHost.Create(pipeline => pipeline.UseDefault(), message =>
        {
            executionCount += 1;

            if (executionCount > 1)
            {
                return new TestMessageResponse(message.Parameter);
            }

            throw new InvalidOperationException("test exception");
        });

        var handler = host.MessageSenders.For(TestMessage.T);
        Assert.DoesNotThrowAsync(() => handler.Handle(new(1)));
    }

    [Test]
    public async Task GivenHandlerThatThrowsTwiceWithDefaultPipelineAndCustomRetryConfiguration_WhenExecutingMessage_NoExceptionIsThrown()
    {
        var executionCount = 0;

        await using var host = TestHost.Create(
            pipeline => pipeline.UseDefault().ConfigureRetry(o => o.RetryAttemptLimit = 2),
            message =>
            {
                executionCount += 1;

                if (executionCount > 2)
                {
                    return new TestMessageResponse(message.Parameter);
                }

                throw new InvalidOperationException("test exception");
            });

        var handler = host.MessageSenders.For(TestMessage.T);
        Assert.DoesNotThrowAsync(() => handler.Handle(new(1)));
    }
}
