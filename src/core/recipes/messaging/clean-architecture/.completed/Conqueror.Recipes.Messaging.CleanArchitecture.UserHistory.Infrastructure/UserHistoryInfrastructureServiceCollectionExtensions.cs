namespace Conqueror.Recipes.Messaging.CleanArchitecture.UserHistory.Infrastructure;

using Microsoft.Extensions.DependencyInjection;

public static class UserHistoryInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddUserHistoryInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<UserHistoryRepository>()
                .AddSingleton<IUserHistoryReadRepository>(p => p.GetRequiredService<UserHistoryRepository>())
                .AddSingleton<IUserHistoryWriteRepository>(p => p.GetRequiredService<UserHistoryRepository>());

        return services;
    }
}
