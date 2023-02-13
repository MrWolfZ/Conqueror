// in a real application this would be loaded from some configuration source
var serverAddress = new Uri("http://localhost:5000");

var services = new ServiceCollection();

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
            // TODO: call increment command on server
            break;

        case "get":
            // TODO: get counter value from server
            break;

        default:
            Console.WriteLine($"invalid operation '{op}'");
            break;
    }
}
catch (ValidationException vex)
{
    Console.WriteLine(vex.Message);
}
