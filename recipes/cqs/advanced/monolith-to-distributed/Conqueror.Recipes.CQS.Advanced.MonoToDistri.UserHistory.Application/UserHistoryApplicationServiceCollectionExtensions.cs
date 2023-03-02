using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.UserHistory.Application;

public static class UserHistoryApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddUserHistoryApplication(this IServiceCollection services)
    {
        services.AddCoreApplication()
                .AddConquerorCQSTypesFromExecutingAssembly();

        return services;
    }
}
