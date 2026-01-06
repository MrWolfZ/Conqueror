# Conqueror recipe (Messaging): getting started

This recipe shows how simple it is to get started using **Conqueror** messaging.

> This recipe is designed to allow you to code along. If you prefer to just see the completed code directly, you can either view it directly [in your browser](.completed) or you can [download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/core/recipes/messaging/getting-started) and open the solution to view the code in your IDE. Note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed.

The best way to explore **Conqueror** messaging is by building an application. To keep it simple we will write a small interactive console app which manages a set of named counters. Once finished the interaction with the app will look like this:

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
dotnet new console -n Conqueror.Recipes.Messaging.GettingStarted
dotnet add Conqueror.Recipes.Messaging.GettingStarted package Conqueror

# Conqueror requires the use of dependency injection, and since this is a simple
# console app, we need to install the dependency injection package explicitly
dotnet add Conqueror.Recipes.Messaging.GettingStarted package Microsoft.Extensions.DependencyInjection

# optionally add the new project to the solution
dotnet sln Conqueror.Recipes.Messaging.GettingStarted.sln add Conqueror.Recipes.Messaging.GettingStarted
```

> If you are using a different dependency injection container (e.g. Autofac or Ninject), see the corresponding recipe for more details on how to integrate conqueror with your container of choice.

Now we can create the core application loop in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.GettingStarted/Program.cs)):

```cs
global using Conqueror;
using Conqueror.Recipes.Messaging.GettingStarted;
using Microsoft.Extensions.DependencyInjection;

// since this is a simple console app, we create the service collection ourselves
var services = new ServiceCollection();

// we'll add services here in the next step

await using var serviceProvider = services.BuildServiceProvider();

var senders = serviceProvider.GetRequiredService<IMessageSenders>();

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

Let's create an in-memory repository for managing our counters in `CountersRepository.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.GettingStarted/CountersRepository.cs)):

```cs
namespace Conqueror.Recipes.Messaging.GettingStarted;

internal class CountersRepository
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
public class CounterNotFoundException(string counterName) : Exception
{
    public string CounterName { get; } = counterName;
}
```

Let's also add this repository to our services as a singleton in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.GettingStarted/Program.cs)):

```diff
var services = new ServiceCollection();

- // we'll add services here in the next step
+ // add the in-memory repository, which contains the counters, as a singleton
+ services.AddSingleton<CountersRepository>();

await using var serviceProvider = services.BuildServiceProvider();
```

At this point you can already run the application and input commands, but they won't do anything just yet.

Now that we have finished the basic application setup we can implement the messages for managing the counters.

> You may notice that the handler classes we are about to create are all `internal`. This is a recommended practice to make it more clear that handlers are meant to be called through their interface and not directly.

We'll start with the message for getting the names of all counters for the `list` operation. In a new file `GetCounterNames.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.GettingStarted/GetCounterNames.cs)) add the following content:

```cs
namespace Conqueror.Recipes.Messaging.GettingStarted;

[Message<GetCounterNamesResponse>]
public partial record GetCounterNames;

public record GetCounterNamesResponse(IReadOnlyCollection<string> CounterNames);

internal partial class GetCounterNamesHandler(CountersRepository repository) : GetCounterNames.IHandler
{
    public async Task<GetCounterNamesResponse> Handle(GetCounterNames message, CancellationToken cancellationToken = default)
    {
        var counters = await repository.GetCounters();
        return new GetCounterNamesResponse(counters.Keys.ToList());
    }
}
```

To be able to call this message handler, we need to add it to the services in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.GettingStarted/Program.cs)):

```diff
services.AddSingleton<CountersRepository>();
+
+ // add our new message handler to the services
+ services.AddMessageHandler<GetCounterNamesHandler>();

await using var serviceProvider = services.BuildServiceProvider();
```

> Note that handlers are always registered as transient. During the development of **Conqueror** we considered the option to specify the lifetime of handlers, but we decided against this since we believe it encourages bad design to couple handlers to any particular scope. If your handler requires access to scoped data or singleton data, this should be done by injecting instances with the correct scope into the handler (e.g. using a singleton `IMemoryCache`).

Now that we have everything set up, we can implement the `list` operation of our application in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.GettingStarted/Program.cs)):

```diff
case "list" when counterName == null:
-   // ...
+   var listResponse = await senders.For(GetCounterNames.T).Handle(new());
+   Console.WriteLine(listResponse.CounterNames.Count > 0 ? $"counters:\n{string.Join('\n', listResponse.CounterNames)}" : "no counters exist");
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

Next, let's add the message for fetching the value of a counter in a new file `GetCounterValue.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.GettingStarted/GetCounterValue.cs)):

```cs
namespace Conqueror.Recipes.Messaging.GettingStarted;

[Message<GetCounterValueResponse>]
public partial record GetCounterValue(string CounterName);

public record GetCounterValueResponse(int CounterValue);

internal partial class GetCounterValueHandler(CountersRepository repository) : GetCounterValue.IHandler
{
    public async Task<GetCounterValueResponse> Handle(GetCounterValue message, CancellationToken cancellationToken = default)
    {
        return new GetCounterValueResponse(await repository.GetCounterValue(message.CounterName));
    }
}
```

We can use this handler to implement the `get` operation in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.GettingStarted/Program.cs)):

```diff
services.AddSingleton<CountersRepository>();

- // add our new message handler to the services
- services.AddMessageHandler<GetCounterNamesHandler>();
+ // add all message handlers
+ services.AddMessageHandler<GetCounterNamesHandler>()
+         .AddMessageHandler<GetCounterValueHandler>();

await using var serviceProvider = services.BuildServiceProvider();
```

```diff
case "get" when counterName != null:
-   // ...
+   var getValueResponse = await senders.For(GetCounterValue.T).Handle(new(counterName));
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

To create counters we can use the `inc` operation, which either increments an existing counter by 1 or creates a new counter with value 1 if the counter does not exist yet. Let's create a message for this in `IncrementCounter.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.GettingStarted/IncrementCounter.cs)):

```cs
namespace Conqueror.Recipes.Messaging.GettingStarted;

[Message<IncrementCounterResponse>]
public partial record IncrementCounter(string CounterName);

public record IncrementCounterResponse(int NewCounterValue);

internal partial class IncrementCounterHandler(CountersRepository repository) : IncrementCounter.IHandler
{
    public async Task<IncrementCounterResponse> Handle(IncrementCounter message, CancellationToken cancellationToken = default)
    {
        var counterValue = await GetCounterValue(message.CounterName);
        await repository.SetCounterValue(message.CounterName, counterValue + 1);
        return new IncrementCounterResponse(counterValue + 1);
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

We can now use this handler to implement the `inc` operation in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.GettingStarted/Program.cs)). At this point you may come to the conclusion that it is a bit annoying that you have to add every handler separately to the services. **Conqueror** provides two convenience extension methods `AddMessageHandlersFromExecutingAssembly()` and `AddMessageHandlersFromAssembly(Assembly assembly)` which discover and add all handlers in an assembly to the services. Let's do that for demonstration purposes:

```diff
services.AddSingleton<CountersRepository>();

- // add all message handlers
- services.AddMessageHandler<GetCounterNamesHandler>()
-         .AddMessageHandler<GetCounterValueHandler>();

+ // add all handlers automatically
+ services.AddMessageHandlersFromAssembly(typeof(Program).Assembly);

await using var serviceProvider = services.BuildServiceProvider();
```

```diff
case "inc" when counterName != null:
-   // ...
+   var incResponse = await senders.For(IncrementCounter.T).Handle(new(counterName));
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

As the last step of this recipe we'll create a message for deleting counters. This message uses the "fire-and-forget" approach, i.e. it does not have any return value. **Conqueror** allows creating messages with or without response. Let's create the new message in `DeleteCounter.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.GettingStarted/DeleteCounter.cs)):

```cs
namespace Conqueror.Recipes.Messaging.GettingStarted;

[Message]
public partial record DeleteCounter(string CounterName);

internal partial class DeleteCounterHandler(CountersRepository repository) : DeleteCounter.IHandler
{
    public async Task Handle(DeleteCounter message, CancellationToken cancellationToken = default)
    {
        await repository.DeleteCounter(message.CounterName);
    }
}
```

And finally we can use this handler to implement the `del` operation in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Messaging.GettingStarted/Program.cs)). Since we use the convenience method for adding all handlers automatically, we do not need to add our new handler separately, and we can just start using it immediately:

```diff
case "del" when counterName != null:
-   // ...
+   await senders.For(DeleteCounter.T).Handle(new(counterName));
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

This concludes our recipe for getting started with **Conqueror** messaging. In summary, you need the following:

- add the [Conqueror](https://www.nuget.org/packages/Conqueror/) NuGet package
- if you are not already writing a web application or using the [generic .NET host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host), add the [Microsoft.Extensions.DependencyInjections](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) Nuget package
- enable automatic discovery of all message handlers:

    ```cs
    services.AddMessageHandlersFromAssembly(typeof(Program).Assembly);
    ```

- start creating messages and handlers

As the next step you can explore how to test message handlers.

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.messaging.getting-started]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
