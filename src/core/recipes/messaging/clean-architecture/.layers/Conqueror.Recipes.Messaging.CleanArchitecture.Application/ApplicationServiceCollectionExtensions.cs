namespace Conqueror.Recipes.Messaging.CleanArchitecture.Application;

using Microsoft.Extensions.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMessageHandlersFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);

        return services;
    }
}
