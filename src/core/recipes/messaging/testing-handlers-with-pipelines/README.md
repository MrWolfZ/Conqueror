# Conqueror recipe (Messaging): testing handlers that have middleware pipelines

This recipe guides you in how to test message handlers which have middleware pipelines with **Conqueror**. If you are looking for guidance on how to test your own custom middlewares, there's [a dedicated recipe](../testing-middlewares#readme) for that.

If you have not yet read the recipes for [solving cross-cutting concerns with middlewares](../solving-cross-cutting-concerns#readme) or [testing handlers](../testing-handlers#readme), we recommend you take a look at those before you start with this recipe.

> This recipe is a bit different than others because we are not going to write any code (but we'll look at some code examples).

From the recipe about [testing handlers](../testing-handlers#readme) you may recall our advice that handlers should always be tested through their public API, invoked via `IMessageSenders` by calling `senders.For(MessageType.T)`. **The same advice is still true for handlers which have middleware pipelines**.

Let's take a look at the `IncrementCounterByHandler` from the [recipe about solving cross-cutting concerns](../solving-cross-cutting-concerns#readme). It configures a reusable default pipeline which, among other things, performs data annotation validation.

```cs
internal partial class IncrementCounterByHandler(CountersRepository repository) : IncrementCounterBy.IHandler
{
    public static void ConfigurePipeline(IncrementCounterBy.IPipeline pipeline) =>
        pipeline.UseDefault()
                .ConfigureRetry(o => o.RetryAttemptLimit = 3);

    public async Task<IncrementCounterByResponse> Handle(IncrementCounterBy message, CancellationToken cancellationToken = default)
    {
        var counterValue = await repository.GetCounterValue(message.CounterName);
        await repository.SetCounterValue(message.CounterName, counterValue + message.IncrementBy);
        return new IncrementCounterByResponse(counterValue + message.IncrementBy);
    }
}
```

The default pipeline includes the data annotation validation middleware we built in that recipe, which is fairly trivial, since it simply invokes the validation of the message in a single line of code.

```cs
internal class DataAnnotationValidationMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        Validator.ValidateObject(ctx.Message, new ValidationContext(ctx.Message), validateAllProperties: true);
        return ctx.Next(ctx.Message, ctx.CancellationToken);
    }
}
```

Now let's think about how we would test the handler. We would certainly want to write functional tests which assert that counters can correctly be incremented with this message (which happens in the body of the `Handle` method). However, we also would want to assert that invalid messages lead to a validation error. How can we do this? By simply executing the message handler like we would normally do, but with an invalid message, and then asserting that the execution throws an exception.

```cs
[Test]
public async Task GivenNonExistingCounter_WhenIncrementingCounterByNegativeAmount_ValidationExceptionIsThrown()
{
    await using var host = TestHost.Create();

    var handler = host.MessageSenders.For(IncrementCounterBy.T);

    Assert.ThrowsAsync<ValidationException>(() => handler.Handle(new(TestCounterName, -1)));
}
```

The critical insight here is that when looking at a handler from the outside, it does not matter whether a behavior is coded directly into its `Handle` method or whether it is added with a middleware in the pipeline. In essence, this means that **the middleware pipeline of a handler becomes an inseparable part of its public API**. In our case this means we treat the handler as if it was written without a pipeline like this:

```cs
internal partial class IncrementCounterByHandler(CountersRepository repository) : IncrementCounterBy.IHandler
{
    public async Task<IncrementCounterByResponse> Handle(IncrementCounterBy message, CancellationToken cancellationToken = default)
    {
        Validator.ValidateObject(message, new ValidationContext(message), validateAllProperties: true);

        var counterValue = await repository.GetCounterValue(message.CounterName);
        await repository.SetCounterValue(message.CounterName, counterValue + message.IncrementBy);
        return new IncrementCounterByResponse(counterValue + message.IncrementBy);
    }
}
```

And this is really all there is to testing handlers with middleware pipelines. Simply test them as shown in the recipe for [testing handlers](../testing-handlers#readme) and treat any middlewares as part of the handler's public API.

One thing to note is that as your application grows, so will your pipelines, as you will start solving more and more cross-cutting concerns with middlewares. This includes tricky concerns like authentication and authorization, which, according to our discussion above, also need to be tested as part of testing the public API of a handler. To support you in testing such handlers, our [other recipes](../../../../../..#recipes) cover addressing common cross-cutting concerns.

From the recipe for [solving cross-cutting concerns with middlewares](../solving-cross-cutting-concerns#readme) you may remember our discussion of **reusable pipelines**. When using such pipelines in your handlers, you may consider writing handler tests only for the most critical middlewares (e.g. authentication and authorization) or middlewares with custom configuration. Testing the reusable pipeline itself can then be done separately, as discussed in the recipe for [testing middlewares and reusable pipelines](../testing-middlewares#readme) (which we recommend you read next).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.messaging.testing-handlers-with-pipelines]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
