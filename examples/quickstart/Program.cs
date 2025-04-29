using Quickstart;

var builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddSingleton<CountersRepository>()

       // This registers all the handlers in the project; alternatively, you can register
       // individual handlers as well
       .AddMessageHandlersFromAssembly(typeof(Program).Assembly)
       .AddSignalHandlersFromAssembly(typeof(Program).Assembly)

       // Add some services that Conqueror needs to properly expose messages via HTTP
       .AddMessageEndpoints()

       // Let's enable Swashbuckle to get a nice Swagger UI
       .AddSwaggerGen();

var app = builder.Build();

app.UseSwagger()
   .UseSwaggerUI();

// This enables message handlers as minimal HTTP API endpoints (including in AOT mode
// if you need that, although please check the corresponding recipe for more details)
app.MapMessageEndpoints();

app.Run();
