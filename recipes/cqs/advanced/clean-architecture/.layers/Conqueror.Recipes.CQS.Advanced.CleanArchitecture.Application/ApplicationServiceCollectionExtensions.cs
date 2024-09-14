using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddConquerorCQSTypesFromExecutingAssembly();

        return services;
    }
}
