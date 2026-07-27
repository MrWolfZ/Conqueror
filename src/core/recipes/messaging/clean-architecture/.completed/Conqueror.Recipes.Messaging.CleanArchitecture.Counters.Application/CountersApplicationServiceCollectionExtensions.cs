namespace Conqueror.Recipes.Messaging.CleanArchitecture.Counters.Application;

using Microsoft.Extensions.DependencyInjection;

public static class CountersApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCountersApplication(this IServiceCollection services)
    {
        services.AddCoreApplication()
                .AddMessageHandlersFromAssembly(typeof(CountersApplicationServiceCollectionExtensions).Assembly);

        return services;
    }
}
