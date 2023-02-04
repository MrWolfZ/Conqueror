# Conqueror recipe (CQS Basic): solving cross-cutting concerns with middlewares (e.g. validation or logging)

This recipe shows how simple it is to solve cross-cutting concerns like validation or logging for your commands and queries with **Conqueror.CQS**.

If you have not read the recipe for [getting started](../getting-started#readme) yet, we recommend you take a look at it before you start with this recipe.

> You can see the completed code for this recipe directly in the [repository](.).

The application, to which we will be adding handling for cross-cutting concerns, is managing a set of named counters. In code, the API of our application is represented with the following types:

```cs
public sealed record GetCounterValueQuery(string CounterName);

public sealed record GetCounterValueQueryResponse(int CounterValue);

public sealed record IncrementCounterByCommand(string CounterName, int IncrementBy);

public sealed record IncrementCounterByCommandResponse(int NewCounterValue);
```

Feel free to take a look at the full code for [the query](Conqueror.Recipes.CQS.Basic.CrossCuttingConcerns/GetCounterValueQuery.cs) and [the command](Conqueror.Recipes.CQS.Basic.CrossCuttingConcerns/IncrementCounterByCommand.cs). The counters are stored in an [in-memory repository](Conqueror.Recipes.CQS.Basic.CrossCuttingConcerns/CountersRepository.cs).

The first cross-cutting concern we are going to address is validation. You may have noticed that the `IncrementBy` property of the `IncrementCounterByCommand` is an `int`, but we expect this value to be a _positive_ integer, and right now it could also be a negative number of zero. To deal with these cases we are going to add validation based on [data annotation attributes](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations?view=net-7.0).

In **Conqueror.CQS** we use middlewares (which implement the [chain-of-responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) pattern) to address these concerns. Each command and query handler is executed as part of a pipeline. The pipeline consists of a set of middlewares which are executed in order. Each middleware wraps the execution of the rest of the pipeline, and can also abort the pipeline execution (e.g. due to a validation failure).

> Middlewares (and pipelines) are separated for commands and queries, since the types of cross-cutting concerns that are relevant for each of them can be quite different (e.g. caching makes sense for queries, but not for commands). This means you may have to implement a middleware twice if you want to handle the same cross-cutting concern for both commands and queries. However, given that middlewares are tpyically written once and then used many times, the cost of implementing them is negligible in the big picture of your app's development. In addition, for many cross-cutting concerns **Conqueror.CQS** provides [pre-built middlewares](../../../..#conquerorcqs), so that you don't have to write them yourself (including the middlewares we are going to build in this recipe for learning purposes).

All of this is quite theoretical, so let's explore it interactively by implementing a command middleware for data annotation validation. Create a new class called [DataAnnotationValidationCommandMiddleware.cs](Conqueror.Recipes.CQS.Basic.CrossCuttingConcerns/DataAnnotationValidationCommandMiddleware.cs):

> If you want to follow along with coding while reading this recipe, you can copy the [recipe directory](.) to your local machine and then delete all `*.cs` files from the `Conqueror.Recipes.CQS.Basic.CrossCuttingConcerns/Middlewares` directory.

```cs
// TODO: add middleware
```

> The middleware is automatically added to the application services since we are using `services.AddConquerorCQSTypesFromExecutingAssembly()`. If you are not using this assembly scanning mechanism, you have to add the middleware explicitly, i.e. `services.AddTransient<DataAnnotationValidationCommandMiddleware>();`.

Now we can start using the middleware in our command handler. Each handler configures its own pipeline by implementing an interface (for command handlers it is `IConfigureCommandPipeline` and for query handlers it is `IConfigureQueryPipeline`). These interfaces contain a static method `ConfigurePipeline`, which takes a pipeline builder and adds middlewares to it. Let's do that for the [IncrementCounterByCommandHandler](Conqueror.Recipes.CQS.Basic.CrossCuttingConcerns/IncrementCounterByCommand.cs):

> The static interface method for the pipeline configuration interfaces only works out-of-the-box if you are using .NET 7 or higher since abstract static interface methods are a new feature introduced in .NET 7. If you are on .NET 6 you can use the **Conqueror.CQS** [analyzers](https://www.nuget.org/packages/Conqueror.CQS.Analyzers/), which contain an analyzer that enforces the static method to be present if a handler implements one of the pipeline configuration interfaces.

```diff
// TODO: add diff for adding pipeline configuration to handler
```

Now the [DataAnnotationValidationCommandMiddleware](Conqueror.Recipes.CQS.Basic.CrossCuttingConcerns/DataAnnotationValidationCommandMiddleware.cs) will be called every time the command handler is executed. The last step to get the validation working is to add a data annotation attribute to the command's `IncrementBy` property to declare that it needs to be a positive integer:

```cs
// TODO: command with annotiation
```

Now we can execute the app and see what happens if we provide a negative parameter value:

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

What is happening here is that the in-memory repository we are using to store our counters is simulating instability by making increment operations fail every once in a while. This is something that you typically have to deal with in your applications, especially if your app talks to other services or a database, since there are many points of failure in such a communication. Often, it is possible to deal with these kinds of transient errors by simply retrying the command, although care must be taken, that the command is [idempotent](https://en.wikipedia.org/wiki/Idempotence). In the application we are building here, this is the case, so we can build a retry middleware that takes care of the intermittent errors the repository is simulating.

Create a new class called [RetryCommandMiddleware.cs](Conqueror.Recipes.CQS.Basic.CrossCuttingConcerns/RetryCommandMiddleware.cs):

```cs
// TODO: add middleware
```

...

- show how to expose middlewares via extension methods
- mention that middlewares can only be added once by default and show how they can be added multiple times
- mention how pipelines can be made reusable (or rather link to a dedicated recipe)
- mention how middlewares can be removed
