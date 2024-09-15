using Conqueror;
using Conqueror.Recipes.CQS.Advanced.CallingHttp.Contracts;
using Conqueror.Recipes.CQS.Advanced.CallingHttp.Middlewares;

// in a real application this would be loaded from some configuration source
var serverAddress = new Uri("http://localhost:5000");

var services = new ServiceCollection();

services.AddConquerorCQSHttpClientServices()
        .AddConquerorCommandClient<IIncrementCounterCommandHandler>(b => b.UseHttp(serverAddress, o => o.Headers.Add("my-header", "my-value")));

await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

if (args.Length is < 1 or > 2)
{
    Console.WriteLine("input commands in format '<op> <counterName>' (e.g. 'inc test' or 'get test')");
    Console.WriteLine("available operations: inc, get");
    return;
}

var op = args.First();
var counterName = args.Skip(1).FirstOrDefault() ?? string.Empty;

try
{
    switch (op)
    {
        case "inc":
            var incrementHandler = serviceProvider.GetRequiredService<IIncrementCounterCommandHandler>();
            var incResponse = await incrementHandler.WithPipeline(pipeline => pipeline.UseDataAnnotationValidation())
                                                    .Handle(new(counterName));
            Console.WriteLine($"incremented counter '{counterName}'; new value: {incResponse.NewCounterValue}");
            break;

        case "get":
            var queryClientFactory = serviceProvider.GetRequiredService<IQueryClientFactory>();
            var getValueHandler = queryClientFactory.CreateQueryClient<IGetCounterValueQueryHandler>(b => b.UseHttp(serverAddress));
            var getValueResponse = await getValueHandler.Handle(new(counterName));
            Console.WriteLine($"counter '{counterName}' value: {getValueResponse.CounterValue}");
            break;

        default:
            Console.WriteLine($"invalid operation '{op}'");
            break;
    }
}
catch (HttpCommandFailedException commandFailedException)
{
    Console.WriteLine($"HTTP command failed with status code {(int?)commandFailedException.StatusCode}");
}
catch (HttpQueryFailedException queryFailedException)
{
    Console.WriteLine($"HTTP query failed with status code {(int?)queryFailedException.StatusCode}");
}
catch (ValidationException vex)
{
    Console.WriteLine(vex.Message);
}
