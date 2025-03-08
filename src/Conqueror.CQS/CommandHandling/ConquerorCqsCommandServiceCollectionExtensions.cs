using Conqueror;
using Conqueror.CQS.CommandHandling;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCqsCommandServiceCollectionExtensions
{
    internal static void AddConquerorCqsCommandServices(this IServiceCollection services)
    {
        services.TryAddTransient<ICommandClientFactory, TransientCommandClientFactory>();
        services.TryAddSingleton<CommandClientFactory>();
        services.TryAddSingleton<CommandTransportRegistry>();
        services.TryAddSingleton<ICommandTransportRegistry>(p => p.GetRequiredService<CommandTransportRegistry>());

        services.AddConquerorContext();
    }
}
