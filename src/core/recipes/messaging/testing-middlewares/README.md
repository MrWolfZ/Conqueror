# Conqueror recipe (Messaging): testing middlewares and reusable pipelines

This recipe shows how simple it is to test your middlewares and reusable pipelines with **Conqueror**.

The middlewares we are going to test are similar to those we built in the recipe for [solving cross-cutting concerns](../solving-cross-cutting-concerns#readme). If you have not yet read that recipe, we recommend you take a look before you start with this one.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/core/recipes/messaging/testing-middlewares) and open the solution file in your IDE (note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/core/recipes/messaging/testing-middlewares).

In this recipe, we will look at three different approaches to testing middlewares: testing simple middlewares, testing configurable middlewares, and testing pipelines which consist of multiple middlewares. All of these approaches have one thing in common: they always test the middleware or pipeline as part of executing a handler. This is in line with our advice from the recipe for [testing message handlers](../testing-handlers#readme), in that you should always test your code through its public API and use it as closely as possible to how it is used in your production application code.

> It is of course possible to write unit tests which instantiate a middleware directly, but then you would have to provide certain details like the middleware context yourself. We hope that after reading this recipe you will agree that testing middlewares through handler executions provides a sufficient level of control to obviate the need for direct tests.

The application, to which we will be adding tests, has two message middlewares: one for [data annotation validation](Conqueror.Recipes.Messaging.TestingMiddlewares/DataAnnotationValidationMiddleware.cs) and one for [retrying failed executions](Conqueror.Recipes.Messaging.TestingMiddlewares/RetryMiddleware.cs).

Let's start by writing tests for the [DataAnnotationValidationMiddleware](Conqueror.Recipes.Messaging.TestingMiddlewares/DataAnnotationValidationMiddleware.cs). Create a new file `DataAnnotationValidationMiddlewareTests.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingMiddlewares.Tests/DataAnnotationValidationMiddlewareTests.cs)) in the test project:

> We're using the [NUnit](https://nunit.org) framework in this recipe, but any of the points discussed here apply to any other testing framework as well.

```cs
namespace Conqueror.Recipes.Messaging.TestingMiddlewares.Tests;

[TestFixture]
public partial class DataAnnotationValidationMiddlewareTests
{
}
```

> The test class is declared `partial` because we are going to add a nested message type to it, and the source generator needs the containing type to be `partial` in order to generate the handler and pipeline interfaces for that message.

Since the data annotation validation middleware is very simple, we can also keep the test code for it simple. To test the middleware as part of executing a handler, we need to create a message and its handler. To isolate tests from each other we recommend that you create them as `private` nested types in the test class:

```cs
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

    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default) =>
        // since we are only testing the input validation, the handler does not need to do anything
        Task.FromResult(new TestMessageResponse(message.Parameter));
}
```

Now we need to create a service provider that has both the handler and the middleware in its services. We can use a helper method for this:

```cs
private static ServiceProvider BuildServiceProvider()
{
    return new ServiceCollection().AddMessageHandler<TestMessageHandler>()
                                  .BuildServiceProvider();
}
```

Now we are ready to start writing tests. Just like when testing handlers, we resolve the handler through `IMessageSenders.For(TestMessage.T)` and execute it. The first test is going to assert that validation fails with an invalid message:

```cs
[Test]
public async Task GivenHandlerWithValidationAnnotations_WhenExecutingWithInvalidMessage_ValidationExceptionIsThrown()
{
    await using var serviceProvider = BuildServiceProvider();
    var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(TestMessage.T);
    Assert.ThrowsAsync<ValidationException>(() => handler.Handle(new(-1)));
}
```

And then we add one more to assert that executing a valid message works:

```cs
[Test]
public async Task GivenHandlerWithValidationAnnotations_WhenExecutingWithValidMessage_NoExceptionIsThrown()
{
    await using var serviceProvider = BuildServiceProvider();
    var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(TestMessage.T);
    Assert.DoesNotThrowAsync(() => handler.Handle(new(1)));
}
```

This concludes the tests for the data annotation validation middleware. As you can see, testing such simple middlewares is fairly straightforward. Next, we will write tests for the retry middleware, which is a bit more complex.

Create a new file `RetryMiddlewareTests.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingMiddlewares.Tests/RetryMiddlewareTests.cs)) in the test project:

```cs
namespace Conqueror.Recipes.Messaging.TestingMiddlewares.Tests;

[TestFixture]
public partial class RetryMiddlewareTests
{
}
```

The retry middleware's behavior depends on what happens when the handler is executed (i.e. based on whether it throws an exception or not). Therefore, we need to be able to control what the handler does in our tests. **Conqueror** provides a way to create handlers from a delegate, which makes it very simple to create a handler with dynamic behavior. Let's create a dedicated test message type and a helper that builds the service provider from a handler delegate:

```cs
[Message<TestMessageResponse>]
private partial record TestMessage(int Parameter);

private record TestMessageResponse(int Value);

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
```

> Creating handlers from delegates like this is only recommended for testing purposes. In the application code itself, handlers should always be proper classes.

Notice that the helper accepts an optional pipeline configuration function. When none is provided, the handler uses the retry middleware with its default configuration; when one is provided, the test can configure the pipeline however it likes. This lets us cover both the default configuration and custom configurations with the same helper.

With the supporting code above we can now start writing tests. The first test will assert that execution is successful when the handler throws an exception once with the default configuration:

```cs
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
```

Let's also add a test which asserts that a handler which throws continuously still causes the execution to fail:

```cs
[Test]
public async Task GivenHandlerThatContinuouslyThrowsWithDefaultConfiguration_WhenExecutingMessage_ExceptionIsThrown()
{
    var expectedException = new InvalidOperationException("test exception");
    await using var serviceProvider = BuildServiceProvider(_ => throw expectedException);

    var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(TestMessage.T);
    var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new(1)));
    Assert.That(thrownException, Is.SameAs(expectedException));
}
```

Next, let's use the custom pipeline configuration to verify that a custom retry attempt limit works as expected:

```cs
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
```

And one last test to verify that a continuously throwing handler still leads to a failure with a custom retry attempt limit:

```cs
[Test]
public async Task GivenHandlerThatContinuouslyThrowsWithCustomRetryAttemptLimitOfThree_WhenExecutingMessage_ExceptionIsThrown()
{
    var expectedException = new InvalidOperationException("test exception");
    await using var serviceProvider = BuildServiceProvider(_ => throw expectedException, pipeline => pipeline.UseRetry(retryAttemptLimit: 3));

    var handler = serviceProvider.GetRequiredService<IMessageSenders>().For(TestMessage.T);
    var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new(1)));
    Assert.That(thrownException, Is.SameAs(expectedException));
}
```

There are more tests which could be written for the retry middleware (for example, the edge cases of providing a zero or negative retry attempt limit), but the tests above should be sufficient to illustrate how tests for configurable middlewares can be written.

As the last step we are going to write tests for the default middleware pipeline defined in [MessagePipelineDefaultExtensions.cs](Conqueror.Recipes.Messaging.TestingMiddlewares/MessagePipelineDefaultExtensions.cs). This pipeline consists of two middlewares: the data annotation validation and retry middlewares we just wrote tests for. This particular pipeline is simple enough that we could test it exactly like we tested the individual middlewares, i.e. we would create a custom service provider inside a helper function in the test class and then resolve the handler from that provider to execute it. If your own reusable pipeline is as simple as this, then this approach is just fine to use. However, in a real application your pipelines are likely going to be significantly bigger and more complex, and it would require quite a lot of work to set up the service provider correctly. For these complex pipelines, a better approach is to consolidate the setup into a dedicated _test host_ class, which lets the actual test class focus on the tests themselves, just like we did in the recipe for [testing message handlers](../testing-handlers#readme).

Let's create such a test host in a new file `TestHost.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingMiddlewares.Tests/TestHost.cs)):

```cs
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
```

This test host takes care of configuring the service provider with the normal application services (through `AddApplicationServices`, which registers all services of the application project). It then registers our delegate handler, letting each test pass in both the pipeline configuration and the handler behavior. The constructor is private and instances are created through the static `Create` factory method. The class implements `IAsyncDisposable`, so each test can create its own host with `await using` and have the service provider disposed automatically at the end of the test. We prefer this composition-based approach over a shared base class, since it keeps each test explicit and self-contained.

> The `TestMessage` type is shared across the tests in this class, so we declare it at the namespace level next to the test host instead of nesting it in the test class.

Next, create a class `DefaultMessagePipelineTests.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.TestingMiddlewares.Tests/DefaultMessagePipelineTests.cs)):

```cs
namespace Conqueror.Recipes.Messaging.TestingMiddlewares.Tests;

[TestFixture]
public class DefaultMessagePipelineTests
{
}
```

When considering which tests to write for a reusable pipeline you have a few options. Depending on the complexity of the pipeline and whether your pipeline has its own parameters, you may want to write tests for various combinations of behaviors (for example, if you have a middleware for authentication and a middleware for authorization, those depend on each other and need to be tested together with different data and configuration combinations). For simple pipelines like our default pipeline, it may be sufficient to write a single test per middleware to assert its presence in the pipeline. Let's do that with the following tests:

```cs
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
```

Lastly, some middlewares may be present on your pipeline, but require the handler to provide a custom configuration (for example, an authorization middleware may be part of a default pipeline to determine its place in the middleware order, but it requires the handler to specify the required permission). This can be done by adjusting the pipeline configuration that the test passes into the host. For learning purposes, let's take a look at how such a test could be written for the retry middleware (even though the retry middleware doesn't require explicit configuration):

```cs
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
```

And that concludes this recipe for testing middlewares and reusable pipelines with **Conqueror**. In summary, we recommend the following:

- always test middlewares and pipelines as part of a handler execution with a handler resolved through `IMessageSenders`
- create handlers from delegates to allow dynamic handler execution behavior and middleware configuration
- for complex middlewares and pipelines, consolidate common setup logic into a test host class that each test creates and disposes via `await using`

As the next step you can explore how to [expose your messages via HTTP](../../../../transports/http/recipes/messaging/exposing-via-http#readme) and [how to test them](../../../../transports/http/recipes/messaging/testing-http#readme).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.messaging.testing-middlewares]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
