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
        services.TryAddSingleton<QueryHandlerRegistry>();
        services.TryAddSingleton<IQueryHandlerRegistry>(p => p.GetRequiredService<QueryHandlerRegistry>());
        services.TryAddSingleton<QueryMiddlewareRegistry>();
        services.TryAddSingleton<IQueryMiddlewareRegistry>(p => p.GetRequiredService<QueryMiddlewareRegistry>());
        services.TryAddSingleton<QueryContextAccessor>();
        services.TryAddSingleton<IQueryContextAccessor>(p => p.GetRequiredService<QueryContextAccessor>());

        services.AddConquerorContext();
    }
}
