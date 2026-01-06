namespace Conqueror.Recipes.Messaging.TestingHandlers;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services
            .AddSingleton<CountersRepository>()
            .AddSingleton<IAdminNotificationService, NoopAdminNotificationService>();

        services.AddMessageHandlersFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
    }
}
