using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.UserHistory.Application;

public static class UserHistoryApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddUserHistoryApplication(this IServiceCollection services)
    {
        services.AddConquerorCQS()
                .AddConquerorCQSTypesFromExecutingAssembly();

        return services;
    }
}
