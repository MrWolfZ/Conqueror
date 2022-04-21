using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Examples.BlazorWebAssembly.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<SharedCounter.SharedCounter>()
                .AddSingleton<InMemoryEventStore>()
                .AddTransient<IEventHub, NullEventHub>();

        services.AddConquerorCQS()
                .AddConquerorCQSTypesFromExecutingAssembly()
                .AddConquerorEventing()
                .AddConquerorEventingTypesFromExecutingAssembly();

        services.ConfigureCommandPipeline(b =>
        {
            b.Use<CommandMetricsMiddleware>()
             .Use<CommandLoggingMiddleware>()
             .Use<CommandTimeoutMiddleware>()
             .UseOptional<CommandAuthorizationMiddleware>()
             .Use<CommandValidationMiddleware>()
             .UseOptional<CommandRetryMiddleware>()
             .UseOptional<CommandTransactionMiddleware>();
        });

        services.ConfigureQueryPipeline(b =>
        {
            b.Use<QueryMetricsMiddleware>()
             .Use<QueryLoggingMiddleware>()
             .Use<QueryTimeoutMiddleware>()
             .UseOptional<QueryAuthorizationMiddleware>()
             .Use<QueryValidationMiddleware>()
             .UseOptional<QueryCachingMiddleware>();
        });

        services.ConfigureEventPublisherPipeline(b => b.Use<EventPublisherLoggingMiddleware>());

        return services;
    }
}