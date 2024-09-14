using Conqueror.Examples.BlazorWebAssembly.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Examples.BlazorWebAssembly.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<SharedCounter>()
                .AddTransient<IEventHub, NullEventHub>();

        services.AddConquerorCQSTypesFromExecutingAssembly()
                .AddConquerorEventingTypesFromExecutingAssembly()
                .AddConquerorEventObserver<InMemoryEventStore>(ServiceLifetime.Singleton);

        return services;
    }
}
