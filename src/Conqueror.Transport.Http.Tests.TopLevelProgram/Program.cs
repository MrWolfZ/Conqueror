using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Conqueror;
using Conqueror.Transport.Http.Tests.TopLevelProgram;
using Microsoft.AspNetCore.Mvc;

[assembly: InternalsVisibleTo("Conqueror.Transport.Http.Tests")]
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "this simulates a normal user app")]

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen(c =>
       {
           c.DocInclusionPredicate((_, _) => true);
       });

builder.Services.AddMessageHandlersFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseConquerorWellKnownErrorHandling();

app.MapPost("/api/custom", (
                    [FromBody] TopLevelTestMessage message,
                    IMessageSenders senders)
                => senders.For(TopLevelTestMessage.T).Handle(message));

app.MapGet("/api/customGet/{payload:int}", async (int payload, IMessageSenders senders)
               =>
           {
               var message = new TopLevelTestMessage { Payload = payload, Nested = new() { NestedString = "test" } };
               return await senders.For(TopLevelTestMessage.T).Handle(message);
           });

app.MapGroup("/group").MapMessageEndpoint(TopLevelTestMessage.T)?.WithName("TopLevelTestMessage2");

await app.RunAsync().ConfigureAwait(false);
