using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Conqueror;
using Conqueror.Transport.Http.Tests.TopLevelProgram;
using Microsoft.AspNetCore.Mvc;

[assembly: InternalsVisibleTo("Conqueror.Transport.Http.Tests")]
[assembly: SuppressMessage(
    "Reliability",
    "CA2007:Consider calling ConfigureAwait on the awaited task",
    Justification = "this simulates a normal user app"
)]

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddConquerorHttpServerAspNetCore()
    .AddMessageHandlersFromAssembly(typeof(Program).Assembly)
    .AddSwaggerGen(c =>
    {
        c.DocInclusionPredicate((_, _) => true);
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseConquerorWellKnownErrorHandling();

app.MapPost(
    "/api/custom",
    ([FromBody] TopLevelTestMessage message, IMessageSenders senders) =>
        senders.For(TopLevelTestMessage.T).Handle(message, CancellationToken.None)
);

app.MapGet(
    "/api/customGet/{payload:int}",
    (int payload, IMessageSenders senders) =>
    {
        var message = new TopLevelTestMessage
        {
            Payload = payload,
            Nested = new NestedObject { NestedString = "test" },
        };

        return senders.For(TopLevelTestMessage.T).Handle(message, CancellationToken.None);
    }
);

app.MapGroup("/group").MapMessageEndpoint(TopLevelTestMessage.T)?.WithName("TopLevelTestMessage2");

await app.RunAsync().ConfigureAwait(false);
