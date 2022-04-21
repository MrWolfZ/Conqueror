using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

internal static class MiddlewareServiceCollectionExtensions
{
    public static IServiceCollection ConfigureCommandPipeline(this IServiceCollection services, Action<ICommandPipelineBuilder> configure)
    {
        return services;
    }
    
    public static IServiceCollection ConfigureQueryPipeline(this IServiceCollection services, Action<IQueryPipelineBuilder> configure)
    {
        return services;
    }
    
    public static IServiceCollection ConfigureEventPublisherPipeline(this IServiceCollection services, Action<IEventPublisherPipelineBuilder> configure)
    {
        return services;
    }
    
    public static IServiceCollection ConfigureEventObserverPipeline(this IServiceCollection services, Action<IEventObserverPipelineBuilder> configure)
    {
        return services;
    }
}

internal interface ICommandPipelineBuilder
{
    ICommandPipelineBuilder Use<TMiddleware>();
    
    ICommandPipelineBuilder UseOptional<TMiddleware>();
}

internal interface IQueryPipelineBuilder
{
    IQueryPipelineBuilder Use<TMiddleware>();
    
    IQueryPipelineBuilder UseOptional<TMiddleware>();
}

internal interface IEventPublisherPipelineBuilder
{
    IEventPublisherPipelineBuilder Use<TMiddleware>();
    
    IEventPublisherPipelineBuilder UseOptional<TMiddleware>();
}

internal interface IEventObserverPipelineBuilder
{
    IEventObserverPipelineBuilder Use<TMiddleware>();
    
    IEventObserverPipelineBuilder UseOptional<TMiddleware>();
}