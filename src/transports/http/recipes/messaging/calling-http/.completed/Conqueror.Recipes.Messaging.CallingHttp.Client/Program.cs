using Conqueror;
using Conqueror.Recipes.Messaging.CallingHttp.Client;
using Conqueror.Recipes.Messaging.CallingHttp.Contracts;
using Conqueror.Recipes.Messaging.CallingHttp.Middlewares;

var services = new ServiceCollection();

services.AddConquerorHttpClient();

await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

var senders = serviceProvider.GetRequiredService<IMessageSenders>();

if (args.Length is < 1 or > 2)
{
    Console.WriteLine("input commands in format '<op> <counterName>' (e.g. 'inc test' or 'get test')");
    Console.WriteLine("available operations: inc, get");
    return;
}

var op = args[0];
var counterName = args.Skip(1).FirstOrDefault() ?? "";

try
{
    switch (op)
    {
        case "inc":
            var incResponse = await senders.For(IncrementCounter.T)
                                           .WithPipeline(p => p.UseDataAnnotationValidation())
                                           .WithTransport(b => b.UseCounterServer())
                                           .Handle(new(counterName));
            Console.WriteLine($"incremented counter '{counterName}'; new value: {incResponse.NewCounterValue}");
            break;

        case "get":
            var getResponse = await senders.For(GetCounterValue.T)
                                           .WithPipeline(p => p.UseDataAnnotationValidation())
                                           .WithTransport(b => b.UseCounterServer())
                                           .Handle(new(counterName));
            Console.WriteLine(getResponse.CounterExists
                                  ? $"counter '{counterName}' value: {getResponse.CounterValue}"
                                  : $"counter '{counterName}' does not exist");
            break;

        default:
            Console.WriteLine($"invalid operation '{op}'");
            break;
    }
}
catch (HttpMessageFailedOnClientException httpException)
{
    Console.WriteLine($"HTTP message failed with status code {(int?)httpException.StatusCode}");
}
catch (ValidationException validationException)
{
    Console.WriteLine(validationException.Message);
}
