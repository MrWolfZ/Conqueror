using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Recipes.CQS.Advanced.MonoToDistri.Counters.Infrastructure;

public static class CountersInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCountersInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<CountersRepository>()
                .AddSingleton<ICountersReadRepository>(p => p.GetRequiredService<CountersRepository>())
                .AddSingleton<ICountersWriteRepository>(p => p.GetRequiredService<CountersRepository>());

        return services;
    }
}
