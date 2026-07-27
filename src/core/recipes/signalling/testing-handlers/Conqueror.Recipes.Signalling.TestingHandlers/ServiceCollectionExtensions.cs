namespace Conqueror.Recipes.Signalling.TestingHandlers;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services
            .AddSingleton<CounterStatistics>()
            .AddSingleton<IAdminNotificationService, NoopAdminNotificationService>();

        services.AddSignalHandlersFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
    }
}
