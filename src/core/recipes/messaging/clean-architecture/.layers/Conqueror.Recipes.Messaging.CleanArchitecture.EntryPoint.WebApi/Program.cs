using Conqueror.Recipes.Messaging.CleanArchitecture.Application;
using Conqueror.Recipes.Messaging.CleanArchitecture.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddConquerorHttpServerAspNetCore()
       .AddSwaggerGen();

builder.Services
       .AddApplication()
       .AddInfrastructure();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapMessageEndpoints();

await app.RunAsync();
