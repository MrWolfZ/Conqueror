namespace Conqueror.Recipes.Messaging.TestingCallingHttp.Server;

public sealed class ServerProgram
{
    private ServerProgram()
    {
    }

    internal static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddConquerorHttpServerAspNetCore()
            .AddSwaggerGen();

        builder.Services
            .AddSingleton<CountersRepository>()
            .AddMessageHandlersFromAssembly(typeof(ServerProgram).Assembly);

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapMessageEndpoints();

        await app.RunAsync();
    }
}
