global using Conqueror;
using Conqueror.Recipes.Messaging.GettingStarted;
using Microsoft.Extensions.DependencyInjection;

// since this is a simple console app, we create the service collection ourselves
var services = new ServiceCollection();

// add the in-memory repository, which contains the counters, as a singleton
services.AddSingleton<CountersRepository>();

// add all handlers automatically
services.AddMessageHandlersFromAssembly(typeof(Program).Assembly);

await using var serviceProvider = services.BuildServiceProvider();

var senders = serviceProvider.GetRequiredService<IMessageSenders>();

Console.WriteLine("input commands in format '<op> [counterName]' (e.g. 'inc test' or 'list')");
Console.WriteLine("available operations: list, get, inc, del");
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

    try
    {
        switch (op)
        {
            case "list" when counterName is null:
                var listResponse = await senders.For(GetCounterNames.T).Handle(new());
                Console.WriteLine(
                    listResponse.CounterNames.Count > 0
                        ? $"counters:\n{string.Join('\n', listResponse.CounterNames)}"
                        : "no counters exist"
                );
                break;

            case "get" when counterName is not null:
                var getValueResponse = await senders.For(GetCounterValue.T).Handle(new(counterName));
                Console.WriteLine($"counter '{counterName}' value: {getValueResponse.CounterValue}");
                break;

            case "inc" when counterName is not null:
                var incResponse = await senders.For(IncrementCounter.T).Handle(new(counterName));
                Console.WriteLine($"incremented counter '{counterName}'; new value: {incResponse.NewCounterValue}");
                break;

            case "del" when counterName is not null:
                await senders.For(DeleteCounter.T).Handle(new(counterName));
                Console.WriteLine($"deleted counter '{counterName}'");
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
