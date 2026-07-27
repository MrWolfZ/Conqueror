namespace Conqueror.Recipes.Messaging.TestingMiddlewares;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(new RetryMiddlewareConfiguration { RetryAttemptLimit = 1 });

        services.AddMessageHandlersFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
    }
}
