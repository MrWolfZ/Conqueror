# Conqueror recipe (CQS Basics): getting started

This recipe shows how simple it is to get started using **Conqueror.CQS**.

> You can see the completed code for this recipe directly in the [repository](.) or explore it interactively in this [.NET Fiddle](https://dotnetfiddle.net/wHWO47).

The best way to explore this library is by building a small application. To keep it simple we will write a small interactive console application which manages a set of named counters. Once finished the interaction with the app will look like this:

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

Let's start by creating a new console app and adding the dependencies.

```sh
dotnet new console -n Conqueror.Recipes.CQS.Basic.GettingStarted
cd Conqueror.Recipes.CQS.Basic.GettingStarted
dotnet add package Conqueror.CQS

# Conqueror requires the use of dependency injection, and since this is a simple
# console app, we need to install the dependency injection package explicitly
dotnet add package Microsoft.Extensions.DependencyInjection
```

> If you are using a different dependency injection container (e.g. Autofac or Ninject), see [this recipe](../../advanced/different-dependency-injection#readme) for more details on how to integrate conqueror with your container of choice.

Now we can create the core application loop in [Program.cs](Program.cs):

```cs
global using Conqueror;
using Conqueror.Recipes.CQS.Basic.GettingStarted;
using Microsoft.Extensions.DependencyInjection;

// since this is a simple console app, we create the service collection ourselves
var services = new ServiceCollection();

// we'll add services here in the next step

await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

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

Now that we have the core application loop, we can create an in-memory repository for managing our counters in [CountersRepository.cs](CountersRepository.cs):

```cs
namespace Conqueror.Recipes.CQS.Basic.GettingStarted;

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
public sealed class CounterNotFoundException : Exception
{
    public CounterNotFoundException(string counterName)
    {
        CounterName = counterName;
    }

    public string CounterName { get; }
}
```

Let's also add this repository to our services as a singleton in [Program.cs](Program.cs):

```diff
var services = new ServiceCollection();

- // we'll add services here in the next step
+ // add the in-memory repository, which contains the counters, as a singleton
+ services.AddSingleton<CountersRepository>();

await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
```

At this point you can already run the application and input commands, but they won't do anything just yet.

Now that we have finished the basic application setup we can implement the commands and queries for managing the counters.

> You may notice that the handler classes we are about to create are all `internal`. This is a recommended practice to make it more clear that handlers are meant to be called through their interface and not directly.

We'll start with the query for getting the names of all counters for the `list` operation. In a new file [GetCounterNamesQuery.cs](GetCounterNamesQuery.cs) add the following content:

```cs
namespace Conqueror.Recipes.CQS.Basic.GettingStarted;

public sealed record GetCounterNamesQuery;

public sealed record GetCounterNamesQueryResponse(IReadOnlyCollection<string> CounterNames);

internal sealed class GetCounterNamesQueryHandler : IQueryHandler<GetCounterNamesQuery, GetCounterNamesQueryResponse>
{
    private readonly CountersRepository repository;

    public GetCounterNamesQueryHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public async Task<GetCounterNamesQueryResponse> ExecuteQuery(GetCounterNamesQuery query, CancellationToken cancellationToken = default)
    {
        var counters = await repository.GetCounters();
        return new(counters.Keys.ToList());
    }
}
```

To be able to call this query handler, we need to add a few things to the services in [Program.cs](Program.cs):

```diff
services.AddSingleton<CountersRepository>();

+ // add the conqueror CQS services
+ services.AddConquerorCQS();
+
+ // register our new query handler (note that you do not need to specify any interface)
+ services.AddTransient<GetCounterNamesQueryHandler>();
+
+ // this method MUST be called exactly once after all conqueror services, handlers etc. are added
+ services.FinalizeConquerorRegistrations();

await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
```

The method `FinalizeConquerorRegistrations` contains logic that finds all registered command and query handlers and adds extra registrations that for example allow resolving a handler from its query handler interface instead of the concrete handler type (e.g. our new handler can be injected as `IQueryHandler<GetCounterNamesQuery, GetCounterNamesQueryResponse>`). The method is also a bit unusual, because it must be called exactly once after everything that is relevant for **Conqueror** has been added to the services. Therefore it is recommended to call it just before building the service provider.

> For ASP.NET Core web applications, you typically do not build the service provider yourself. See [this recipe](../../advanced/exposing-via-http#readme) for more details on how to use **Conqueror.CQS** in such web applications.

Now that we have everything set up, we can finally implement the `list` operation of our application in [Program.cs](Program.cs):

```diff
case "list" when counterName == null:
-   // ...
+   var listHandler = serviceProvider.GetRequiredService<IQueryHandler<GetCounterNamesQuery, GetCounterNamesQueryResponse>>();
+   var listResponse = await listHandler.ExecuteQuery(new());
+   Console.WriteLine(listResponse.CounterNames.Any() ? $"counters:\n{string.Join("\n", listResponse.CounterNames)}" : "no counters exist");
    break;
```

You can now run the application and execute the `list` operation (although there aren't any counter names to show just yet):

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

Next, let's add the query for fetching the value of a counter. While doing this, we'll also use a cool feature from **Conqueror.CQS**. As you saw when we implemented the `list` operation, we needed to resolve the query handler using the rather unwieldy and difficult to type interface `IQueryHandler<GetCounterNamesQuery, GetCounterNamesQueryResponse>`. To make it easier to use your command and query handlers, **Conqueror.CQS** allows you to define your own interface for the handler, which you can then use to resolve or inject your handler. Let's see how that looks like in the query for fetching a counter's value in [GetCounterValueQuery.cs](GetCounterValueQuery.cs):

```cs
namespace Conqueror.Recipes.CQS.Basic.GettingStarted;

public sealed record GetCounterValueQuery(string CounterName);

public sealed record GetCounterValueQueryResponse(int CounterValue);

// this interface can be resolved or injected to call a handler; note that the interface
// must not have any extra methods, it just inherits from the generic handler interface
public interface IGetCounterValueQueryHandler : IQueryHandler<GetCounterValueQuery, GetCounterValueQueryResponse>
{
}

internal sealed class GetCounterValueQueryHandler : IGetCounterValueQueryHandler
{
    private readonly CountersRepository repository;

    public GetCounterValueQueryHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public async Task<GetCounterValueQueryResponse> ExecuteQuery(GetCounterValueQuery query, CancellationToken cancellationToken = default)
    {
        return new(await repository.GetCounterValue(query.CounterName));
    }
}
```

> A custom handler interface must not have any additional methods, it just inherits from the generic handler interface. While this limitation does not seem to make much sense on the surface, it is required for advanced use cases like [calling HTTP commands and queries from another application](../../advanced/calling-http#readme). It also encourages you to adhere to the [single-responsibility principle](https://en.wikipedia.org/wiki/Single-responsibility_principle) by ensuring that each handler only has a single public method.

We can now use this handler to implement the `get` operation in [Program.cs](Program.cs):

```diff
services.AddConquerorCQS();

- // register our new query handler (note that you do not need to specify any interface)
- services.AddTransient<GetCounterNamesQueryHandler>();
+ // register all query handlers
+ services.AddTransient<GetCounterNamesQueryHandler>()
+         .AddTransient<GetCounterValueQueryHandler>();

// this method MUST be called exactly once after all conqueror services, handlers etc. are added
services.FinalizeConquerorRegistrations();
```

```diff
case "get" when counterName != null:
-   // ...
+   var getValueHandler = serviceProvider.GetRequiredService<IGetCounterValueQueryHandler>();
+   var getValueResponse = await getValueHandler.ExecuteQuery(new(counterName));
+   Console.WriteLine($"counter '{counterName}' value: {getValueResponse.CounterValue}");
    break;
```

You can now run the application and execute the `get` operation (although there aren't any counters to get a value for just yet, which we'll address in the next step):

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

To create counters we can use the `inc` operation, which either increments an existing counter by 1 or creates a new counter with value 1 if the counter does not exist yet. Let's create a command for this in [IncrementCounterCommand.cs](IncrementCounterCommand.cs) (using a dedicated handler interface as we did above):

```cs
namespace Conqueror.Recipes.CQS.Basic.GettingStarted;

public sealed record IncrementCounterCommand(string CounterName);

public sealed record IncrementCounterCommandResponse(int NewCounterValue);

public interface IIncrementCounterCommandHandler : ICommandHandler<IncrementCounterCommand, IncrementCounterCommandResponse>
{
}

internal sealed class IncrementCounterCommandHandler : IIncrementCounterCommandHandler
{
    private readonly CountersRepository repository;

    public IncrementCounterCommandHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public async Task<IncrementCounterCommandResponse> ExecuteCommand(IncrementCounterCommand command, CancellationToken cancellationToken = default)
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

We can now use this handler to implement the `inc` operation in [Program.cs](Program.cs). At this point you may come to the conclusion that it is a bit annoying that you have to add every handler separately to the services. **Conqueror.CQS** provides two convenience extension methods `AddConquerorCQSTypesFromExecutingAssembly()` and `AddConquerorCQSTypesFromAssembly(Assembly assembly)` which discover and add all handlers in an assembly to the services as transient. The methods will not overwrite any handlers that are already added, meaning you can add non-transient handlers yourself (e.g. `services.AddSingleton<GetCounterNamesQueryHandler>()`) and then register all remaining ones automatically with the convenience methods. Let's do that for demonstration purposes:

```diff
services.AddConquerorCQS();

- // register all query handlers
- services.AddTransient<GetCounterNamesQueryHandler>()
-         .AddTransient<GetCounterValueQueryHandler>();
+ // register some handlers manually for demonstration purposes (i.e. they could just be transient)
+ services.AddSingleton<GetCounterNamesQueryHandler>()
+         .AddScoped<IncrementCounterCommandHandler>();
+
+ // add all remaining handlers automatically as transient
+ services.AddConquerorCQSTypesFromExecutingAssembly();

// this method MUST be called exactly once after all conqueror services, handlers etc. are added
services.FinalizeConquerorRegistrations();
```

```diff
case "inc" when counterName != null:
-   // ...
+   var incrementHandler = serviceProvider.GetRequiredService<IIncrementCounterCommandHandler>();
+   var incResponse = await incrementHandler.ExecuteCommand(new(counterName));
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

As the last step of this recipe we'll create a command for deleting counters. This command uses the "fire-and-forget" approach, i.e. it does not have any return value. **Conqueror.CQS** allows creating commands without or without response. Let's create the new command in [DeleteCounterCommand.cs](DeleteCounterCommand.cs):

```cs
namespace Conqueror.Recipes.CQS.Basic.GettingStarted;

public sealed record DeleteCounterCommand(string CounterName);

// a command handler does not need to have a response
public interface IDeleteCounterCommandHandler : ICommandHandler<DeleteCounterCommand>
{
}

internal sealed class DeleteCounterCommandHandler : IDeleteCounterCommandHandler
{
    private readonly CountersRepository repository;

    public DeleteCounterCommandHandler(CountersRepository repository)
    {
        this.repository = repository;
    }

    public async Task ExecuteCommand(DeleteCounterCommand command, CancellationToken cancellationToken = default)
    {
        await repository.DeleteCounter(command.CounterName);
    }
}
```

And finally we can use this handler to implement the `del` operation in [Program.cs](Program.cs). Since we use the convenience method for adding all handlers automatically, we do not need to add our new handler separately, and we can just start using it immediately:

```diff
case "del" when counterName != null:
-   // ...
+   var deleteHandler = serviceProvider.GetRequiredService<IDeleteCounterCommandHandler>();
+   await deleteHandler.ExecuteCommand(new(counterName));
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

This concludes our recipe for getting started with **Conqueror.CQS**. We recommend that you take a look at the recipe for [testing the command and query handlers](../testing-handlers#readme) we created in this recipe.

Or head over to our [other recipes](../../../../README.md#recipes) for more guidance on different topics.
