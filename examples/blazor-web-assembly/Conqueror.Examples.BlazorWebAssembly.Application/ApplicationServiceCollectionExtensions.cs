using Conqueror.Examples.BlazorWebAssembly.Domain;
using Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Examples.BlazorWebAssembly.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<SharedCounter>()
                .AddSingleton<InMemoryEventStore>()
                .AddTransient<IEventHub, NullEventHub>();

        services.AddConquerorCQSTypesFromExecutingAssembly()
                .AddConquerorCQSTypesFromAssembly(typeof(CommandTimeoutMiddleware).Assembly)
                .AddConquerorEventing()
                .AddConquerorEventingTypesFromExecutingAssembly();

        return services;
    }
}
