using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Conqueror;
using Conqueror.Transport.Http.Server.AspNetCore.Messaging;
using Conqueror.Transport.Http.Tests.TopLevelProgram;
using Microsoft.AspNetCore.Mvc;

[assembly: InternalsVisibleTo("Conqueror.Transport.Http.Tests")]
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "this simulates a normal user app")]

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddControllers()
       .AddConquerorMessageControllers();

builder.Services.AddConquerorMessageHandlersFromExecutingAssembly();

var app = builder.Build();

app.UseConqueror();

app.MapPost("/api/custom", (
                    HttpContext ctx,
                    [FromBody] TopLevelTestMessage message)
                => ctx.HandleMessage(message));

app.MapGet("/api/customGet/{payload:int}", async (int payload, HttpContext ctx)
               =>
           {
               var message = new TopLevelTestMessage { Payload = payload, Nested = new() { NestedString = "test" } };
               return await ctx.HandleMessage(message);
           });

app.MapGroup("/group").MapConquerorMessageEndpoint<TopLevelTestMessage>().WithName("TopLevelTestMessage2");

app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
