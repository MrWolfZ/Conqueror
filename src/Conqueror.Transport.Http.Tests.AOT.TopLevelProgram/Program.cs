using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Conqueror;
using Conqueror.Transport.Http.Server.AspNetCore.Messaging;
using Conqueror.Transport.Http.Tests.AOT.TopLevelProgram;
using Microsoft.AspNetCore.Routing.Constraints;

[assembly: InternalsVisibleTo("Conqueror.Transport.Http.Tests")]
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "this simulates a normal user app")]

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services
       .AddMessageEndpoints()
       .AddMessageHandler<TopLevelTestMessageHandler>();

builder.Services
       .AddEndpointsApiExplorer()
       .AddSwaggerGen(c =>
       {
           c.DocInclusionPredicate((_, _) => true);
       });

builder.Services.Configure<RouteOptions>(options => options.SetParameterPolicy<RegexInlineRouteConstraint>("regex"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseConquerorWellKnownErrorHandling();

app.MapPost("/api/custom", async (HttpContext ctx)
                =>
            {
                var message = await ctx.Request.ReadFromJsonAsync(TopLevelTestMessageJsonSerializerContext.Default.TopLevelTestMessage);

                if (message is null)
                {
                    return Results.BadRequest("could not parse message");
                }

                var topLevelTestMessageResponse = await ctx.HandleMessage(message);

                return Results.Json(topLevelTestMessageResponse, TopLevelTestMessageJsonSerializerContext.Default);
            });

app.MapGet("/api/customGet/{payload:int}", async (int payload, HttpContext ctx)
               =>
           {
               var message = new TopLevelTestMessage { Payload = payload, Nested = new() { NestedString = "test" } };
               var topLevelTestMessageResponse = await ctx.HandleMessage(message);

               return Results.Json(topLevelTestMessageResponse, TopLevelTestMessageJsonSerializerContext.Default);
           });

app.MapGet("/api/chained/{payload:int}", async (int payload, HttpContext ctx)
               =>
           {
               var message = new TopLevelTestMessage { Payload = payload, Nested = new() { NestedString = "test" } };
               var topLevelTestMessageResponse = await ctx.RequestServices
                                                          .GetRequiredService<IMessageSenders>()
                                                          .For(TopLevelTestMessage.T)
                                                          .WithTransport(b => b.UseHttp(new("http://localhost:5202/group/")))
                                                          .Handle(message, ctx.RequestAborted);

               return Results.Json(topLevelTestMessageResponse, TopLevelTestMessageJsonSerializerContext.Default);
           });

app.MapGroup("/group").MapMessageEndpoints();

await app.RunAsync().ConfigureAwait(false);
