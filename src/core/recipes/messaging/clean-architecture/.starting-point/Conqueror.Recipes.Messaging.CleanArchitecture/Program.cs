using Conqueror.Recipes.Messaging.CleanArchitecture;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddConquerorHttpServerAspNetCore()
       .AddSwaggerGen();

builder.Services
       .AddSingleton<CountersRepository>()
       .AddSingleton<UserHistoryRepository>()
       .AddMessageHandlersFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapMessageEndpoints();

await app.RunAsync();
