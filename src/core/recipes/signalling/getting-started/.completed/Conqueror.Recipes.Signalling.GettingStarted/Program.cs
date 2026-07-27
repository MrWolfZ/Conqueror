global using Conqueror;
using Conqueror.Recipes.Signalling.GettingStarted;
using Microsoft.Extensions.DependencyInjection;

// since this is a simple console app, we create the service collection ourselves
var services = new ServiceCollection();

// add the singleton which keeps the running statistics across all published signals
services.AddSingleton<CounterStatistics>();

// add all signal handlers automatically
services.AddSignalHandlersFromAssembly(typeof(Program).Assembly);

await using var serviceProvider = services.BuildServiceProvider();

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
        case "inc" when counterName is not null:
            var newValue = counters.GetValueOrDefault(counterName) + 1;
            counters[counterName] = newValue;

            // publish the signal; the publisher does not know or care which handlers receive it
            await publishers.For(CounterIncremented.T).Handle(new(counterName, newValue));
            break;

        default:
            Console.WriteLine($"invalid input '{line}'");
            break;
    }
}
