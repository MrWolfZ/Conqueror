using Conqueror;
using Conqueror.CQS.QueryHandling;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCqsQueryServiceCollectionExtensions
{
    internal static void AddConquerorCqsQueryServices(this IServiceCollection services)
    {
        services.TryAddTransient<IQueryClientFactory, TransientQueryClientFactory>();
        services.TryAddSingleton<QueryClientFactory>();
        services.TryAddSingleton<QueryTransportRegistry>();
        services.TryAddSingleton<IQueryTransportRegistry>(p => p.GetRequiredService<QueryTransportRegistry>());

        services.AddConquerorContext();
    }
}
