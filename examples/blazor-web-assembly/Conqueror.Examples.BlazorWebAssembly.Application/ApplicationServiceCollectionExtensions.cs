using Conqueror.Examples.BlazorWebAssembly.Domain;
using Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Examples.BlazorWebAssembly.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<SharedCounter>()
                .AddTransient<IEventHub, NullEventHub>();

        services.AddConquerorCQSTypesFromExecutingAssembly()
                .AddConquerorCQSTypesFromAssembly(typeof(CommandTimeoutMiddleware).Assembly)
                .AddConquerorEventingTypesFromExecutingAssembly()
                .AddConquerorEventObserver<InMemoryEventStore>(ServiceLifetime.Singleton);

        return services;
    }
}
