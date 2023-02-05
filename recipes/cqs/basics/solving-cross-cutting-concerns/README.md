# Conqueror recipe (CQS Basics): solving cross-cutting concerns with middlewares (e.g. validation or retrying on failure)

This recipe shows how simple it is to solve cross-cutting concerns like validation or retrying on failure for your commands and queries with **Conqueror.CQS**.

If you have not read the recipe for [getting started](../getting-started#readme) yet, we recommend you take a look at it before you start with this recipe.

> This recipe is designed to allow you to code along. [Download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/basics/solving-cross-cutting-concerns) and open the solution file in your IDE (note that you need to have [.NET 6 or later](https://dotnet.microsoft.com/en-us/download) installed). If you prefer to just view the completed code directly, you can do so [in your browser](.completed) or with your IDE in the solution [downloaded as part of the folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/basics/solving-cross-cutting-concerns).

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
// TODO: add middleware
```

> The middleware is automatically added to the application services since we are using `services.AddConquerorCQSTypesFromExecutingAssembly()` in [Program.cs](Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/Program.cs). If you are not using this assembly scanning mechanism, or want your middleware to have a different lifetime (e.g. a singleton), you have to add the middleware explicitly, e.g. `services.AddSingleton<DataAnnotationValidationCommandMiddleware>();`.

Now we can start using the middleware in our command handler. Each handler configures its own pipeline by implementing an interface (for command handlers it is `IConfigureCommandPipeline` and for query handlers it is `IConfigureQueryPipeline`). These interfaces contain a static method `ConfigurePipeline`, which takes a pipeline builder and adds middlewares to it. Let's do that for the [IncrementCounterByCommandHandler](Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/IncrementCounterByCommand.cs):

> The static interface method for the pipeline configuration interfaces only works out-of-the-box if you are using .NET 7 or higher since [static virtual interface methods](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/static-virtual-interface-members) are a new feature introduced in .NET 7 / C# 11. If you are on .NET 6 you can use the **Conqueror.CQS** [analyzers](https://www.nuget.org/packages/Conqueror.CQS.Analyzers/), which contain an analyzer that enforces the static method to be present if a handler implements one of the pipeline configuration interfaces (there is also a code fix to automatically add the method). In the project for this recipe the analyzers are already added.

```diff
// TODO: add diff for adding pipeline configuration to handler
```

> Note that by default, middlewares can only be added once to a pipeline, which is reasonable for most kinds of middlewares. If you want to build a middleware which can be added multiple times to a pipeline, you can use `pipeline.UseAllowMultiple<MyMiddleware>();`.

Now the `DataAnnotationValidationCommandMiddleware` will be called every time the command handler is executed. The last step to get the validation working is to add a data annotation attribute to the command's `IncrementBy` property to declare that it needs to be a positive integer:

```cs
// TODO: command with annotiation
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
-1 is an invalid amount to increment by
q
shutting down...
```

It's working great. However, if you execute three increments in sequence, something unexpected happens:

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
// TODO: add middleware
```

Our new middleware simply invokes the rest of the pipeline whenever an exception occurs during execution. It repeats retries up to 3 times in case the error occurs multiple times.

As the next step, add the new middleware to the command handler's pipeline:

```diff
// TODO: add diff for adding middleware to pipeline
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

It is quite common for middlewares to be configurable, so let's take a look at a few options for achieving that. The simplest option is to create a class which contains the configuration parameters and then inject this class into the middleware. Create a new class `RetryConfiguration.cs`:

```cs
// TODO: configuration class
```

In a real application this class may be populated from a configuration file (for example using the [options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)), but to keep it simple, we'll just add it to the services with a fixed value in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/Program.cs)).

```diff
// TODO: add configuration class to services
```

There are two ways to access the configuration instance in our middleware. Firstly, you could just add the configuration class as a parameter to the middleware's constructor. This works, but carries a very subtle risk: if the lifetime of the middleware would be longer than that of the injected class (e.g. the middleware was a singleton and the injected class was transient), then you would run into what is known as a [captive dependency](https://blog.ploeh.dk/2014/06/02/captive-dependency/). To prevent this from happening, **Conqueror.CQS** exposes the `IServiceProvider`, from the scope in which the handler is resolved, as a property on the middleware context. This allows resolving dependencies safely regardless of the lifetime of the middleware or the handler. Let's do that in our `RetryCommandMiddleware.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/RetryCommandMiddleware.cs)):

```diff
// TODO: resolve configuration instance from context
```

The ability to resolve dependencies from the service provider can be very useful, but for configuring the middleware we can do even better. One downside of the configuration approach we just implemented is, that it is not very self-documented. As user of the middleware would need to know about the existence of the configuration class to be able to use it properly. As a better alternative to this, middlewares can have an explicit configuration. Create a new class `RetryCommandMiddlewareConfiguration.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/RetryCommandMiddlewareConfiguration.cs)):

```cs
// TODO: configuration class
```

We also need to slightly adjust our middleware in `RetryCommandMiddleware.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/RetryCommandMiddleware.cs)):

```diff
// TODO: add configuration to the retry middleware
```

Finally, we adjust how we use the middleware in our command handler's pipeline in `IncrementCounterByCommand.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/IncrementCounterByCommand.cs)):

```diff
// TODO: adjust handler pipeline configuration
```

That's a lot of extra code for configuring the pipeline. Fortunately, the pipeline configuration uses the [builder pattern](https://en.wikipedia.org/wiki/Builder_pattern), which, together with C#'s excellent [extension methods](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods), allows us to simplify this quite a bit. The recommended approach for providing middlewares is to accompany them with a set of extension methods for configuring pipelines. Let's add such an extension method for our retry middleware in a new class `RetryCommandMiddlewarePipelineBuilderExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/RetryCommandMiddlewarePipelineBuilderExtensions.cs)):

```cs
// TODO: configuration extension methods
```

With this change, we can now simplify the usage in our command handler's pipeline configuration in `IncrementCounterByCommand.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/IncrementCounterByCommand.cs)):

```diff
// TODO: adjust handler pipeline configuration
```

Much better. There's still room for improvement though. As it stands, all handlers would have the same retry attempt limit. However, it might be useful to allow each handler to define its own limit. This can easily be done by adding a parameter to the middleware's extension method in `RetryCommandMiddlewarePipelineBuilderExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/RetryCommandMiddlewarePipelineBuilderExtensions.cs)):

```diff
// TODO: add configuration parameter
```

This allows us to specify a custom retry attempt limit in our command handler's pipeline configuration in `IncrementCounterByCommand.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/IncrementCounterByCommand.cs)):

```diff
// TODO: adjust handler pipeline configuration
```

There is one last improvement we can make. When building a real application you will create many command and query handlers, and likely you will want most if not all of those handlers to have the same or at least similar pipelines. The recommended approach for this is to create extension methods for the pipeline builders and configure a default pipeline in those methods. Let's do that for command handlers by creating a new class `CommandPipelineDefaultBuilderExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/CommandPipelineDefaultBuilderExtensions.cs)):

```cs
// TODO: extension method
```

We can now change our command handler pipeline to use this new default pipeline in `IncrementCounterByCommand.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/IncrementCounterByCommand.cs)):

```diff
// TODO: adjust handler pipeline configuration
```

There are two limitations to this approach. Firstly, we lost the ability to provide a custom retry attempt limit per handler, and secondly, we may have handlers which don't need retry capabilities, but want to make use of the rest of the default pipeline. Both of these concerns could be addressed by adding parameters to the `UseDefault` method, but this would quickly grow out of hand. Instead, **Conqueror.CQS** offers a few more methods for pipeline builders that allow for a simpler and more composable solution. Let's add two new extension methods in `RetryCommandMiddlewarePipelineBuilderExtensions.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns/RetryCommandMiddlewarePipelineBuilderExtensions.cs)):

```cs
// TODO: new extension methods
```

These new methods allow modifying the default pipeline without changing the implementation of `UseDefault`. Here you can see how these methods could be used in a pipeline configuration:

```cs
// TODO: usage examples
```

All of these extensions methods are recommended conventions, but there is no limit to your imagination for what kind of methods you can create (the recipes for [addressing specific cross-cutting concerns](../../../../README.md#cqs-cross-cutting-concerns) contain examples of other useful extension methods).

This concludes our recipe for solving cross-cutting concerns with **Conqueror.CQS**. In summary, these are the steps:

- build your own middleware or add a package reference to one of the [pre-built middlewares](../../../../../..#conquerorcqs)
- write custom extension methods for your own or even pre-built middlewares to customize how they are added to handler pipelines
- create default pipelines to align pipelines across all your handlers

As the next step we recommend that you explore how to [test command and query handlers that have pipelines](../testing-handlers-with-pipelines#readme) as well as how to [test middlewares themselves](../testing-middlewares#readme). Another useful recipe is about [re-using middleware pipelines to solve cross-cutting concerns when calling external systems](../../advanced/reuse-piplines-for-external-calls#readme).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.
