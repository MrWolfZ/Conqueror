using Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Application;
using Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Infrastructure;
using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Application;
using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Infrastructure;

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
