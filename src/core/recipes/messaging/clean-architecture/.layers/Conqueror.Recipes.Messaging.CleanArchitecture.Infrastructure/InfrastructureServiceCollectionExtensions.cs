namespace Conqueror.Recipes.Messaging.CleanArchitecture.Infrastructure;

using Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<CountersRepository>()
                .AddSingleton<ICountersReadRepository>(p => p.GetRequiredService<CountersRepository>())
                .AddSingleton<ICountersWriteRepository>(p => p.GetRequiredService<CountersRepository>());

        services.AddSingleton<UserHistoryRepository>()
                .AddSingleton<IUserHistoryReadRepository>(p => p.GetRequiredService<UserHistoryRepository>())
                .AddSingleton<IUserHistoryWriteRepository>(p => p.GetRequiredService<UserHistoryRepository>());

        return services;
    }
}
