# Conqueror recipe (Signalling): getting started

This recipe shows how simple it is to get started using **Conqueror** signalling.

> This recipe is designed to allow you to code along. If you prefer to just see the completed code directly, you can either view it directly [in your browser](.completed) or you can [download this recipe's folder](https://download-directory.github.io?url=https://github.com/MrWolfZ/Conqueror/tree/main/src/core/recipes/signalling/getting-started) and open the solution to view the code in your IDE. Note that you need to have [.NET 8 or later](https://dotnet.microsoft.com/en-us/download) installed.

Signalling is **Conqueror**'s take on the [publish-subscribe](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) pattern. A *signal* is a message that announces that something happened, and any number of *handlers* can react to it independently. The key value is decoupling: the code that publishes a signal does not know (or care) which handlers receive it, and you can add new handlers without touching the publisher.

The best way to explore **Conqueror** signalling is by building an application. To keep it simple we will write a small interactive console app which increments named counters. Every time a counter is incremented we publish a signal, and two independent handlers react to it: one prints a notification, the other keeps running statistics. Once finished the interaction with the app will look like this:

```txt
> dotnet run
input commands in format 'inc <counterName>' (e.g. 'inc test')
input q to quit
inc test
notification: counter 'test' was incremented to 1
statistics: 1 increment(s) recorded so far
inc test
notification: counter 'test' was incremented to 2
statistics: 2 increment(s) recorded so far
inc other
notification: counter 'other' was incremented to 1
statistics: 3 increment(s) recorded so far
q
shutting down...
```

Let's start by creating a new console app and adding the dependencies (if you prefer you can of course create the project via your IDE).

```sh
dotnet new console -n Conqueror.Recipes.Signalling.GettingStarted
dotnet add Conqueror.Recipes.Signalling.GettingStarted package Conqueror

# Conqueror requires the use of dependency injection, and since this is a simple
# console app, we need to install the dependency injection package explicitly
dotnet add Conqueror.Recipes.Signalling.GettingStarted package Microsoft.Extensions.DependencyInjection

# optionally add the new project to the solution
dotnet sln Conqueror.Recipes.Signalling.GettingStarted.sln add Conqueror.Recipes.Signalling.GettingStarted
```

Now we can create the core application loop in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Signalling.GettingStarted/Program.cs)):

```cs
global using Conqueror;
using Conqueror.Recipes.Signalling.GettingStarted;
using Microsoft.Extensions.DependencyInjection;

// since this is a simple console app, we create the service collection ourselves
var services = new ServiceCollection();

// we'll add services and handlers here in the next steps

await using var serviceProvider = services.BuildServiceProvider();

// ISignalPublishers is a factory that creates a publisher for any signal type
var publishers = serviceProvider.GetRequiredService<ISignalPublishers>();

// the publisher owns the counter values; the handlers only react to the published signal
var counters = new Dictionary<string, int>();

Console.WriteLine("input commands in format 'inc <counterName>' (e.g. 'inc test')");
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
    var counterName = input.Skip(1).FirstOrDefault();

    switch (op)
    {
        case "inc" when counterName != null:
            var newValue = counters.GetValueOrDefault(counterName) + 1;
            counters[counterName] = newValue;

            // ...
            break;

        default:
            Console.WriteLine($"invalid input '{line}'");
            break;
    }
}
```

Note that this program does not compile yet since it imports a namespace that will only exist once we create the signal type in the next step.

A signal is defined as a `record` marked with the `[Signal]` attribute. The **Conqueror** source generator inspects this attribute and generates everything that is needed to publish and handle the signal (which is why the type must be `partial`). Let's create the signal in a new file `CounterIncremented.cs` ([view completed file](.completed/Conqueror.Recipes.Signalling.GettingStarted/CounterIncremented.cs)):

```cs
namespace Conqueror.Recipes.Signalling.GettingStarted;

[Signal]
public partial record CounterIncremented(string CounterName, int NewValue);
```

Now we can publish the signal from `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Signalling.GettingStarted/Program.cs)). The `CounterIncremented.T` property is generated by the source generator and is used for type inference; `publishers.For(...)` returns a publisher for the signal, and its `Handle` method publishes the signal to all handlers:

```diff
case "inc" when counterName != null:
    var newValue = counters.GetValueOrDefault(counterName) + 1;
    counters[counterName] = newValue;

-   // ...
+   // publish the signal; the publisher does not know or care which handlers receive it
+   await publishers.For(CounterIncremented.T).Handle(new(counterName, newValue));
    break;
```

At this point you can already run the application and increment counters, but nothing observable happens yet since there are no handlers subscribed to the signal (publishing a signal that has no handlers is simply a no-op).

Let's fix that by creating our first handler. A signal handler is a `partial` class that implements the generated `CounterIncremented.IHandler` interface. Create a new file `NotificationHandler.cs` ([view completed file](.completed/Conqueror.Recipes.Signalling.GettingStarted/NotificationHandler.cs)):

```cs
namespace Conqueror.Recipes.Signalling.GettingStarted;

internal partial class NotificationHandler : CounterIncremented.IHandler
{
    public async Task Handle(CounterIncremented signal, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        Console.WriteLine($"notification: counter '{signal.CounterName}' was incremented to {signal.NewValue}");
    }
}
```

> You may notice that the handler class is `internal`. This is a recommended practice to make it clear that handlers are meant to be triggered by publishing a signal, not by calling them directly.

To make the handler receive published signals, we need to add it to the services in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Signalling.GettingStarted/Program.cs)):

```diff
var services = new ServiceCollection();

- // we'll add services and handlers here in the next steps
+ // add our signal handler to the services
+ services.AddSignalHandler<NotificationHandler>();

await using var serviceProvider = services.BuildServiceProvider();
```

Now run the application and increment a counter to see the handler react:

```txt
> dotnet run
input commands in format 'inc <counterName>' (e.g. 'inc test')
input q to quit
inc test
notification: counter 'test' was incremented to 1
q
shutting down...
```

So far this looks a lot like sending a message to a single handler. The power of signalling becomes apparent once we add a *second* handler for the same signal. Let's create a handler that keeps running statistics. Since handlers are registered as transient (a new instance is created for every published signal), any state it accumulates must live in a service with a longer lifetime, so we keep the running total in a singleton. Create a new file `StatisticsHandler.cs` ([view completed file](.completed/Conqueror.Recipes.Signalling.GettingStarted/StatisticsHandler.cs)):

```cs
namespace Conqueror.Recipes.Signalling.GettingStarted;

internal partial class StatisticsHandler(CounterStatistics statistics) : CounterIncremented.IHandler
{
    public async Task Handle(CounterIncremented signal, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        statistics.RecordedIncrements++;
        Console.WriteLine($"statistics: {statistics.RecordedIncrements} increment(s) recorded so far");
    }
}

internal class CounterStatistics
{
    public int RecordedIncrements { get; set; }
}
```

> Note that handlers are registered as transient by default. You can specify a different lifetime during registration, but we recommend against it, since we believe it encourages bad design to couple handlers to any particular scope. If your handler requires access to scoped or singleton data, this should be done by injecting instances with the correct scope into the handler (as we do here with the singleton `CounterStatistics`).

We register the singleton and the second handler in `Program.cs` ([view completed file](.completed/Conqueror.Recipes.Signalling.GettingStarted/Program.cs)):

```diff
var services = new ServiceCollection();

- // add our signal handler to the services
- services.AddSignalHandler<NotificationHandler>();
+ // add the singleton which keeps the running statistics across all published signals
+ services.AddSingleton<CounterStatistics>();
+
+ // add both signal handlers to the services
+ services.AddSignalHandler<NotificationHandler>()
+         .AddSignalHandler<StatisticsHandler>();

await using var serviceProvider = services.BuildServiceProvider();
```

Now run the application again. A single `inc` command publishes one signal, and *both* handlers react to it, even though the publishing code in `Program.cs` was not changed at all:

```txt
> dotnet run
input commands in format 'inc <counterName>' (e.g. 'inc test')
input q to quit
inc test
notification: counter 'test' was incremented to 1
statistics: 1 increment(s) recorded so far
q
shutting down...
```

This is the core value of signalling: the publisher is completely decoupled from its handlers. To add new behavior when a counter is incremented, you just write another handler and register it - the publisher stays untouched.

At this point you may come to the conclusion that it is a bit annoying that you have to add every handler separately to the services. **Conqueror** provides a convenience extension method `AddSignalHandlersFromAssembly(Assembly assembly)` which discovers and adds all handlers in an assembly to the services. Let's use that instead:

```diff
services.AddSingleton<CounterStatistics>();

- // add both signal handlers to the services
- services.AddSignalHandler<NotificationHandler>()
-         .AddSignalHandler<StatisticsHandler>();

+ // add all signal handlers automatically
+ services.AddSignalHandlersFromAssembly(typeof(Program).Assembly);

await using var serviceProvider = services.BuildServiceProvider();
```

The application behaves exactly as before, but now any new handler you add to the project is picked up automatically:

```txt
> dotnet run
input commands in format 'inc <counterName>' (e.g. 'inc test')
input q to quit
inc test
notification: counter 'test' was incremented to 1
statistics: 1 increment(s) recorded so far
inc test
notification: counter 'test' was incremented to 2
statistics: 2 increment(s) recorded so far
inc other
notification: counter 'other' was incremented to 1
statistics: 3 increment(s) recorded so far
q
shutting down...
```

You may have noticed that the handlers always run in a predictable order: the notification is printed before the statistics. By default, **Conqueror** broadcasts a signal to its handlers *sequentially*, i.e. one handler after another. If the order does not matter and you'd rather run handlers concurrently, you can switch to parallel broadcasting at the call site:

```cs
await publishers
    .For(CounterIncremented.T)
    .WithTransport(t => t.UseInProcess().WithParallelBroadcastingStrategy())
    .Handle(new(counterName, newValue));
```

This concludes our recipe for getting started with **Conqueror** signalling. In summary, you need the following:

- add the [Conqueror](https://www.nuget.org/packages/Conqueror/) NuGet package
- if you are not already writing a web application or using the [generic .NET host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host), add the [Microsoft.Extensions.DependencyInjections](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) Nuget package
- enable automatic discovery of all signal handlers:

    ```cs
    services.AddSignalHandlersFromAssembly(typeof(Program).Assembly);
    ```

- create a signal with `[Signal]`, publish it via `publishers.For(YourSignal.T).Handle(...)`, and write as many handlers for it as you need

We handled all signals in-process here, but the same publisher can also deliver signals across process boundaries via a transport (e.g. HTTP server-sent events); see the [other recipes](../../../../../..#recipes) for more details.

As the next step you can explore how to [test signal handlers](../testing-handlers).

Or head over to our [other recipes](../../../../../..#recipes) for more guidance on different topics.

If you have any suggestions for how to improve this recipe, please let us know by [creating an issue](https://github.com/MrWolfZ/Conqueror/issues/new?template=recipe-improvement-suggestion.md&title=[recipes.signalling.getting-started]%20...) or by [forking the repository](https://github.com/MrWolfZ/Conqueror/fork) and providing a pull request for the suggestion.
