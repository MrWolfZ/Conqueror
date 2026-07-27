namespace Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.EntryPoint.WebApi;

using Conqueror.Recipes.Messaging.MonolithToDistributed.Core.Application;
using Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Application;
using Conqueror.Recipes.Messaging.MonolithToDistributed.Counters.Infrastructure;
using Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Contracts;

// use a class with a custom name instead of a top-level program to distinguish
// this entry point from the ones of other bounded contexts
public sealed class CountersProgram
{
    private CountersProgram()
    {
    }

    public static async Task Main()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services
               .AddConquerorHttpServerAspNetCore()
               .AddSwaggerGen();

        builder.Services
               .AddCountersApplication()
               .AddCountersInfrastructure();

        // configure the sender for the UserHistory context's message to use the HTTP
        // transport with the base address of the UserHistory web application
        builder.Services
               .AddConquerorHttpClient()
               .AddSingleton<SetMostRecentlyIncrementedCounterForUser.IHandler>(
                   p => p.GetRequiredService<IMessageSenders>()
                         .For(SetMostRecentlyIncrementedCounterForUser.T)
                         .WithPipeline(pipeline => pipeline.UseDefault())
                         .WithTransport(b => b.UseHttp(p.GetRequiredService<IConfiguration>().GetValue<Uri>("UserHistoryBaseAddress")!)));

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapMessageEndpoints();

        await app.RunAsync();
    }
}
