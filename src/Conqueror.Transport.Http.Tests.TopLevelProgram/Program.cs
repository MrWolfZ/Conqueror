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
       .AddMessageControllers();

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen(c =>
       {
           c.DocInclusionPredicate((_, _) => true);
       });

builder.Services.AddMessageHandlersFromExecutingAssembly();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseConquerorWellKnownErrorHandling();

app.MapPost("/api/custom", (
                    HttpContext ctx,
                    [FromBody] TopLevelTestMessage message)
                => ctx.GetMessageClient(TopLevelTestMessage.T).Handle(message, ctx.RequestAborted));

app.MapGet("/api/customGet/{payload:int}", async (int payload, HttpContext ctx)
               =>
           {
               var message = new TopLevelTestMessage { Payload = payload, Nested = new() { NestedString = "test" } };
               return await ctx.GetMessageClient(TopLevelTestMessage.T).Handle(message, ctx.RequestAborted);
           });

app.MapGroup("/group").MapMessageEndpoint<TopLevelTestMessage>().WithName("TopLevelTestMessage2");

app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
