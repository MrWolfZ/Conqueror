using Conqueror.Recipes.Messaging.CleanArchitecture.Counters.Application;
using Conqueror.Recipes.Messaging.CleanArchitecture.Counters.Infrastructure;
using Conqueror.Recipes.Messaging.CleanArchitecture.UserHistory.Application;
using Conqueror.Recipes.Messaging.CleanArchitecture.UserHistory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddConquerorHttpServerAspNetCore()
       .AddSwaggerGen();

builder.Services
       .AddCountersApplication()
       .AddCountersInfrastructure()
       .AddUserHistoryApplication()
       .AddUserHistoryInfrastructure();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapMessageEndpoints();

await app.RunAsync();
