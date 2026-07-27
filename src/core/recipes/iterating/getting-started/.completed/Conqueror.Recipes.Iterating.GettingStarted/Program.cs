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
        case "list" when country is not null:
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
