using Quickstart;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddSingleton<CountersRepository>()
    // This registers all the handlers in the project; alternatively, you can register
    // individual handlers as well
    .AddMessageHandlersFromAssembly(typeof(Program).Assembly)
    .AddSignalHandlersFromAssembly(typeof(Program).Assembly)
    // Add services that Conqueror needs to properly expose things via HTTP
    .AddConquerorHttpServerAspNetCore()
    // Let's also enable Swashbuckle to get a nice Swagger UI
    .AddSwaggerGen();

var app = builder.Build();

app.UseSwagger().UseSwaggerUI();

// This enables message handlers as minimal HTTP API endpoints (including in AOT mode
// if you need that, although please check the corresponding recipe for more details)
app.MapMessageEndpoints();

// This adds a minimal API endpoint which allows consuming published signals
// via Server-Sent Events
app.MapServerSentEventsSignalsEndpoint("api/signals/sse");

await app.RunAsync();
