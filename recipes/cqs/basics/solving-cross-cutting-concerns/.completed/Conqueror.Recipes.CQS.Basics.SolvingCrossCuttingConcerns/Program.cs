using Conqueror.Recipes.CQS.Basics.SolvingCrossCuttingConcerns;

var services = new ServiceCollection();

services.AddSingleton<CountersRepository>();

services.AddSingleton(new RetryMiddlewareConfiguration { RetryAttemptLimit = 2 });

services.AddConquerorCQS()
        .AddConquerorCQSTypesFromExecutingAssembly()
        .FinalizeConquerorRegistrations();

await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

Console.WriteLine("input commands in format '<op> [counterName]' (e.g. 'inc test 1' or 'get test')");
Console.WriteLine("available operations: inc, get");
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
    var value = input.Skip(2).Select(int.Parse).FirstOrDefault();

    try
    {
        switch (op)
        {
            case "inc" when counterName != null:
                var incrementHandler = serviceProvider.GetRequiredService<IIncrementCounterByCommandHandler>();
                var incResponse = await incrementHandler.ExecuteCommand(new(counterName, value));
                Console.WriteLine($"incremented counter '{counterName}'; new value: {incResponse.NewCounterValue}");
                break;

            case "get" when counterName != null:
                var getValueHandler = serviceProvider.GetRequiredService<IGetCounterValueQueryHandler>();
                var getValueResponse = await getValueHandler.ExecuteQuery(new(counterName));
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
