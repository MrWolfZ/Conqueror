using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Application;
using Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Infrastructure;

namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.EntryPoint.WebApi;

public sealed class UserHistoryProgram
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
               .AddUserHistoryApplication()
               .AddUserHistoryInfrastructure();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseConqueror();
        app.MapControllers();

        app.Run();
    }
}
