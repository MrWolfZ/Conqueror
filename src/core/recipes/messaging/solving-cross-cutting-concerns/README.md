# Conqueror recipe (Messaging): solving cross-cutting concerns with middlewares (e.g. validation or retrying on failure)

This recipe shows how simple it is to solve cross-cutting concerns like validation or retrying on failure for your messages with **Conqueror**.

If you have not read the recipe for [getting started](../getting-started#readme) yet, we recommend you take a look at it before you start with this recipe.

> This recipe is designed to allow you to code along. If you prefer to just see the completed code directly, you can either view it directly [in your browser](.completed) or you can [download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/core/recipes/messaging/solving-cross-cutting-concerns) and open the solution to view the code in your IDE. Note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed.

The application, to which we will be adding handling for cross-cutting concerns, is managing a set of named counters. In code, the API of our application is represented with the following types:

```cs
[Message<IncrementCounterByResponse>]
public partial record IncrementCounterBy(string CounterName, int IncrementBy);

public record IncrementCounterByResponse(int NewCounterValue);

[Message<GetCounterValueResponse>]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(int CounterValue);
```

Feel free to take a look at the full code for [incrementing a counter](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/IncrementCounterBy.cs) and [getting a counter's value](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/GetCounterValue.cs). The counters are stored in an [in-memory repository](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/CountersRepository.cs).

The first cross-cutting concern we are going to address is validation. You may have noticed that the `IncrementBy` property of the `IncrementCounterBy` message is an `int`, but we expect this value to be a _positive_ integer, and right now it could also be a negative number or zero. To deal with these cases we are going to add validation based on [data annotation attributes](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations).

In **Conqueror** we use middlewares (which implement the [chain-of-responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) pattern) to address these concerns. Each message handler is executed as part of a pipeline. The pipeline consists of a set of middlewares which are executed in order. Each middleware wraps the execution of the rest of the pipeline, and can also abort the pipeline execution (e.g. due to a validation failure).

> A middleware is generic over the message and response type, so a single middleware implementation works for every one of your handlers. Whether a message reads or writes data is just a convention, so you are free to apply different middlewares to different handlers (e.g. only adding caching to handlers that read data). For many common cross-cutting concerns **Conqueror** ships [pre-built middlewares](../../../../../..#recipes), so that you don't have to write them yourself.

All of this is quite theoretical, so let's explore it interactively by implementing a middleware for data annotation validation. Create a new class called `DataAnnotationValidationMiddleware.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/DataAnnotationValidationMiddleware.cs)):

```cs
namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

internal class DataAnnotationValidationMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        // this will validate the object according to data annotation attributes and
        // will throw a ValidationException if validation fails
        Validator.ValidateObject(ctx.Message, new ValidationContext(ctx.Message), validateAllProperties: true);

        // if validation passes, execute the rest of the pipeline
        return ctx.Next(ctx.Message, ctx.CancellationToken);
    }
}
```

Now we can start using the middleware in our message handler. Each handler configures its own pipeline by implementing a static method `ConfigurePipeline`, which takes a pipeline and adds middlewares to it.

Let's add a pipeline configuration for our handler in `IncrementCounterBy.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/IncrementCounterBy.cs)):

```diff
  internal partial class IncrementCounterByHandler(CountersRepository repository) : IncrementCounterBy.IHandler
  {
+     public static void ConfigurePipeline(IncrementCounterBy.IPipeline pipeline) =>
+         pipeline.Use(new DataAnnotationValidationMiddleware<IncrementCounterBy, IncrementCounterByResponse>());

      public async Task<IncrementCounterByResponse> Handle(IncrementCounterBy message, CancellationToken cancellationToken = default)
      {
```

> Note that the `ConfigurePipeline` method is executed every time the handler is executed, meaning every handler execution gets a fresh pipeline. This also implies that the `ConfigurePipeline` method should not perform any expensive operations, otherwise it will slow down execution.

Now the `DataAnnotationValidationMiddleware` will be called every time the message handler is executed. The last step to get the validation working is to add a data annotation attribute to the message's `IncrementBy` property to declare that it needs to be a positive integer:

```cs
[Message<IncrementCounterByResponse>]
public partial record IncrementCounterBy(string CounterName, int IncrementBy)
{
    [Range(1, int.MaxValue, ErrorMessage = "invalid amount to increment by, it must be a strictly positive integer")]
    public int IncrementBy { get; } = IncrementBy;
}
```

Let's execute the app and see what happens if we provide a negative parameter value:

```txt
> dotnet run
input commands in format '<op> [counterName] [param]' (e.g. 'inc test 1' or 'get test')
available operations: inc, get
input q to quit
inc test 1
incremented counter 'test'; new value: 1
inc test -1
invalid amount to increment by, it must be a strictly positive integer
q
shutting down...
```

The validation is working as expected. However, if you execute three increments in sequence, something unexpected happens:

```txt
> dotnet run
input commands in format '<op> [counterName] [param]' (e.g. 'inc test 1' or 'get test')
available operations: inc, get
input q to quit
inc test 1
incremented counter 'test'; new value: 1
inc test 1
incremented counter 'test'; new value: 2
inc test 1
an unexpected error occurred while executing operation
q
shutting down...
```

What is happening here is that the [in-memory repository](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/CountersRepository.cs) we are using to store our counters, is simulating instability by making increment operations fail every once in a while. This is something that you typically have to deal with in your applications, especially if your app communicates with other services or a database, since there are many points of failure in such a communication. Often it is possible to deal with these kinds of transient errors by simply retrying the message, although care must be taken, that the message is [idempotent](https://en.wikipedia.org/wiki/Idempotence). In the application we are building here, this is the case since the exception is thrown before any counter value is changed, and therefore we can safely retry the message when it fails. We'll do that by building a retry middleware that takes care of the intermittent errors the repository is simulating.

Create a new class called `RetryMiddleware.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/RetryMiddleware.cs)):

```cs
namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

// in a real application, instead use https://www.nuget.org/packages/Conqueror.Middleware.Polly
internal class RetryMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        // retry up to 2 times if message execution fails
        var retryAttemptLimit = 2;

        var usedRetryAttempts = 0;

        while (true)
        {
            try
            {
                return await ctx.Next(ctx.Message, ctx.CancellationToken);
            }
            catch when (usedRetryAttempts < retryAttemptLimit)
            {
                usedRetryAttempts += 1;
            }
        }
    }
}
```

Our new middleware simply invokes the rest of the pipeline whenever an exception occurs during execution. The [exception filter](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/exception-handling-statements#exception-filters) only catches the exception while retry attempts remain; once they are exhausted, the exception is allowed to propagate.

As the next step, add the new middleware to the handler's pipeline. We need to be a bit careful here in that we want to add the retry middleware _after_ the validation middleware, since otherwise the retry middleware would retry on validation failures as well, which makes no sense since the validation would fail again every time.

```cs
public static void ConfigurePipeline(IncrementCounterBy.IPipeline pipeline) =>
    pipeline.Use(new DataAnnotationValidationMiddleware<IncrementCounterBy, IncrementCounterByResponse>())
            .Use(new RetryMiddleware<IncrementCounterBy, IncrementCounterByResponse>());
```

Note that chaining calls like this is a recommended practice for the builder pattern, but is not required. The configuration could also be done like this:

```cs
public static void ConfigurePipeline(IncrementCounterBy.IPipeline pipeline)
{
    pipeline.Use(new DataAnnotationValidationMiddleware<IncrementCounterBy, IncrementCounterByResponse>());
    pipeline.Use(new RetryMiddleware<IncrementCounterBy, IncrementCounterByResponse>());
}
```

Let's run the application and verify that we can now successfully increase the counter 3 times.

```txt
> dotnet run
input commands in format '<op> [counterName] [param]' (e.g. 'inc test 1' or 'get test')
available operations: inc, get
input q to quit
inc test 1
incremented counter 'test'; new value: 1
inc test 1
incremented counter 'test'; new value: 2
inc test 1
incremented counter 'test'; new value: 3
q
shutting down...
```

It works! But there are a few improvements we can still make to our new middleware. As you saw when you implemented the middleware, the maximum number of retry attempts was hardcoded in the middleware's body. To make the middleware more re-usable, it would be better if the number of attempts could be configured from the outside.

It is quite common for middlewares to be configurable, so let's take a look at a few options for achieving that. The simplest option is to create a class which contains the configuration parameters and then pass an instance of this into the middleware. Create a new class `RetryMiddlewareConfiguration.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/RetryMiddlewareConfiguration.cs)):

```cs
namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

internal class RetryMiddlewareConfiguration
{
    public int RetryAttemptLimit { get; set; }
}
```

In a real application this class may be populated from a configuration file (for example using the [options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)), but to keep it simple, we'll just add it to the services with a fixed value in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/Program.cs)).

```diff
  var services = new ServiceCollection();

  services.AddSingleton<CountersRepository>();
+
+ services.AddSingleton(new RetryMiddlewareConfiguration { RetryAttemptLimit = 2 });

  services.AddMessageHandlersFromAssembly(typeof(Program).Assembly);
```

To access this configuration in the middleware, we can use the fact that **Conqueror** exposes the `IServiceProvider`, from the scope in which the handler is resolved, as a property on the middleware context. Let's do that in our `RetryMiddleware.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/RetryMiddleware.cs)):

```diff
  public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
  {
-     // retry up to 2 times if message execution fails
-     var retryAttemptLimit = 2;
+     var retryAttemptLimit = ctx.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>().RetryAttemptLimit;

      var usedRetryAttempts = 0;
```

The ability to resolve dependencies from the service provider can be very useful, but for configuring the middleware we can do even better. One downside of the configuration approach we just implemented is, that it is not very self-documented. A user of the middleware would need to know about the existence of the configuration class to be able to use it properly. As a better alternative to this, middlewares can have an explicit configuration. We can use the same `RetryMiddlewareConfiguration` class we created, and add it as the middleware configuration in `RetryMiddleware.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/RetryMiddleware.cs)):

```diff
  internal class RetryMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
      where TMessage : class, IMessage<TMessage, TResponse>
  {
+     public required RetryMiddlewareConfiguration Configuration { get; init; }
+
      public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
      {
-         var retryAttemptLimit = ctx.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>().RetryAttemptLimit;
+         var retryAttemptLimit = Configuration.RetryAttemptLimit;

          var usedRetryAttempts = 0;
```

Finally, we adjust how we use the middleware in our handler's pipeline in `IncrementCounterBy.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/IncrementCounterBy.cs)):

```cs
// the pipeline exposes the service provider from the
// scope in which the handler is resolved in order to allow you
// to get any services you need for configuring the pipeline
public static void ConfigurePipeline(IncrementCounterBy.IPipeline pipeline) =>
    pipeline.Use(new DataAnnotationValidationMiddleware<IncrementCounterBy, IncrementCounterByResponse>())
            .Use(new RetryMiddleware<IncrementCounterBy, IncrementCounterByResponse>
            {
                Configuration = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>(),
            });
```

That's a lot of extra code for configuring the pipeline. Fortunately, the pipeline uses the [builder pattern](https://en.wikipedia.org/wiki/Builder_pattern), which, together with C#'s excellent [extension methods](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods), allows us to simplify this quite a bit. The recommended approach for providing middlewares is to accompany them with a set of extension methods for configuring pipelines. Let's add such an extension method for our retry middleware in a new class `RetryMiddlewarePipelineExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/RetryMiddlewarePipelineExtensions.cs)):

```cs
namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

internal static class RetryMiddlewarePipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseRetry<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var configuration = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>();
        return pipeline.Use(new RetryMiddleware<TMessage, TResponse> { Configuration = configuration });
    }
}
```

While we're at it we'll also create an extension method for the data annotation validation middleware in a new class `DataAnnotationValidationMiddlewarePipelineExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/DataAnnotationValidationMiddlewarePipelineExtensions.cs)):

```cs
namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

internal static class DataAnnotationValidationMiddlewarePipelineExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseDataAnnotationValidation<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Use(new DataAnnotationValidationMiddleware<TMessage, TResponse>());
    }
}
```

With these changes, we can now simplify the pipeline configuration of our handler in `IncrementCounterBy.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/IncrementCounterBy.cs)):

```cs
public static void ConfigurePipeline(IncrementCounterBy.IPipeline pipeline) =>
    pipeline.UseDataAnnotationValidation()
            .UseRetry();
```

Much better. There's still room for improvement though. As it stands, all handlers would have the same retry attempt limit. However, it might be useful to allow each handler to define its own limit. This can easily be done by adding a parameter to the middleware's extension method in `RetryMiddlewarePipelineExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/RetryMiddlewarePipelineExtensions.cs)):

```diff
  namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

  internal static class RetryMiddlewarePipelineExtensions
  {
-     public static IMessagePipeline<TMessage, TResponse> UseRetry<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
+     public static IMessagePipeline<TMessage, TResponse> UseRetry<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline, int? retryAttemptLimit = null)
          where TMessage : class, IMessage<TMessage, TResponse>
      {
-         var configuration = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>();
+         var defaultRetryAttemptLimit = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>().RetryAttemptLimit;
+         var configuration = new RetryMiddlewareConfiguration { RetryAttemptLimit = retryAttemptLimit ?? defaultRetryAttemptLimit };
          return pipeline.Use(new RetryMiddleware<TMessage, TResponse> { Configuration = configuration });
      }
  }
```

This allows us to specify a custom retry attempt limit in our handler's pipeline configuration in `IncrementCounterBy.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/IncrementCounterBy.cs)):

```cs
public static void ConfigurePipeline(IncrementCounterBy.IPipeline pipeline) =>
    pipeline.UseDataAnnotationValidation()
            .UseRetry(retryAttemptLimit: 3);
```

There is one last improvement we can make. When building a real application you will create many message handlers, and likely you will want most if not all of those handlers to have the same or at least similar pipelines (so that you get consistent validation, logging, error handling etc.). The recommended approach for this is to create extension methods for the pipeline and configure a default pipeline in those methods. Let's do that by creating a new class `MessagePipelineDefaultExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/MessagePipelineDefaultExtensions.cs)):

```cs
namespace Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

internal static class MessagePipelineDefaultExtensions
{
    public static IMessagePipeline<TMessage, TResponse> UseDefault<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.UseDataAnnotationValidation()
                       .UseRetry();
    }
}
```

We can now change our handler pipeline to use this new default pipeline in `IncrementCounterBy.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/IncrementCounterBy.cs)):

```cs
public static void ConfigurePipeline(IncrementCounterBy.IPipeline pipeline) =>
    pipeline.UseDefault();
```

This approach is called **reusable pipelines**. Most of your applications will likely have one default pipeline for handlers and then a few other reusable pipelines for specific use cases. In general, pipelines are composable, meaning you can also wrap a set of middlewares into a reusable pipeline and then combine this pipeline with other pipelines into a new reusable pipeline and so on. There are no limits to your creativity for how to structure your pipelines, but we recommend providing at least one simple-to-use default pipeline.

There are a few limitations to reusable pipelines that we'll address next.

Firstly, in our default pipeline example above, we lost the ability to provide a custom retry attempt limit per handler. Secondly, we may have handlers which don't need retry capabilities, but want to make use of the rest of the default pipeline (or any other reusable pipeline). Lastly, you may want to inject a middleware into the middle of a reusable pipeline. All of these concerns could be addressed by adding parameters to the reusable pipeline method, but this would quickly grow out of hand as the number of middlewares in the pipeline increases.

Therefore, **Conqueror** offers a way to configure middlewares on a reusable pipeline as well as for removing middlewares from such a pipeline. The only aspect for which we don't provide a built-in solution is injecting a middleware into the middle of a pipeline. We'll discuss the reasons behind this below, but first we will take a look at the other two aspects.

Let's add two new extension methods `ConfigureRetry` and `WithoutRetry` in `RetryMiddlewarePipelineExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns/RetryMiddlewarePipelineExtensions.cs)):

```cs
public static IMessagePipeline<TMessage, TResponse> ConfigureRetry<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline,
                                                                                        Action<RetryMiddlewareConfiguration> configure)
    where TMessage : class, IMessage<TMessage, TResponse>
{
    return pipeline.Configure<RetryMiddleware<TMessage, TResponse>>(m => configure(m.Configuration));
}

public static IMessagePipeline<TMessage, TResponse> WithoutRetry<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline)
    where TMessage : class, IMessage<TMessage, TResponse>
{
    return pipeline.Without<RetryMiddleware<TMessage, TResponse>>();
}
```

These new methods allow modifying the default pipeline without changing the implementation of `UseDefault`. Here you can see how these methods could be used in a pipeline configuration:

```cs
// set a custom retry limit on the default pipeline
public static void ConfigurePipeline(IncrementCounterBy.IPipeline pipeline) =>
    pipeline.UseDefault()
            .ConfigureRetry(o => o.RetryAttemptLimit = 3);

// use the default pipeline without the retry middleware
public static void ConfigurePipeline(IncrementCounterBy.IPipeline pipeline) =>
    pipeline.UseDefault()
            .WithoutRetry();
```

The extension method triplet of `UseX`, `ConfigureX`, and `WithoutX` is a recommended convention, but there is no limit to your imagination for what kind of methods you can create.

Lastly, let's discuss how you could allow injecting middlewares into the middle of a reusable pipeline. During development of **Conqueror** we considered various APIs for achieving this generically, but determined that it would add too much complexity to the API and would make pipelines too brittle. Therefore, this is something that you need to explicitly build into your reusable pipelines, for example by providing explicit points in the pipeline into which middlewares could be injected. Let's assume you would want to allow a developer to inject a middleware in between the data annotation validation and retry middlewares in our default pipeline above. To achieve this, the default pipeline could be built like this:

```cs
public static IMessagePipeline<TMessage, TResponse> UseDefault<TMessage, TResponse>(this IMessagePipeline<TMessage, TResponse> pipeline,
                                                                                    Action<IMessagePipeline<TMessage, TResponse>>? preRetryHook = null)
    where TMessage : class, IMessage<TMessage, TResponse>
{
    pipeline.UseDataAnnotationValidation();

    preRetryHook?.Invoke(pipeline);

    return pipeline.UseRetry();
}
```

The default pipeline could then be used like this:

```cs
public static void ConfigurePipeline(IncrementCounterBy.IPipeline pipeline) =>
    pipeline.UseDefault(preRetryHook: p => p.UseMyOtherMiddleware());
```

This approach allows you to control exactly where additional middlewares could be injected. You could also build conditionals or other control structures into the reusable pipeline method, but always consider whether the reusability of the pipeline is worth the extra complexity in its definition. Maybe it is good enough to simply specify a dedicated pipeline directly in the handler which requires the extra middleware.

This concludes our recipe for solving cross-cutting concerns with **Conqueror**. In summary, these are the steps:

- build your own middleware or add a package reference to one of the pre-built middlewares (e.g. [Conqueror.Middleware.Logging](https://www.nuget.org/packages/Conqueror.Middleware.Logging) or [Conqueror.Middleware.Polly](https://www.nuget.org/packages/Conqueror.Middleware.Polly))
- write custom extension methods for your own or even pre-built middlewares to customize how they are added to handler pipelines
- configure a middleware pipeline for each handler
- create reusable pipelines to share them across all your handlers

As the next step we recommend that you explore how to [test message handlers that have pipelines](../testing-handlers-with-pipelines#readme) as well as how to [test middlewares themselves](../testing-middlewares#readme).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.messaging.solving-cross-cutting-concerns]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
