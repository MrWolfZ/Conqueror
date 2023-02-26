# Conqueror recipe (CQS Basics): testing middlewares and reusable pipelines

This recipe shows how simple it is to test your middlewares and reusable pipelines with **Conqueror.CQS**.

The middlewares we are going to test are similar to those we built in the recipe for [solving cross-cutting concerns](../solving-cross-cutting-concerns#readme). If you have not yet read that recipe, we recommend you take a look before you start with this one.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/basics/testing-middlewares) and open the solution file in your IDE (note that you need to have [.NET 6 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/basics/testing-middlewares).

In this recipe, we will look at three different approaches to testing middlewares: testing simple middlewares, testing configurable middlewares, and testing pipelines which consist of muliple middlewares. All of these approaches have one thing in common: they always test the middleware or pipeline as part of executing a handler. This is in line with our advice from the recipe for [testing command and query handlers](../testing-handlers#readme), in that you should always your code through its public API and use it as closely as possible to how it is used in your production application code.

> It is of course possible to write unit tests which instantiate a middleware directly, but then you would have provide certain details like the middleware context yourself. We hope that after reading this recipe you will agree that testing middlewares through handler executions provides a sufficient level of control to obliviate the need for direct tests.

The application, to which we will be adding tests, has two command middlewares: one for [data annotation validation](Conqueror.Recipes.CQS.Basics.TestingMiddlewares/DataAnnotationValidationCommandMiddleware.cs) and one for [retrying failed executions](Conqueror.Recipes.CQS.Basics.TestingMiddlewares/RetryCommandMiddleware.cs).

> Testing query middlewares and pipelines works exactly like it does for commands. Therefore we'll only look at comand middlewares and pipelines in this recipe to keep it simple.

Let's start by writing tests for the [DataAnnotationValidationCommandMiddleware](Conqueror.Recipes.CQS.Basics.TestingMiddlewares/DataAnnotationValidationCommandMiddleware.cs). Create a new file `DataAnnotationValidationCommandMiddlewareTests.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.TestingMiddlewares.Tests/DataAnnotationValidationCommandMiddlewareTests.cs)) in the test project:

> We're using the [NUnit](https://nunit.org) framework in this recipe, but any of the points discussed here apply to any other testing framework as well.

```cs
namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares.Tests;

[TestFixture]
public sealed class DataAnnotationValidationCommandMiddlewareTests
{
}
```

Since the data annotation validation middleware is very simple, we can also keep the test code for it simple. To test the middleware as part of executing a handler, we need to create a command and its handler. To isolate tests from each other we recommend that you create them as `private` nested classes in the test class:

```cs
private sealed record TestCommand(int Parameter)
{
    [Range(1, int.MaxValue)]
    public int Parameter { get; } = Parameter;
}

private sealed record TestCommandResponse(int Value);

private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
{
    public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
        pipeline.UseDataAnnotationValidation();

    public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
    {
        // since we are only testing the input validation, the handler does not need to do anything
        return Task.FromResult<TestCommandResponse>(new(command.Parameter));
    }
}
```

> Note that we are intentionally not creating a custom `ITestCommandHandler` interface here since that interface would need to be public in order to allow **Conqueror** to dynamically implement the interface.

Now we need to create a service provider that has both the handler and the middleware in its services. We can use a helper method for this:

```cs
private static ServiceProvider BuildServiceProvider()
{
    return new ServiceCollection().AddConquerorCommandMiddleware<DataAnnotationValidationCommandMiddleware>()
                                  .AddConquerorCommandHandler<TestCommandHandler>()
                                  .BuildServiceProvider();
}
```

Now we are ready to start writing tests. The first one is going to assert that validation fails with an invalid command:

```cs
[Test]
public async Task GivenHandlerWithValidationAnnotations_WhenExecutingWithInvalidCommand_ValidationExceptionIsThrown()
{
    await using var serviceProvider = BuildServiceProvider();
    var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
    Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(new(-1)));
}
```

And then we add one more to assert that executing a valid command works:

```cs
[Test]
public async Task GivenHandlerWithValidationAnnotations_WhenExecutingWithValidCommand_NoExceptionIsThrown()
{
    await using var serviceProvider = BuildServiceProvider();
    var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
    Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(new(1)));
}
```

This concludes the tests for the data annotation validation middleware. As you can see, testing such simple middlewares is fairly straightforward. Next, we will write tests for the retry middleware, which is a bit more complex.

Create a new file `RetryCommandMiddlewareTests.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.TestingMiddlewares.Tests/RetryCommandMiddlewareTests.cs)) in the test project:

```cs
namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares.Tests;

[TestFixture]
public sealed class RetryCommandMiddlewareTests
{
}
```

The retry middleware's behavior depends on what happens when the handler is executed (i.e. based on whether it throws an exception or not). Therefore, we need to be able to control what the handler does in our tests. **Conqueror.CQS** provides a way to create handlers from a delegate, which makes it very simple to create a handler with dynamic behavior. Let's create a dedicated test comand type and create the handler delegate:

```cs
private sealed record TestCommand(int Parameter);

private sealed record TestCommandResponse(int Value);

private static ServiceProvider BuildServiceProvider(Func<TestCommand, Task<TestCommandResponse>> handlerExecuteFn)
{
    return new ServiceCollection().AddConquerorCommandMiddleware<RetryCommandMiddleware>()

                                  // create a handler from a delegate
                                  .AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((command, _, _) => handlerExecuteFn(command),
                                                                                                        pipeline => pipeline.UseRetry())

                                  // add the retry middleware's default configuration
                                  .AddSingleton(new RetryMiddlewareConfiguration { RetryAttemptLimit = 1 })
                                  .BuildServiceProvider();
}
```

> Creating handlers from delegates like this is only recommended for testing purposes. In the application code itself, handlers shoud always be proper classes.

With the supporting code above we can now start writing tests. The first test will assert that execution is successful when the handler throws an exception once with the default configuration:

```cs
[Test]
public async Task GivenHandlerThatThrowsOnceWithDefaultConfiguration_WhenExecutingCommand_NoExceptionIsThrown()
{
    var executionCount = 0;

    await using var serviceProvider = BuildServiceProvider(cmd =>
    {
        executionCount += 1;

        if (executionCount > 1)
        {
            return Task.FromResult(new TestCommandResponse(cmd.Parameter));
        }

        throw new InvalidOperationException("test exception");
    });

    var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
    Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(new(1)));
}
```

Let's also add a test which asserts that a handler which throws continuously still causes the execution to fail:

```cs
[Test]
public async Task GivenHandlerThatContinuouslyThrowsWithDefaultConfiguration_WhenExecutingCommand_ExceptionIsThrown()
{
    var expectedException = new InvalidOperationException("test exception");
    await using var serviceProvider = BuildServiceProvider(_ => throw expectedException);

    var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
    var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new(1)));
    Assert.That(thrownException, Is.SameAs(expectedException));
}
```

You may have noticed that we referred to the default configuration, but currently we cannot provide a custom middleware configuration for the handler. However, we can use the same approach as for the dynamic handler behavior by passing a custom middleware configuration function into the service provider build method:

```diff
- private static ServiceProvider BuildServiceProvider(Func<TestCommand, Task<TestCommandResponse>> handlerExecuteFn)
+ private static ServiceProvider BuildServiceProvider(Func<TestCommand, Task<TestCommandResponse>> handlerExecuteFn,
+                                                     Action<ICommandPipelineBuilder>? configurePipeline = null)
  {
      return new ServiceCollection().AddConquerorCommandMiddleware<RetryCommandMiddleware>()

                                   // create a handler from a delegate
                                   .AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((command, _, _) => handlerExecuteFn(command),
-                                                                                                        pipeline => pipeline.UseRetry())
+                                                                                                        configurePipeline ?? (pipeline => pipeline.UseRetry()))

                                    // add the retry middleware's default configuration
                                    .AddSingleton(new RetryMiddlewareConfiguration { RetryAttemptLimit = 1 })
```

With this change we can dynamically configure the pipeline in our tests. Let's add a test which verifies that a custom retry attempt limit works as expected:

```cs
[Test]
public async Task GivenHandlerThatThrowsThreeTimesWithCustomRetryAttemptLimitOfThree_WhenExecutingCommand_NoExceptionIsThrown()
{
    var executionCount = 0;

    await using var serviceProvider = BuildServiceProvider(cmd =>
    {
        executionCount += 1;

        if (executionCount > 3)
        {
            return Task.FromResult(new TestCommandResponse(cmd.Parameter));
        }

        throw new InvalidOperationException("test exception");
    }, pipeline => pipeline.UseRetry(o => o.RetryAttemptLimit = 3));

    var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
    Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(new(1)));
}
```

And one last test to verify that a continuously throwing handler still leads to a failure with a custom retry attempt limit:

```cs
[Test]
public async Task GivenHandlerThatContinuouslyThrowsWithCustomRetryAttemptLimitOfThree_WhenExecutingCommand_ExceptionIsThrown()
{
    var expectedException = new InvalidOperationException("test exception");
    await using var serviceProvider = BuildServiceProvider(_ => throw expectedException, 
                                                           pipeline => pipeline.UseRetry(o => o.RetryAttemptLimit = 3));

    var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
    var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new(1)));
    Assert.That(thrownException, Is.SameAs(expectedException));
}
```

There are more tests which could be written for the retry middleware (for example, the edge cases of providing a zero or negative retry attempt limit), but the tests above should be sufficient to illustrate how tests for configurable middlewares can be written.

As the last step we are going to write tests for the default middleware pipeline defined in [CommandPipelineDefaultBuilderExtensions.cs](Conqueror.Recipes.CQS.Basics.TestingMiddlewares/CommandPipelineDefaultBuilderExtensions.cs). This pipeline consists of two middlewares: the data annotation validation and retry middlewares we just wrote tests for. This particular pipeline is simple enough that we could test it exactly like we tested the individual middlewares, i.e. we would create a custom service provider inside a helper function in the test class and then resolve the handler from that provider to execute it. If your own reusable pipeline is as simple as this, then this approach is just fine to use. However, in a real application your pipelines are likely going to be significantly bigger and more complex, and it would require quite a lot of work to set up the service provider correctly. For these complex pipelines, a better approach is to create (or reuse) a custom `TestBase` class, which takes care of the setup and lets the actual test class focus on the tests themselves, just like we did in the recipe for [testing command and query handlers](../testing-handlers#readme).

Let's create such a test base in a new file `TestBase.cs`:

```cs
namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares.Tests;

public abstract class TestBase : IDisposable
{
    private readonly Lazy<ServiceProvider> serviceProviderLazy;

    protected TestBase()
    {
        serviceProviderLazy = new(BuildServiceProvider);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public void Dispose()
    {
        if (serviceProviderLazy.IsValueCreated)
        {
            serviceProviderLazy.Value.Dispose();
        }
    }

    protected T Resolve<T>()
        where T : notnull => serviceProviderLazy.Value.GetRequiredService<T>();

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddApplicationServices();

        ConfigureServices(services);

        return services.BuildServiceProvider();
    }
}
```

This test base takes care of configuring the service provider with the normal application services, and also provides a virtual method to allow any subclasses to add extra services to the provider.

> If you are looking for a test base for an ASP.NET Core web application, you can find one [here](../../advanced/testing-http/.completed/Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests/TestBase.cs) in the completed code of the recipe for [testing HTTP command and query handlers](../../advanced/testing-http#readme).

Next, create a class `DefaultCommandPipelineTests.cs`:

```cs
namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares.Tests;

[TestFixture]
public sealed class DefaultCommandPipelineTests : TestBase
{
}
```

We are still going to use a custom test command and a delegate handler, but the setup looks slightly different:

```cs
private Action<ICommandPipelineBuilder> configurePipeline = pipeline => pipeline.UseDefault();

private Func<TestCommand, Task<TestCommandResponse>> handlerExecutionFn = cmd => Task.FromResult(new TestCommandResponse(cmd.Parameter));

private ICommandHandler<TestCommand, TestCommandResponse> Handler => Resolve<ICommandHandler<TestCommand, TestCommandResponse>>();

protected override void ConfigureServices(IServiceCollection services)
{
    base.ConfigureServices(services);

    // create handler from delegate; note that we intentionally wrap the pipeline configuration function
    // in another arrow function to ensure it can the changed inside tests
    services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((command, _, _) => handlerExecutionFn(command),
                                                                                  pipeline => configurePipeline(pipeline));
}

private sealed record TestCommand(int Parameter)
{
    [Range(1, int.MaxValue)]
    public int Parameter { get; } = Parameter;
}

private sealed record TestCommandResponse(int Value);
```

When considering which tests to write for a reusable pipeline you have a few options. Depending on the complexity of the pipeline and whether your pipeline has its own parameters, you may want to write tests for various combinations of behaviors (for example, if you have a middleware for authentication and a middleware for authorization, those depend on each other and need to be tested together with different data and configuration combinations). For simple pipelines like our default pipeline, it may be sufficient to write a single test per middleware to assert its presence in the pipeline. Let's do that with the following tests:

```cs
[Test]
public void GivenHandlerWithDefaultPipeline_WhenExecutingWithInvalidCommand_ValidationExceptionIsThrown()
{
    Assert.ThrowsAsync<ValidationException>(() => Handler.ExecuteCommand(new(-1)));
}

[Test]
public void GivenHandlerThatThrowsOnceWithDefaultPipeline_WhenExecutingCommand_NoExceptionIsThrown()
{
    var executionCount = 0;

    handlerExecutionFn = cmd =>
    {
        executionCount += 1;

        if (executionCount > 1)
        {
            return Task.FromResult(new TestCommandResponse(cmd.Parameter));
        }

        throw new InvalidOperationException("test exception");
    };

    Assert.DoesNotThrowAsync(() => Handler.ExecuteCommand(new(1)));
}
```

Lastly, some middlewares may be present on your pipeline, but require the handler to provide a custom configuration (for example, an authorization middleware may be part of a default middleware to determine its place in the middleware order, but it requires the handler to specify the required permission). This can be done by setting the `configurePipeline` field of our test class. For learning purposes, let's take a look at how such a test could be written for the retry middleware (even though the retry middleware doesn't require explicit configuration):

```cs
[Test]
public void GivenHandlerThatThrowsTwiceWithDefaultPipelineAndCustomRetryConfiguration_WhenExecutingCommand_NoExceptionIsThrown()
{
    var executionCount = 0;

    configurePipeline = pipeline => pipeline.UseDefault()
                                            .ConfigureRetry(o => o.RetryAttemptLimit = 2);

    handlerExecutionFn = cmd =>
    {
        executionCount += 1;

        if (executionCount > 2)
        {
            return Task.FromResult(new TestCommandResponse(cmd.Parameter));
        }

        throw new InvalidOperationException("test exception");
    };

    Assert.DoesNotThrowAsync(() => Handler.ExecuteCommand(new(1)));
}
```

And that concludes this recipe for testing middlewares and reusable pipelines with **Conqueror.CQS**. In summary, we recommend the following:

- always test middlewares and pipelines as part of a handler execution with a handler resolved from a service provider
- create handlers from delegates to allow dynamic handler execution behavior and middleware configuration
- for complex middlewares and pipelines, consolidate common setup logic into a base class

As the next step you can explore how to [expose your commands and queries via HTTP](../../advanced/exposing-via-http#readme) and [how to test them](../../advanced/testing-http#readme) (the completed code for the linked recipe contains an implementation of [TestBase](../../advanced/testing-http/.completed/Conqueror.Recipes.CQS.Advanced.TestingHttp.Tests/TestBase.cs) which creates a fully configured HTTP test server).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.cqs.basics.testing-middlewares]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
