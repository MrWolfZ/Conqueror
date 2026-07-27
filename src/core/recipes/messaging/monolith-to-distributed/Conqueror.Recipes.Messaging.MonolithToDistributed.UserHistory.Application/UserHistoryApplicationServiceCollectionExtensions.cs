namespace Conqueror.Recipes.Messaging.MonolithToDistributed.UserHistory.Application;

using Microsoft.Extensions.DependencyInjection;

public static class UserHistoryApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddUserHistoryApplication(this IServiceCollection services)
    {
        services.AddCoreApplication()
                .AddMessageHandlersFromAssembly(typeof(UserHistoryApplicationServiceCollectionExtensions).Assembly);

        return services;
    }
}
