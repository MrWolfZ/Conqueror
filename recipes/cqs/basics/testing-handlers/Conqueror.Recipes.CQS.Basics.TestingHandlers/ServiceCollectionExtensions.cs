using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.CQS.Basics.TestingHandlers;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<CountersRepository>()
                .AddSingleton<IAdminNotificationService, NoopAdminNotificationService>();

        services.AddConquerorCQS()
                .AddConquerorCQSTypesFromExecutingAssembly()
                .FinalizeConquerorRegistrations();
    }
}
