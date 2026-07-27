using Conqueror.Recipes.Messaging.SolvingCrossCuttingConcerns;

var services = new ServiceCollection();

services.AddSingleton<CountersRepository>();

services.AddMessageHandlersFromAssembly(typeof(Program).Assembly);

await using var serviceProvider = services.BuildServiceProvider();

var senders = serviceProvider.GetRequiredService<IMessageSenders>();

Console.WriteLine("input commands in format '<op> [counterName] [param]' (e.g. 'inc test 1' or 'get test')");
Console.WriteLine("available operations: inc, get");
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
    var value = input.Skip(2).Select(int.Parse).FirstOrDefault();

    try
    {
        switch (op)
        {
            case "inc" when counterName is not null:
                var incResponse = await senders.For(IncrementCounterBy.T).Handle(new(counterName, value));
                Console.WriteLine($"incremented counter '{counterName}'; new value: {incResponse.NewCounterValue}");
                break;

            case "get" when counterName is not null:
                var getValueResponse = await senders.For(GetCounterValue.T).Handle(new(counterName));
                Console.WriteLine($"counter '{counterName}' value: {getValueResponse.CounterValue}");
                break;

            default:
                Console.WriteLine($"invalid input '{line}'");
                break;
        }
    }
    catch (ValidationException vex)
    {
        Console.WriteLine(vex.Message);
    }
    catch (Exception)
    {
        Console.WriteLine("an unexpected error occurred while executing operation");
    }
}
