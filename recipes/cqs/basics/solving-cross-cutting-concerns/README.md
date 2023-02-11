# Conqueror recipe (CQS Basics): solving cross-cutting concerns with middlewares (e.g. validation or retrying on failure)

This recipe shows how simple it is to solve cross-cutting concerns like validation or retrying on failure for your commands and queries with **Conqueror.CQS**.

If you have not read the recipe for [getting started](../getting-started#readme) yet, we recommend you take a look at it before you start with this recipe.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/basics/solving-cross-cutting-concerns) and open the solution file in your IDE (note that you need to have [.NET 6 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the `completed` folder of the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/basics/solving-cross-cutting-concerns).

The application, to which we will be adding handling for cross-cutting concerns, is managing a set of named counters. In code, the API of our application is represented with the following types:

```cs
public sealed record IncrementCounterByCommand(string CounterName, int IncrementBy);

public sealed record IncrementCounterByCommandResponse(int NewCounterValue);

public sealed record GetCounterValueQuery(string CounterName);

public sealed record GetCounterValueQueryResponse(int CounterValue);
```

Feel free to take a look at the full code for [the command](Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/IncrementCounterByCommand.cs) and [the query](Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/GetCounterValueQuery.cs). The counters are stored in an [in-memory repository](Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/CountersRepository.cs).

The first cross-cutting concern we are going to address is validation. You may have noticed that the `IncrementBy` property of the `IncrementCounterByCommand` is an `int`, but we expect this value to be a _positive_ integer, and right now it could also be a negative number or zero. To deal with these cases we are going to add validation based on [data annotation attributes](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations?view=net-7.0).

In **Conqueror.CQS** we use middlewares (which implement the [chain-of-responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) pattern) to address these concerns. Each command and query handler is executed as part of a pipeline. The pipeline consists of a set of middlewares which are executed in order. Each middleware wraps the execution of the rest of the pipeline, and can also abort the pipeline execution (e.g. due to a validation failure).

> Middlewares (and pipelines) are separated for commands and queries, since the types of cross-cutting concerns that are relevant for each of them can be quite different (e.g. caching makes sense for queries, but not for commands). This means you may have to implement a middleware twice if you want to handle the same cross-cutting concern for both commands and queries. However, given that middlewares are tpyically written once and then used many times, the cost of implementing them is negligible in the big picture of your app's development. In addition, for many cross-cutting concerns **Conqueror.CQS** provides [pre-built middlewares](../../../../../..#conquerorcqs), so that you don't have to write them yourself. In the rest of this recipe we will only be building command middlewares, but you can see the equivalent query middlewares in the [completed recipe](.completed).

All of this is quite theoretical, so let's explore it interactively by implementing a command middleware for data annotation validation. Create a new class called `DataAnnotationValidationCommandMiddleware.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/DataAnnotationValidationCommandMiddleware.cs)):

```cs
namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal sealed class DataAnnotationValidationCommandMiddleware : ICommandMiddleware
{
    public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        // this will validate the object according to data annotation validations and
        // will throw a ValidationException if validation fails
        Validator.ValidateObject(ctx.Command, new(ctx.Command), true);

        // if validation passes, execute the rest of the pipeline
        return ctx.Next(ctx.Command, ctx.CancellationToken);
    }
}
```

> The middleware is automatically added to the application services since we are using `services.AddConquerorCQSTypesFromExecutingAssembly()` in [Program.cs](Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/Program.cs). If you are not using this assembly scanning mechanism, or want your middleware to have a different lifetime (e.g. a singleton), you have to add the middleware explicitly, e.g. `services.AddSingleton<DataAnnotationValidationCommandMiddleware>();`.

Now we can start using the middleware in our command handler. Each handler configures its own pipeline by implementing an interface (for command handlers it is `IConfigureCommandPipeline` and for query handlers it is `IConfigureQueryPipeline`). These interfaces contain a static method `ConfigurePipeline`, which takes a pipeline builder and adds middlewares to it.

> The static interface method for the pipeline configuration interfaces only works out-of-the-box if you are using .NET 7 or higher since [static virtual interface methods](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/static-virtual-interface-members) are a new feature introduced in .NET 7 / C# 11. If you are on .NET 6 you can use the **Conqueror.CQS** [analyzers](https://www.nuget.org/packages/Conqueror.CQS.Analyzers/), which contain an analyzer that enforces the static method to be present if a handler implements one of the pipeline configuration interfaces (there is also a code fix to automatically add the method). In the project for this recipe the analyzers are already added.

Let's add a pipeline configuration for our command handler in `IncrementCounterByCommandHandler.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/IncrementCounterByCommand.cs)):

```diff
  public interface IIncrementCounterByCommandHandler : ICommandHandler<IncrementCounterByCommand, IncrementCounterByCommandResponse>
  {
  }

- internal sealed class IncrementCounterByCommandHandler : IIncrementCounterByCommandHandler
+ internal sealed class IncrementCounterByCommandHandler : IIncrementCounterByCommandHandler, IConfigureCommandPipeline
  {
      private readonly CountersRepository repository;

      public IncrementCounterByCommandHandler(CountersRepository repository)
      {
          this.repository = repository;
      }
+
+     public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => 
+         pipeline.Use<DataAnnotationValidationCommandMiddleware>();

      public async Task<IncrementCounterByCommandResponse> ExecuteCommand(IncrementCounterByCommand command, CancellationToken cancellationToken = default)
      {
```

> Note that by default, middlewares can only be added once to a pipeline, which is reasonable for most kinds of middlewares. If you want to build a middleware which can be added multiple times to a pipeline, you can use `pipeline.UseAllowMultiple<MyMiddleware>();`. Also note that the `ConfigurePipeline` method is executed every time the handler is executed, meaning every handler execution gets a fresh pipeline. This also implies that the `ConfigurePipeline` method should not perform any expensive operations, otherwise it will slow down execution.

Now the `DataAnnotationValidationCommandMiddleware` will be called every time the command handler is executed. The last step to get the validation working is to add a data annotation attribute to the command's `IncrementBy` property to declare that it needs to be a positive integer:

```cs
public sealed record IncrementCounterByCommand(string CounterName, int IncrementBy)
{
    [Range(1, int.MaxValue, ErrorMessage = "invalid amount to increment by, it must be a strictly positive integer")]
    public int IncrementBy { get; } = IncrementBy;
}
```

Let's execute the app and see what happens if we provide a negative parameter value:

```txt
> dotnet run
input commands in format '<op> <counterName> [param]' (e.g. 'inc test 1' or 'get test')
available operations: get, inc
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
input commands in format '<op> <counterName> [param]' (e.g. 'inc test 1' or 'get test')
available operations: get, inc
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

What is happening here is that the in-memory repository we are using to store our counters is simulating instability by making increment operations fail every once in a while. This is something that you typically have to deal with in your applications, especially if your app communicates with other services or a database, since there are many points of failure in such a communication. Often it is possible to deal with these kinds of transient errors by simply retrying the command, although care must be taken, that the command is [idempotent](https://en.wikipedia.org/wiki/Idempotence). In the application we are building here, this is the case since the exception is thrown before any counter value is changed, and therefore we can safely retry the command when it fails. We'll do that by building a retry middleware that takes care of the intermittent errors the repository is simulating.

Create a new class called `RetryCommandMiddleware.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/RetryCommandMiddleware.cs)):

```cs
namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal sealed class RetryCommandMiddleware : ICommandMiddleware
{
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
        where TCommand : class
    {
        // retry up to 2 times if command execution fails
        var retryAttemptLimit = 2;

        var usedRetryAttempts = 0;

        while (true)
        {
            try
            {
                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
            catch
            {
                if (usedRetryAttempts >= retryAttemptLimit)
                {
                    throw;
                }

                usedRetryAttempts += 1;
            }
        }
    }
}
```

Our new middleware simply invokes the rest of the pipeline whenever an exception occurs during execution. It repeats retries up to 3 times in case the error occurs multiple times.

As the next step, add the new middleware to the command handler's pipeline. We need to be a bit careful here in that we want to add the retry middleware _after_ the validation middleware, since otherwise the retry middleware would retry on validation failures as well, which makes no sense since the validation would fail again every time.

```cs
public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
    pipeline.Use<DataAnnotationValidationCommandMiddleware>()
            .Use<RetryCommandMiddleware>();
```

Note that chaining calls like this is a recommended practice for the builder pattern, but is not required. The configuration could also be done like this:

```cs
public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
{
    pipeline.Use<DataAnnotationValidationCommandMiddleware>();
    pipeline.Use<RetryCommandMiddleware>();
}
```

Let's run the application and verify that we can now successfully increase the counter 3 times.

```txt
> dotnet run
input commands in format '<op> <counterName> [param]' (e.g. 'inc test 1' or 'get test')
available operations: get, inc
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

It works! But there are a few improvements we can still make to our new middleware. As you saw when you implemented the middleware, the maximum number of retry attempts was hardcoded in the middleware. To make the middleware more re-usable it would be better if the number of attempts could be configured from the outside.

It is quite common for middlewares to be configurable, so let's take a look at a few options for achieving that. The simplest option is to create a class which contains the configuration parameters and then inject this class into the middleware. Create a new class `RetryMiddlewareConfiguration.cs`:

```cs
namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal sealed class RetryMiddlewareConfiguration
{
    public int RetryAttemptLimit { get; set; }
}
```

In a real application this class may be populated from a configuration file (for example using the [options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)), but to keep it simple, we'll just add it to the services with a fixed value in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/Program.cs)).

```diff
  var services = new ServiceCollection();

  services.AddSingleton<CountersRepository>();
+
+ services.AddSingleton(new RetryMiddlewareConfiguration { RetryAttemptLimit = 2 });

  services.AddConquerorCQS()
          .AddConquerorCQSTypesFromExecutingAssembly()
          .FinalizeConquerorRegistrations();
```

There are two ways to access the configuration instance in our middleware. Firstly, you could just add the configuration class as a parameter to the middleware's constructor. This works, but carries a very subtle risk: if the lifetime of the middleware would be longer than that of the injected class (e.g. the middleware was a singleton and the injected class was transient), then you would run into what is known as a [captive dependency](https://blog.ploeh.dk/2014/06/02/captive-dependency/). To prevent this from happening, **Conqueror.CQS** exposes the `IServiceProvider`, from the scope in which the handler is resolved, as a property on the middleware context. This allows resolving dependencies safely regardless of the lifetime of the middleware or the handler. Let's do that in our `RetryCommandMiddleware.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/RetryCommandMiddleware.cs)):

```diff
  public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
      where TCommand : class
  {
-     // retry up to 2 times if command execution fails
-     var retryAttemptLimit = 2;
+     var retryAttemptLimit = ctx.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>().RetryAttemptLimit;

      var usedRetryAttempts = 0;
```

The ability to resolve dependencies from the service provider can be very useful, but for configuring the middleware we can do even better. One downside of the configuration approach we just implemented is, that it is not very self-documented. As user of the middleware would need to know about the existence of the configuration class to be able to use it properly. As a better alternative to this, middlewares can have an explicit configuration. We can use the same `RetryMiddlewareConfiguration` class we created, and add it as the middleware configuration in `RetryCommandMiddleware.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/RetryCommandMiddleware.cs)):

```diff
  namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

- internal sealed class RetryCommandMiddleware : ICommandMiddleware
+ internal sealed class RetryCommandMiddleware : ICommandMiddleware<RetryMiddlewareConfiguration>
  {
-     public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
+     public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, RetryMiddlewareConfiguration> ctx)
          where TCommand : class
      {
-         var retryAttemptLimit = ctx.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>().RetryAttemptLimit;
+         var retryAttemptLimit = ctx.Configuration.RetryAttemptLimit;

          var usedRetryAttempts = 0;
```

Finally, we adjust how we use the middleware in our command handler's pipeline in `IncrementCounterByCommand.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/IncrementCounterByCommand.cs)):

```cs
// the pipeline builder exposes the service provider from the
// scope in which the handler is resolved in order to allow you
// to get any services you need for configuring the pipeline
public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
    pipeline.Use<DataAnnotationValidationCommandMiddleware>()
            .Use<RetryCommandMiddleware, RetryMiddlewareConfiguration>(
              pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>());
```

That's a lot of extra code for configuring the pipeline. Fortunately, the pipeline configuration uses the [builder pattern](https://en.wikipedia.org/wiki/Builder_pattern), which, together with C#'s excellent [extension methods](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods), allows us to simplify this quite a bit. The recommended approach for providing middlewares is to accompany them with a set of extension methods for configuring pipelines. Let's add such an extension method for our retry middleware in a new class `RetryCommandMiddlewarePipelineBuilderExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/RetryCommandMiddlewarePipelineBuilderExtensions.cs)):

```cs
namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class RetryCommandMiddlewarePipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseRetry(this ICommandPipelineBuilder pipeline)
    {
        var configuration = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>();
        return pipeline.Use<RetryCommandMiddleware, RetryMiddlewareConfiguration>(configuration);
    }
}
```

While we're at it we'll also create an extension method for the data annotation validation middleware in a new class `DataAnnotationValidationCommandMiddlewarePipelineBuilderExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/DataAnnotationValidationCommandMiddlewarePipelineBuilderExtensions.cs)):

```cs
namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class DataAnnotationValidationCommandMiddlewarePipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseDataAnnotationValidation(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Use<DataAnnotationValidationCommandMiddleware>();
    }
}
```

With these changes, we can now simplify the pipeline configuration of our command handler in `IncrementCounterByCommand.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/IncrementCounterByCommand.cs)):

```cs
public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
    pipeline.UseDataAnnotationValidation()
            .UseRetry();
```

Much better. There's still room for improvement though. As it stands, all handlers would have the same retry attempt limit. However, it might be useful to allow each handler to define its own limit. This can easily be done by adding a parameter to the middleware's extension method in `RetryCommandMiddlewarePipelineBuilderExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/RetryCommandMiddlewarePipelineBuilderExtensions.cs)):

```diff
  namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

  internal static class RetryCommandMiddlewarePipelineBuilderExtensions
  {
-     public static ICommandPipelineBuilder UseRetry(this ICommandPipelineBuilder pipeline)
+     public static ICommandPipelineBuilder UseRetry(this ICommandPipelineBuilder pipeline, int? retryAttemptLimit = null)
      {
-         var configuration = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>();
+         var defaultRetryAttemptLimit = pipeline.ServiceProvider.GetRequiredService<RetryMiddlewareConfiguration>().RetryAttemptLimit;
+         var configuration = new RetryMiddlewareConfiguration { RetryAttemptLimit = retryAttemptLimit ?? defaultRetryAttemptLimit };
          return pipeline.Use<RetryCommandMiddleware, RetryMiddlewareConfiguration>(configuration);
      }
  }
```

This allows us to specify a custom retry attempt limit in our command handler's pipeline configuration in `IncrementCounterByCommand.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/IncrementCounterByCommand.cs)):

```cs
public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
    pipeline.UseDataAnnotationValidation()
            .UseRetry(retryAttemptLimit: 3);
```

There is one last improvement we can make. When building a real application you will create many command and query handlers, and likely you will want most if not all of those handlers to have the same or at least similar pipelines (so that you get consistent validation, logging, error handling etc.). The recommended approach for this is to create extension methods for the pipeline builders and configure a default pipeline in those methods. Let's do that for command handlers by creating a new class `CommandPipelineDefaultBuilderExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/CommandPipelineDefaultBuilderExtensions.cs)):

```cs
namespace Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

internal static class CommandPipelineDefaultBuilderExtensions
{
    public static ICommandPipelineBuilder UseDefault(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.UseDataAnnotationValidation()
                       .UseRetry();
    }
}
```

We can now change our command handler pipeline to use this new default pipeline in `IncrementCounterByCommand.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/IncrementCounterByCommand.cs)):

```cs
public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
    pipeline.UseDefault();
```

This approach is called **reusable pipelines**. Most of your applications will likely have one default pipeline for command and query handlers and then a few other reusable pipelines for specific use cases. In general, pipelines are composable, meaning you can also wrap a set of middlewares into a reusable pipeline and then combine this pipeline with other pipelines into a new reusable pipeline and so on. There are no limits to your creativity for how to structure your pipelines, but we recommend providing at least one simple-to-use default pipeline.

There are a few limitations to reusable pipelines that we'll address next.

Firstly, in our default pipeline example above, we lost the ability to provide a custom retry attempt limit per handler. Secondly, we may have handlers which don't need retry capabilities, but want to make use of the rest of the default pipeline (or any other reusable pipeline). Lastly, you may want to inject a middleware into the middle of a reusable pipeline. All of these concerns could be addressed by adding parameters to the reusable pipeline method, but this would quickly grow out of hand as the number of middlewares in the pipeline increases.

Therefore, **Conqueror.CQS** offers a way to configure middlewares on a reusable pipeline as well as for removing middlewares from such a pipeline. The only aspect for which we don't provide a built-in solution is injecting a middleware into the middle of a pipeline. We'll discuss the reasons behind this below, but first we will take a look at the other two aspects.

Let's add two new extension methods `ConfigureRetry` and `WithoutRetry` in `RetryCommandMiddlewarePipelineBuilderExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/RetryCommandMiddlewarePipelineBuilderExtensions.cs)):

```cs
public static ICommandPipelineBuilder ConfigureRetry(this ICommandPipelineBuilder pipeline, Action<RetryMiddlewareConfiguration> configure)
{
    return pipeline.Configure<RetryCommandMiddleware, RetryMiddlewareConfiguration>(configure);
}

public static ICommandPipelineBuilder WithoutRetry(this ICommandPipelineBuilder pipeline)
{
    return pipeline.Without<RetryCommandMiddleware, RetryMiddlewareConfiguration>();
}
```

These new methods allow modifying the default pipeline without changing the implementation of `UseDefault`. Here you can see how these methods could be used in a pipeline configuration:

```cs
// set a custom retry limit on the default pipeline
public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
    pipeline.UseDefault()
            .ConfigureRetry(o => o.RetryAttemptLimit = 3);

// use the default pipeline without the retry middleware
public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
    pipeline.UseDefault()
            .WithoutRetry();
```

The extension method triplet of `UseX`, `ConfigureX`, and `WithoutX` is a recommended convention, but there is no limit to your imagination for what kind of methods you can create (the recipes for [addressing specific cross-cutting concerns](../../../../README.md#cqs-cross-cutting-concerns) contain examples of other useful extension methods).

Lastly, let's discuss how you could allow injecting middlewares into the middle of a reusable pipeline. During development of **Conqueror.CQS** we considered various APIs for achieving this generically, but determined that it would add too much complexity to the API and would make pipelines too brittle. Therefore, this is something that you need to explicitly build into your reusable pipelines, for example by providing explicit points in the pipeline into which middlewares could be injected. Let's assume you would want to allow a developer to inject a middleware in between the data annotation validation and retry middlewares in our default pipeline above. To achieve this, the default pipeline could be built like this:

```cs
public static ICommandPipelineBuilder UseDefault(this ICommandPipelineBuilder pipeline,
                                                 Action<ICommandPipelineBuilder>? preRetryHook = null)
{
    pipeline.UseDataAnnotationValidation();

    preRetryHook?.Invoke(pipeline);

    return pipeline.UseRetry();
}
```

The default pipeline could then be used like this:

```cs
public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
    pipeline.UseDefault(preRetryHook: p => p.UseMyOtherMiddleware());
```

This approach allows you to control exactly where additional middlewares could be injected. You could also build conditionals or other control structures into the reusable pipeline method, but always consider whether the reusability of the pipeline is worth the extra complexity in its definition. Maybe it is good enough to simply specify a dedicated pipeline directly in the handler which requires the extra middleware.

This concludes our recipe for solving cross-cutting concerns with **Conqueror.CQS**. In summary, these are the steps:

- build your own middleware or add a package reference to one of the [pre-built middlewares](../../../../../..#conquerorcqs)
- write custom extension methods for your own or even pre-built middlewares to customize how they are added to handler pipelines
- create default pipelines to align pipelines across all your handlers

As the next step we recommend that you explore how to [test command and query handlers that have pipelines](../testing-handlers-with-pipelines#readme) as well as how to [test middlewares themselves](../testing-middlewares#readme). Another useful recipe is about [re-using middleware pipelines to solve cross-cutting concerns when calling external systems](../../advanced/reuse-piplines-for-external-calls#readme).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.
