using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Examples.BlazorWebAssembly.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<SharedCounter.SharedCounter>()
                .AddSingleton<InMemoryEventStore>();

        services.AddConquerorCQS()
                .AddConquerorCQSTypesFromExecutingAssembly()
                .AddConquerorEventing()
                .AddConquerorEventingTypesFromExecutingAssembly();

        return services;
    }
}
