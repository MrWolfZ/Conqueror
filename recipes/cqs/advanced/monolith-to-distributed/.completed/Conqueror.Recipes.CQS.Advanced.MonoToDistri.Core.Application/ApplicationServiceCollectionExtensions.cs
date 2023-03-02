using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.Core.Application;

public static class CoreApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCoreApplication(this IServiceCollection services)
    {
        services.AddConquerorCQSDataAnnotationValidationMiddlewares()
                .AddConquerorCQSLoggingMiddlewares();

        return services;
    }
}
