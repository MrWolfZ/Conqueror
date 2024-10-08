global using Conqueror;
using Conqueror.Recipes.CQS.Basics.GettingStarted;
using Microsoft.Extensions.DependencyInjection;

// since this is a simple console app, we create the service collection ourselves
var services = new ServiceCollection();

// add the in-memory repository, which contains the counters, as a singleton
services.AddSingleton<CountersRepository>();

// add all handlers automatically
services.AddConquerorCQSTypesFromExecutingAssembly();

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
                var listHandler = serviceProvider.GetRequiredService<IQueryHandler<GetCounterNamesQuery, GetCounterNamesQueryResponse>>();
                var listResponse = await listHandler.Handle(new());
                Console.WriteLine(listResponse.CounterNames.Any() ? $"counters:\n{string.Join("\n", listResponse.CounterNames)}" : "no counters exist");
                break;

            case "get" when counterName != null:
                var getValueHandler = serviceProvider.GetRequiredService<IGetCounterValueQueryHandler>();
                var getValueResponse = await getValueHandler.Handle(new(counterName));
                Console.WriteLine($"counter '{counterName}' value: {getValueResponse.CounterValue}");
                break;

            case "inc" when counterName != null:
                var incrementHandler = serviceProvider.GetRequiredService<IIncrementCounterCommandHandler>();
                var incResponse = await incrementHandler.Handle(new(counterName));
                Console.WriteLine($"incremented counter '{counterName}'; new value: {incResponse.NewCounterValue}");
                break;

            case "del" when counterName != null:
                var deleteHandler = serviceProvider.GetRequiredService<IDeleteCounterCommandHandler>();
                await deleteHandler.Handle(new(counterName));
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
