using System.Runtime.CompilerServices;
using Conqueror;
using Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests.TopLevelProgram;
using Microsoft.AspNetCore.Mvc;

[assembly: InternalsVisibleTo("Conqueror.CQS.Transport.Http.Server.AspNetCore.Tests")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
       .AddControllers()
       .AddConquerorCQSHttpControllers();

builder.Services.AddConquerorCQSTypesFromExecutingAssembly();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseConqueror();

app.MapGet("/customQueryEndpoint", (HttpContext ctx, int payload, ITopLevelTestQueryHandler handler) => handler.Handle(new(payload), ctx.RequestAborted));

app.MapPost("/customCommandEndpoint", (HttpContext ctx, [FromBody] TopLevelTestCommand command, ITopLevelTestCommandHandler handler) => handler.Handle(command, ctx.RequestAborted));

app.MapControllers();

app.Run();
