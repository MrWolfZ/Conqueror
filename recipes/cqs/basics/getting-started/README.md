# Conqueror recipe (CQS Basics): getting started

This recipe shows how simple it is to get started using **Conqueror.CQS**.

> This recipe is designed to allow you to code along. If you prefer to just see the completed code directly, you can either view it directly [in your browser](.completed) or you can [download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/recipes/cqs/basics/getting-started) and open the solution to view the code in your IDE. Note that you need to have [.NET 6 or later](https://dotnet.microsoft.com/en-us/download) installed.

The best way to explore **Conqueror.CQS** is by building an application. To keep it simple we will write a small interactive console app which manages a set of named counters. Once finished the interaction with the app will look like this:

```txt
> dotnet run
input commands in format '<op> [counterName]' (e.g. 'inc test' or 'list')
available operations: list, get, inc, del
input q to quit
get test
counter 'test' does not exist
inc test
incremented counter 'test'; new value: 1
inc test
incremented counter 'test'; new value: 2
get test
counter 'test' value: 2
list
counters:
test
del test
deleted counter 'test'
get test
counter 'test' does not exist
q
shutting down...
```

Let's start by creating a new console app and adding the dependencies (if you prefer you can of course create the project via your IDE).

```sh
dotnet new console -n Conqueror.Recipes.CQS.Basics.GettingStarted
dotnet add Conqueror.Recipes.CQS.Basics.GettingStarted package Conqueror.CQS

# Conqueror requires the use of dependency injection, and since this is a simple
# console app, we need to install the dependency injection package explicitly
dotnet add Conqueror.Recipes.CQS.Basics.GettingStarted package Microsoft.Extensions.DependencyInjection

# optionally add the new project to the solution
dotnet sln Conqueror.Recipes.CQS.Basics.GettingStarted.sln add Conqueror.Recipes.CQS.Basics.GettingStarted
```

> If you are using a different dependency injection container (e.g. Autofac or Ninject), see [this recipe](../../advanced/different-dependency-injection#readme) for more details on how to integrate conqueror with your container of choice.

Now we can create the core application loop in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.GettingStarted/Program.cs)):

```cs
global using Conqueror;
using Conqueror.Recipes.CQS.Basics.GettingStarted;
using Microsoft.Extensions.DependencyInjection;

// since this is a simple console app, we create the service collection ourselves
var services = new ServiceCollection();

// we'll add services here in the next step

await using var serviceProvider = services.BuildServiceProvider();

Console.WriteLine("input commands in format '<op> [counterName]' (e.g. 'inc test' or 'list')");
Console.WriteLine("available operations: list, get, inc, del");
Console.WriteLine("input q to quit");

while (true)
{
    var line = Console.ReadLine() ?? string.Empty;

    if (line == "q")
    {
        Console.WriteLine("shutting down...");
        return;
    }
    
    var input = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);

    var op = input.FirstOrDefault();
    var counterName = input.Skip(1).FirstOrDefault();

    try
    {
        switch (op)
        {
            case "list" when counterName == null:
                // ...
                break;

            case "get" when counterName != null:
                // ...
                break;

            case "inc" when counterName != null:
                // ...
                break;

            case "del" when counterName != null:
                // ...
                break;

            default:
                Console.WriteLine($"invalid input '{line}'");
                break;
        }
    }
    catch (CounterNotFoundException ex)
    {
        Console.WriteLine($"counter '{ex.CounterName}' does not exist");
    }
}
```

Note that this program does not compile yet since it already references a few things we'll only create in the next step.

To store our counters, we are going to use the [repository pattern](https://martinfowler.com/eaaCatalog/repository.html). For simplicity, we store the counters in memory, but in a real application the repository would talk to some kind of database. There are other ways to interact with a data store, but repositories are a common way to do this. In very simple apps you may even consider talking directly to the database from within your handlers, but beware the challenges this poses for testing.

Let's create an in-memory repository for managing our counters in `CountersRepository.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.GettingStarted/CountersRepository.cs)):

```cs
namespace Conqueror.Recipes.CQS.Basics.GettingStarted;

internal sealed class CountersRepository
{
    // we ignore thread-safety concerns for simplicity here, so we just use a simple dictionary
    private readonly Dictionary<string, int> counters = new();

    // we return tasks from these methods to more closely resemble an actual repository that talks to some database
    public async Task<IReadOnlyDictionary<string, int>> GetCounters()
    {
        await Task.CompletedTask;
        return counters;
    }
    
    public async Task<int> GetCounterValue(string counterName)
    {
        await Task.CompletedTask;
        return counters.TryGetValue(counterName, out var v) ? v : throw new CounterNotFoundException(counterName);
    }

    public async Task SetCounterValue(string counterName, int newValue)
    {
        await Task.CompletedTask;
        counters[counterName] = newValue;
    }

    public async Task DeleteCounter(string counterName)
    {
        await Task.CompletedTask;

        if (!counters.Remove(counterName))
        {
            throw new CounterNotFoundException(counterName);
        }
    }
}

// we use an exception to handle the case of a non-existing counter; there are other approaches to this
// as well (e.g. returning `null` or a boolean) but an exception allows for unified error handling
public sealed class CounterNotFoundException(string counterName) : Exception
{
    public string CounterName { get; } = counterName;
}
```

Let's also add this repository to our services as a singleton in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.GettingStarted/Program.cs)):

```diff
var services = new ServiceCollection();

- // we'll add services here in the next step
+ // add the in-memory repository, which contains the counters, as a singleton
+ services.AddSingleton<CountersRepository>();

await using var serviceProvider = services.BuildServiceProvider();
```

At this point you can already run the application and input commands, but they won't do anything just yet.

Now that we have finished the basic application setup we can implement the commands and queries for managing the counters.

> You may notice that the handler classes we are about to create are all `internal`. This is a recommended practice to make it more clear that handlers are meant to be called through their interface and not directly.

We'll start with the query for getting the names of all counters for the `list` operation. In a new file `GetCounterNamesQuery.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.GettingStarted/GetCounterNamesQuery.cs)) add the following content:

```cs
namespace Conqueror.Recipes.CQS.Basics.GettingStarted;

public sealed record GetCounterNamesQuery;

public sealed record GetCounterNamesQueryResponse(IReadOnlyCollection<string> CounterNames);

internal sealed class GetCounterNamesQueryHandler(CountersRepository repository) : IQueryHandler<GetCounterNamesQuery, GetCounterNamesQueryResponse>
{
    public async Task<GetCounterNamesQueryResponse> Handle(GetCounterNamesQuery query, CancellationToken cancellationToken = default)
    {
        var counters = await repository.GetCounters();
        return new(counters.Keys.ToList());
    }
}
```

To be able to call this query handler, we need to add it to the services in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.GettingStarted/Program.cs)):

```diff
services.AddSingleton<CountersRepository>();
+
+ // add our new query handler to the services
+ services.AddConquerorQueryHandler<GetCounterNamesQueryHandler>();

await using var serviceProvider = services.BuildServiceProvider();
```

> Note that handlers are always registered as transient. During the development of **Conqueror** we considered the option to specify the lifetime of handlers, but we decided against this since we believe it encourages bad design to couple handlers to any particular scope. If your handler requires access to scoped data or singleton data, this should be done by injecting instances with the correct scope into the handler (e.g. using a singleton `IMemoryCache`).

Now that we have everything set up, we can implement the `list` operation of our application in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.GettingStarted/Program.cs)):

```diff
case "list" when counterName == null:
-   // ...
+   var listHandler = serviceProvider.GetRequiredService<IQueryHandler<GetCounterNamesQuery, GetCounterNamesQueryResponse>>();
+   var listResponse = await listHandler.Handle(new());
+   Console.WriteLine(listResponse.CounterNames.Any() ? $"counters:\n{string.Join("\n", listResponse.CounterNames)}" : "no counters exist");
    break;
```

With this change in place you can run the application and execute the `list` operation (although there aren't any counter names to show just yet):

```txt
> dotnet run
input commands in format '<op> [counterName]' (e.g. 'inc test' or 'list')
available operations: list, get, inc, del
input q to quit
list
no counters exist
q
shutting down...
```

Next, let's add the query for fetching the value of a counter. While doing this, we'll also use a cool feature from **Conqueror.CQS**. As you saw when we implemented the `list` operation, we needed to resolve the query handler using the rather unwieldy and difficult to type interface `IQueryHandler<GetCounterNamesQuery, GetCounterNamesQueryResponse>`. To make it easier to use your command and query handlers, **Conqueror.CQS** allows you to define your own interface for the handler, which you can then use to resolve or inject your handler. Let's see how that looks like in the query for fetching a counter's value in `GetCounterValueQuery.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.GettingStarted/GetCounterValueQuery.cs)):

```cs
namespace Conqueror.Recipes.CQS.Basics.GettingStarted;

public sealed record GetCounterValueQuery(string CounterName);

public sealed record GetCounterValueQueryResponse(int CounterValue);

// this interface can be resolved or injected to call a handler; note that the interface
// must not have any extra methods, it just inherits from the generic handler interface
public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>;

internal sealed class GetCounterValueQueryHandler(CountersRepository repository) : IGetCounterValueQueryHandler
{
    public async Task<GetCounterValueQueryResponse> Handle(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        return new(await repository.GetCounterValue(query.CounterName));
    }
}
```

> A custom handler interface must not have any additional methods, it just inherits from the generic handler interface. While this limitation does not seem to make much sense on the surface, it is required for advanced use cases like [calling HTTP commands and queries from another application](../../advanced/calling-http#readme). It also encourages you to adhere to the [single-responsibility principle](https://en.wikipedia.org/wiki/Single-responsibility_principle) by ensuring that each handler only has a single public method.

We can use this handler to implement the `get` operation in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.GettingStarted/Program.cs)):

```diff
services.AddSingleton<CountersRepository>();

- // add our new query handler to the services
- services.AddConquerorQueryHandler<GetCounterNamesQueryHandler>();
+ // add all query handlers
+ services.AddConquerorQueryHandler<GetCounterNamesQueryHandler>()
+         .AddConquerorQueryHandler<GetCounterValueQueryHandler>();

await using var serviceProvider = services.BuildServiceProvider();
```

```diff
case "get" when counterName != null:
-   // ...
+   var getValueHandler = serviceProvider.GetRequiredService<IGetCounterValueQueryHandler>();
+   var getValueResponse = await getValueHandler.Handle(new(counterName));
+   Console.WriteLine($"counter '{counterName}' value: {getValueResponse.CounterValue}");
    break;
```

Run the application and execute the `get` operation (although there aren't any counters to get a value for just yet, which we'll address in the next step):

```txt
> dotnet run
input commands in format '<op> [counterName]' (e.g. 'inc test' or 'list')
available operations: list, get, inc, del
input q to quit
get test
counter 'test' does not exist
q
shutting down...
```

To create counters we can use the `inc` operation, which either increments an existing counter by 1 or creates a new counter with value 1 if the counter does not exist yet. Let's create a command for this in `IncrementCounterCommand.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.GettingStarted/IncrementCounterCommand.cs)), using a dedicated handler interface as we did above:

```cs
namespace Conqueror.Recipes.CQS.Basics.GettingStarted;

public sealed record IncrementCounterCommand(string CounterName);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

public interface IIncrementCounterCommandHandler : ICommandHandler<IncrementCounterCommand, IncrementCounterCommandResponse>;

internal sealed class IncrementCounterCommandHandler(CountersRepository repository) : IIncrementCounterCommandHandler
{
    public async Task<IncrementCounterCommandResponse> Handle(IncrementCounterCommand command, CancellationToken cancellationToken = default)
    {
        var counterValue = await GetCounterValue(command.CounterName);
        await repository.SetCounterValue(command.CounterName, counterValue + 1);
        return new(counterValue + 1);
    }

    private async Task<int> GetCounterValue(string counterName)
    {
        try
        {
            return await repository.GetCounterValue(counterName);
        }
        catch (CounterNotFoundException)
        {
            return 0;
        }
    }
}

```

We can now use this handler to implement the `inc` operation in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.GettingStarted/Program.cs)). At this point you may come to the conclusion that it is a bit annoying that you have to add every handler separately to the services. **Conqueror.CQS** provides two convenience extension methods `AddConquerorCQSTypesFromExecutingAssembly()` and `AddConquerorCQSTypesFromAssembly(Assembly assembly)` which discover and add all handlers in an assembly to the services. Let's do that for demonstration purposes:

```diff
services.AddSingleton<CountersRepository>();

- // add all query handlers
- services.AddConquerorQueryHandler<GetCounterNamesQueryHandler>()
-         .AddConquerorQueryHandler<GetCounterValueQueryHandler>();

+ // add all handlers automatically
+ services.AddConquerorCQSTypesFromExecutingAssembly();

await using var serviceProvider = services.BuildServiceProvider();
```

```diff
case "inc" when counterName != null:
-   // ...
+   var incrementHandler = serviceProvider.GetRequiredService<IIncrementCounterCommandHandler>();
+   var incResponse = await incrementHandler.Handle(new(counterName));
+   Console.WriteLine($"incremented counter '{counterName}'; new value: {incResponse.NewCounterValue}");
    break;
```

You can now run the application and execute the `inc` operation to create and increment counters:

```txt
> dotnet run
input commands in format '<op> [counterName]' (e.g. 'inc test' or 'list')
available operations: list, get, inc, del
input q to quit
inc test
incremented counter 'test'; new value: 1
inc test
incremented counter 'test'; new value: 2
get test
counter 'test' value: 2
list
counters:
test
q
shutting down...
```

As the last step of this recipe we'll create a command for deleting counters. This command uses the "fire-and-forget" approach, i.e. it does not have any return value. **Conqueror.CQS** allows creating commands without or without response. Let's create the new command in `DeleteCounterCommand.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.GettingStarted/DeleteCounterCommand.cs)):

```cs
namespace Conqueror.Recipes.CQS.Basics.GettingStarted;

public sealed record DeleteCounterCommand(string CounterName);

// a command handler does not need to have a response
public interface IDeleteCounterCommandHandler : ICommandHandler<DeleteCounterCommand>;

internal sealed class DeleteCounterCommandHandler(CountersRepository repository) : IDeleteCounterCommandHandler
{
    public async Task Handle(DeleteCounterCommand command, CancellationToken cancellationToken = default)
    {
        await repository.DeleteCounter(command.CounterName);
    }
}
```

And finally we can use this handler to implement the `del` operation in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.CQS.Basics.GettingStarted/Program.cs)). Since we use the convenience method for adding all handlers automatically, we do not need to add our new handler separately, and we can just start using it immediately:

```diff
case "del" when counterName != null:
-   // ...
+   var deleteHandler = serviceProvider.GetRequiredService<IDeleteCounterCommandHandler>();
+   await deleteHandler.Handle(new(counterName));
+   Console.WriteLine($"deleted counter '{counterName}'");
    break;
```

You can now run the application and execute the `del` operation to delete counters:

```txt
> dotnet run
input commands in format '<op> [counterName]' (e.g. 'inc test' or 'list')
available operations: list, get, inc, del
input q to quit
inc test
incremented counter 'test'; new value: 1
inc test
incremented counter 'test'; new value: 2
get test
counter 'test' value: 2
del test
deleted counter 'test'
get test
counter 'test' does not exist
q
shutting down...
```

This concludes our recipe for getting started with **Conqueror.CQS**. In summary, you need the following:

- add the [Conqueror.CQS](https://www.nuget.org/packages/Conqueror.CQS/) NuGet package
- if you are not already writing a web application or using the [generic .NET host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host), add the [Microsoft.Extensions.DependencyInjections](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) Nuget package
- enable automatic discovery of all **Conqueror.CQS** types:

    ```cs
    services.AddConquerorCQSTypesFromExecutingAssembly();
    ```

- start creating commands, queries, and handlers

As the next step you can explore how to [test command and query handlers](../testing-handlers#readme).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.cqs.basics.getting-started]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
