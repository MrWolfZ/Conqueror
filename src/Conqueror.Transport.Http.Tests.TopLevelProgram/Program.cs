using System.Runtime.CompilerServices;
using Conqueror;
using Conqueror.Transport.Http.Tests.TopLevelProgram;
using Microsoft.AspNetCore.Mvc;

[assembly: InternalsVisibleTo("Conqueror.Transport.Http.Tests")]

var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

builder.Services
       .AddControllers();

////       .AddConquerorMessageHandlerControllers();

builder.Services.AddConquerorMessageHandlersFromExecutingAssembly();

var app = builder.Build();

// Configure the HTTP request pipeline.

//// app.UseConqueror();

app.MapPost("/customMessageEndpoint", (
                    HttpContext ctx,
                    [FromBody] TopLevelTestMessage message,
                    IMessageClients messageClients)
                => messageClients.For<TopLevelTestMessage.IHandler>()
                                 .WithTransport(b => b.UseInProcess()) // TODO: UseInProcessForHttpServer
                                 .Handle(message, ctx.RequestAborted));

app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
