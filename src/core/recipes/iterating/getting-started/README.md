# Conqueror recipe (Iterating): getting started

This recipe shows how simple it is to get started using **Conqueror** iterating.

> Iterating is currently an **experimental** feature of **Conqueror**: its API is not yet stable and may change in future versions, so it is not yet recommended for production use, but it is a great fit for proofs-of-concept and toy apps.

> This recipe is designed to allow you to code along. If you prefer to just see the completed code directly, you can either view it directly [in your browser](.completed) or you can [download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/core/recipes/iterating/getting-started) and open the solution to view the code in your IDE. Note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed.

Iterating is **Conqueror**'s take on the request/stream pattern. It is the streaming analog of messaging's request/response: instead of a handler returning a single response for a request, an *iterator* handler returns a *stream* of items for a request. The stream is consumed with a pull-based approach (using `IAsyncEnumerable<T>`), which means the consumer controls the pace - each item is produced only once the consumer asks for it. This is a great fit for use cases like paging through a large data set or reading events one at a time.

The best way to explore **Conqueror** iterating is by building an application. To keep it simple we will write a small interactive console app which lists the cities of a country. The cities are streamed one at a time from a handler that simulates fetching each item from a slow data source. Once finished the interaction with the app will look like this:

```txt
> dotnet run
input commands in format 'list <country>' (e.g. 'list at')
input q to quit
list at
received: Vienna
received: Graz
received: Linz
stream complete
list de
received: Berlin
received: Munich
received: Hamburg
stream complete
q
shutting down...
```

Let's start by creating a new console app and adding the dependencies (if you prefer you can of course create the project via your IDE).

```sh
dotnet new console -n Conqueror.Recipes.Iterating.GettingStarted
dotnet add Conqueror.Recipes.Iterating.GettingStarted package Conqueror

# Conqueror requires the use of dependency injection, and since this is a simple
# console app, we need to install the dependency injection package explicitly
dotnet add Conqueror.Recipes.Iterating.GettingStarted package Microsoft.Extensions.DependencyInjection

# optionally add the new project to the solution
dotnet sln Conqueror.Recipes.Iterating.GettingStarted.sln add Conqueror.Recipes.Iterating.GettingStarted
```

An iterator is defined as a `record` marked with the `[Iterator<TItem>]` attribute, where `TItem` is the type of the items that will be streamed. The **Conqueror** source generator inspects this attribute and generates everything that is needed to handle and consume the iterator (which is why the type must be `partial`). Let's create the iterator in a new file `GetCities.cs` ([view completed file](.completed/Conqueror.Recipes.Iterating.GettingStarted/GetCities.cs)):

```cs
namespace Conqueror.Recipes.Iterating.GettingStarted;

[Iterator<string>]
public partial record GetCities(string Country);
```

An iterator handler is a `partial` class that implements the generated `GetCities.IHandler` interface. Its `Handle` method returns an `IAsyncEnumerable<TItem>`, so it is written as an async iterator method that `yield return`s each item. The `[EnumeratorCancellation]` attribute forwards the cancellation token into the generated enumerator. Create a new file `GetCitiesHandler.cs` ([view completed file](.completed/Conqueror.Recipes.Iterating.GettingStarted/GetCitiesHandler.cs)):

```cs
namespace Conqueror.Recipes.Iterating.GettingStarted;

using System.Runtime.CompilerServices;

internal partial class GetCitiesHandler : GetCities.IHandler
{
    private static readonly Dictionary<string, string[]> CitiesByCountry = new()
    {
        ["at"] = ["Vienna", "Graz", "Linz"],
        ["de"] = ["Berlin", "Munich", "Hamburg"],
    };

    public async IAsyncEnumerable<string> Handle(
        GetCities iterator,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cities = CitiesByCountry.GetValueOrDefault(iterator.Country) ?? [];

        foreach (var city in cities)
        {
            // simulate fetching each item from a slow data source; the consumer only
            // pays for the items it actually pulls
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            yield return city;
        }
    }
}
```

> You may notice that the handler class is `internal`. This is a recommended practice to make it clear that handlers are meant to be triggered by consuming an iterator, not by calling them directly.

Now we can create the application loop in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Iterating.GettingStarted/Program.cs)). `IIterators` is a factory that creates a client for any iterator type; the `GetCities.T` property is generated by the source generator and is used for type inference. `iterators.For(GetCities.T)` returns a client for the iterator, and its `Handle` method returns the `IAsyncEnumerable<string>` that we consume with `await foreach`:

```cs
global using Conqueror;
using Conqueror.Recipes.Iterating.GettingStarted;
using Microsoft.Extensions.DependencyInjection;

// since this is a simple console app, we create the service collection ourselves
var services = new ServiceCollection();

// add all iterator handlers automatically
services.AddIteratorHandlersFromAssembly(typeof(Program).Assembly);

await using var serviceProvider = services.BuildServiceProvider();

// IIterators is a factory that creates a client for any iterator type
var iterators = serviceProvider.GetRequiredService<IIterators>();

Console.WriteLine("input commands in format 'list <country>' (e.g. 'list at')");
Console.WriteLine("input q to quit");

while (true)
{
    var line = Console.ReadLine() ?? "";

    if (line == "q")
    {
        Console.WriteLine("shutting down...");
        return;
    }

    var input = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);

    var op = input.FirstOrDefault();
    var country = input.Skip(1).FirstOrDefault();

    switch (op)
    {
        case "list" when country != null:
            // consume the stream at our own pace; each item arrives as the handler produces it
            await foreach (var city in iterators.For(GetCities.T).Handle(new(country)))
            {
                Console.WriteLine($"received: {city}");
            }

            Console.WriteLine("stream complete");
            break;

        default:
            Console.WriteLine($"invalid input '{line}'");
            break;
    }
}
```

We registered the handler with the convenience extension method `AddIteratorHandlersFromAssembly(Assembly assembly)`, which discovers and adds all iterator handlers in an assembly to the services. If you prefer to register handlers individually, you can use `services.AddIteratorHandler<GetCitiesHandler>()` instead.

Now run the application and list the cities of a country to see the handler stream them one at a time:

```txt
> dotnet run
input commands in format 'list <country>' (e.g. 'list at')
input q to quit
list at
received: Vienna
received: Graz
received: Linz
stream complete
q
shutting down...
```

Because the stream is pull-based, the `await foreach` loop pulls each city only when it is ready for the next one, and the handler produces the next item only when it is pulled. For an unknown country the handler simply yields no items at all, so the stream completes immediately:

```txt
> dotnet run
input commands in format 'list <country>' (e.g. 'list at')
input q to quit
list fr
stream complete
q
shutting down...
```

This is the core value of iterating: the consumer stays in control of the pace, and items are produced lazily as they are pulled instead of all at once. This makes it a natural fit for paging through large data sets or reading events one at a time without loading everything into memory.

This concludes our recipe for getting started with **Conqueror** iterating. In summary, you need the following:

- add the [Conqueror](https://www.nuget.org/packages/Conqueror/) NuGet package
- if you are not already writing a web application or using the [generic .NET host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host), add the [Microsoft.Extensions.DependencyInjections](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) Nuget package
- enable automatic discovery of all iterator handlers:

    ```cs
    services.AddIteratorHandlersFromAssembly(typeof(Program).Assembly);
    ```

- create an iterator with `[Iterator<TItem>]`, write a handler that returns `IAsyncEnumerable<TItem>`, and consume it via `iterators.For(YourIterator.T).Handle(...)` with `await foreach`

We handled the iterator in-process here. Just like messaging, iterating supports pipelines for solving cross-cutting concerns (e.g. logging) at the call site; head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.iterating.getting-started]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
