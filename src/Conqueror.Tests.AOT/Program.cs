using System.Diagnostics.CodeAnalysis;
using Conqueror;
using Conqueror.Tests.AOT;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "this simulates a normal user app")]

var builder = Host.CreateDefaultBuilder();

builder.ConfigureServices(services =>
{
    services.AddMessageHandler<TestMessageHandler>()
            .AddSignalHandler<TestSignalHandler>();
});

var host = builder.Build();

var messageHandler = host.Services.GetRequiredService<IMessageSenders>().For(TestMessage.T);
var response = await messageHandler.Handle(new() { Payload = 10 });

Console.WriteLine($"got response: {response}");
