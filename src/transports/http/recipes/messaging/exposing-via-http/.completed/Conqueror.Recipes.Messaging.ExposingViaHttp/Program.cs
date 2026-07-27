using Conqueror;
using Conqueror.Recipes.Messaging.ExposingViaHttp;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddConquerorHttpServerAspNetCore()
    .AddSwaggerGen(c => c.DocInclusionPredicate((_, _) => true));

builder.Services
    .AddSingleton<CountersRepository>()
    .AddMessageHandlersFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapMessageEndpoints();

app.MapPost("/api/custom/incrementCounter",
    async (IncrementCounter message, IMessageSenders senders, CancellationToken cancellationToken) =>
    {
        var response = await senders.For(IncrementCounter.T).Handle(message, cancellationToken);
        return Results.Json(response, statusCode: StatusCodes.Status201Created);
    });

app.MapGet("/api/custom/getCounterValue",
    ([AsParameters] GetCounterValue message, IMessageSenders senders, CancellationToken cancellationToken) =>
        senders.For(GetCounterValue.T).Handle(message, cancellationToken));

await app.RunAsync();
