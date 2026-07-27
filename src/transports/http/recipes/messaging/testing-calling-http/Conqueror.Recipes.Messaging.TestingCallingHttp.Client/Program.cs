using Conqueror;
using Conqueror.Recipes.Messaging.TestingCallingHttp.Client;
using Conqueror.Recipes.Messaging.TestingCallingHttp.Contracts;
using Conqueror.Recipes.Messaging.TestingCallingHttp.Middlewares;
using Microsoft.Extensions.Hosting;

var host = await new HostBuilder().ConfigureServices(services =>
{
    services.AddConquerorHttpClient();

    // in a real application the server address would be loaded from some configuration
    // source; the HTTP client is registered in the services so that tests can replace it
    services.AddSingleton(new HttpClient { BaseAddress = new("http://localhost:5000") });

    // register ready-configured handlers for the messages we call; this gives the rest of
    // the application (and our tests) a single seam for how the messages are sent
    services.AddSingleton<IncrementCounter.IHandler>(p =>
        p.GetRequiredService<IMessageSenders>()
            .For(IncrementCounter.T)
            .WithPipeline(pipeline => pipeline.UseDataAnnotationValidation())
            .WithTransport(b => b.UseCounterServer()));

    services.AddSingleton<GetCounterValue.IHandler>(p =>
        p.GetRequiredService<IMessageSenders>()
            .For(GetCounterValue.T)
            .WithPipeline(pipeline => pipeline.UseDataAnnotationValidation())
            .WithTransport(b => b.UseCounterServer()));
}).StartAsync();

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
            var incResponse = await host.Services.GetRequiredService<IncrementCounter.IHandler>()
                                        .Handle(new(counterName));
            Console.WriteLine($"incremented counter '{counterName}'; new value: {incResponse.NewCounterValue}");
            break;

        case "get":
            var getResponse = await host.Services.GetRequiredService<GetCounterValue.IHandler>()
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
