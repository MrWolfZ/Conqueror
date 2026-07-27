using Conqueror.Recipes.Messaging.ExposingViaHttp;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

builder.Services
    .AddSingleton<CountersRepository>()
    .AddMessageHandlersFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

await app.RunAsync();
