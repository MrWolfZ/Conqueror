using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Application;

public static class CountersApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCountersApplication(this IServiceCollection services)
    {
        services.AddCoreApplication()
                .AddConquerorCQSTypesFromExecutingAssembly();

        return services;
    }
}
