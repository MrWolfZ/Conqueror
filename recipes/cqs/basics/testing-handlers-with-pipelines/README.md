# Conqueror recipe (CQS Basics): testing command and query handlers that have middleware pipelines

This recipe guides you in how to test command and query handlers which have middleware pipelines **Conqueror.CQS**. If you are looking for guidance on how to test your own custom middlewares, there's [a dedicated recipe](../testing-middlewares#readme) for that.

If you have not yet read the recipes for [solving cross-cutting concerns with middlewares](../solving-cross-cutting-concerns#readme) or [testing command and query handlers](../testing-handlers#readme), we recommend you take a look at those before you start with this recipe.

> This recipe is a bit different than others because we are not going to write any code (but we'll look at some code examples).

From the recipe about [testing command and query handlers](../testing-handlers#readme) you may recall our advice that handlers should always be tested through their public API, called via either the `ICommandHandler` and `IQueryHandler` interfaces or your own custom handler interfaces. **The same advice is still true for handlers which have middleware pipelines**.

Let's take a look at the `IncrementCounterByCommandHandler` from the [recipe about solving cross-cutting concerns](../solving-cross-cutting-concerns#readme). Here we are going to configure the handler's pipeline to perform data annotation validation.

> Everything discussed here is the same for query handlers, so we'll only look at a command handler in this recipe for simplicity.

```cs
internal sealed class IncrementCounterByCommandHandler : IIncrementCounterByCommandHandler, IConfigureCommandPipeline
{
    private readonly CountersRepository repository;

    public IncrementCounterByCommandHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => 
        pipeline.UseDataAnnotationValidation();

    public async Task<IncrementCounterByCommandResponse> ExecuteCommand(IncrementCounterByCommand command, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(command.CounterName);
        await repository.SetCounterValue(command.CounterName, counterValue + command.IncrementBy);
        return new(counterValue + command.IncrementBy);
    }
}
```

The data annotation validation middleware is fairly trivial, since it simply invokes the validation of the command in a single line of code.

```cs
internal sealed class DataAnnotationValidationCommandMiddleware : ICommandMiddleware
{
    public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        Validator.ValidateObject(ctx.Command, new(ctx.Command), true);
        return ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}
```

Now let's think about how we would test the handler. We would certainly want to write functional tests which assert that counters can correctly be incremented with this command (which happens in the body of the `ExecuteCommand` method). However, we also would want to assert that invalid commands lead to a validation error. How can we do this? By simply executing the command handler like we would normally do, but with an invalid command, and then asserting that the execution throws an exception.

```cs
[Test]
public void GivenNonExistingCounter_WhenExecutingCommandWithNegativeIncrementBy_ValidationExceptionIsThrown()
{
    var handler = Resolve<IIncrementCounterCommandHandler>();
    Assert.ThrowsAsync<ValidationException>(() => handler.ExecuteCommand(new(TestCounterName, -1)));
}
```

The critical insight here is that when looking at a handler from the outside, it does not matter whether a behavior is coded directly into its `ExecuteCommand` method or whether it is added with a middleware in the pipeline. In essence, this means that **the middleware pipeline of a handler becomes an inseparable part of its public API**. In our case this means we treat the handler as if it was written without a pipeline like this:

```cs
internal sealed class IncrementCounterByCommandHandler : IIncrementCounterByCommandHandler
{
    private readonly CountersRepository repository;

    public IncrementCounterByCommandHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public async Task<IncrementCounterByCommandResponse> ExecuteCommand(IncrementCounterByCommand command, CancellationToken cancellationToken = default)
    {
        Validator.ValidateObject(command, new(command), true);

        var counterValue = await repository.GetCounterValue(command.CounterName);
        await repository.SetCounterValue(command.CounterName, counterValue + command.IncrementBy);
        return new(counterValue + command.IncrementBy);
    }
}
```

And this is really all there is to testing handlers with middleware pipelines. Simply test them as shown in the recipe for [testing command and query handlers](../testing-handlers#readme) and treat any middlewares as part of the handler's public API.

One thing to note is that as your application grows, so will your pipelines, as you will start solving more and more cross-cutting concerns with middlewares. This includes tricky concerns like authentication and authorization, which, according to our discussion above, also need to be tested as part of testing the public API of a handler. To support you in testing such handlers, we have dedicated recipes for [addressing some of the most common cross-cutting concerns](../../../../README.md#cqs-cross-cutting-concerns), which always include sections about how to test handlers.

From the recipe for [solving cross-cutting concerns with middlewares](../solving-cross-cutting-concerns#readme) you may remember our discussion of **reusable pipelines**. When using such pipelines in your handlers, you may consider writing handler tests only for the most critical middlewares (e.g. authentication and authorization) or middlewares with custom configuration. Testing the reusable pipeline itself can then be done separately, as discussed in the recipe for [testing middlewares and reusable pipelines](../testing-middlewares#readme) (which we recommend you read next).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.
