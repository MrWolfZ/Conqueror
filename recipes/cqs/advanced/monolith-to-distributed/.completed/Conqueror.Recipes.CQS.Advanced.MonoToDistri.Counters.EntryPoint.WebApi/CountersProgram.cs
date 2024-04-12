using Conqueror.Recipes.CQS.Advanced.MonoToDistri.Core.Application;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Application;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Infrastructure;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Contracts;

namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.EntryPoint.WebApi;

// use a class with a custom name instead of a top-level program to distinguish
// this entry point from the ones of other bounded contexts
public sealed class CountersProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
               .AddControllers()
               .AddConquerorCQSHttpControllers();

        builder.Services
               .AddEndpointsApiExplorer()
               .AddSwaggerGen();

        builder.Services
               .AddCountersApplication()
               .AddCountersInfrastructure();

        builder.Services
               .AddConquerorCQSHttpClientServices()
               .AddConquerorCommandClient<ISetMostRecentlyIncrementedCounterForUserCommandHandler>(
                   b => b.UseHttp(b.ServiceProvider.GetRequiredService<IConfiguration>().GetValue<Uri>("UserHistoryBaseAddress")!),
                   pipeline => pipeline.UseDefault());

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseConqueror();
        app.MapControllers();

        app.Run();
    }
}
