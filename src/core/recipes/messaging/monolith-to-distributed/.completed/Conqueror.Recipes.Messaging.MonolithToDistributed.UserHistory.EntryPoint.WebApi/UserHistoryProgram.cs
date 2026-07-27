namespace Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.EntryPoint.WebApi;

using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Application;
using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Infrastructure;

// use a class with a custom name instead of a top-level program to distinguish
// this entry point from the ones of other bounded contexts
public sealed class UserHistoryProgram
{
    private UserHistoryProgram()
    {
    }

    public static async Task Main()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services
               .AddConquerorHttpServerAspNetCore()
               .AddSwaggerGen();

        builder.Services
               .AddUserHistoryApplication()
               .AddUserHistoryInfrastructure();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapMessageEndpoints();

        await app.RunAsync();
    }
}
