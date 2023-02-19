using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Infrastructure;

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
