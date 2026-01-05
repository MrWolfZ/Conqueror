using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Conqueror;
using Conqueror.Middleware.Authorization.Tests.AOT;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: SuppressMessage(
    "Reliability",
    "CA2007:Consider calling ConfigureAwait on the awaited task",
    Justification = "this simulates a normal user app"
)]

var builder = Host.CreateDefaultBuilder();

builder.ConfigureServices(services =>
{
    services.AddMessageHandler<TestMessageHandler>();
});

var host = builder.Build();

var conquerorContext = host.Services.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();
conquerorContext.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity("test"));

var messageHandler = host.Services.GetRequiredService<IMessageSenders>().For(TestMessage.T);
var response = await messageHandler
    .WithPipeline(pipeline => pipeline.UseAuthorization(c => c
        .AddAuthorizationCheck("test", ctx => ctx.Success())))
    .Handle(new() { Payload = 10 }, CancellationToken.None);

Console.WriteLine($"got response: {response}");
