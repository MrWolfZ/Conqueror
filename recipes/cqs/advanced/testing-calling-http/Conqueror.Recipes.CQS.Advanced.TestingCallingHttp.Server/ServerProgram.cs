namespace Conqueror.Recipes.CQS.Advanced.TestingCallingHttp.Server;

public sealed class ServerProgram
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
               .AddSingleton<CountersRepository>()
               .AddConquerorCQSTypesFromExecutingAssembly()
               .AddConquerorCQSDataAnnotationValidationMiddlewares();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseConqueror();
        app.MapControllers();

        app.Run();
    }
}
