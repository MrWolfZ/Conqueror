using System.Diagnostics.CodeAnalysis;
using Conqueror;
using Conqueror.Transport.FileSystem.Tests.AOT;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: SuppressMessage(
    "Reliability",
    "CA2007:Consider calling ConfigureAwait on the awaited task",
    Justification = "this simulates a normal user app"
)]

var builder = Host.CreateDefaultBuilder();

var baseDirectory = Path.Combine(Path.GetTempPath(), "conqueror-aot-test", Guid.NewGuid().ToString());
Directory.CreateDirectory(baseDirectory);

try
{
    builder.ConfigureServices(services =>
    {
        services.AddMessageHandler<TestMessageHandler>().AddConquerorFileSystemTransport();
    });

    var host = builder.Build();

    await using var handle = host
        .Services.GetRequiredService<IMessageReceivers>()
        .RunFileSystemMessageReceivers(CancellationToken.None);

    await handle.InitialConnectionTask;

    var messageHandler = host.Services.GetRequiredService<IMessageSenders>().For(TestMessage.T);
    var response = await messageHandler
        .WithTransport(b => b.UseFileSystem(baseDirectory, TimeSpan.FromMilliseconds(10)))
        .Handle(new() { Payload = 10 }, CancellationToken.None);

    Console.WriteLine($"got response: {response}");
}
finally
{
    if (Directory.Exists(baseDirectory))
    {
        Directory.Delete(baseDirectory, recursive: true);
    }
}
