using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.CQS.Advanced.CleanArchitecture.Counters.Application;

public static class CountersApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCountersApplication(this IServiceCollection services)
    {
        services.AddConquerorCQS()
                .AddConquerorCQSTypesFromExecutingAssembly();

        return services;
    }
}
