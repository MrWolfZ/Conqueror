using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

public sealed class EventLoggingMiddleware<TEvent>(
    ILoggerFactory loggerFactory,
    JsonSerializerOptions jsonSerializerOptions)
    : IEventMiddleware<TEvent>
    where TEvent : class
{
    public async Task Execute(EventMiddlewareContext<TEvent> ctx)
    {
        var logger = loggerFactory.CreateLogger($"Event[{typeof(TEvent).Name}]");

        try
        {
            logger.LogInformation("Event of type {EventType} with payload {EventPayload} occurred", typeof(TEvent).Name, Serialize(ctx.Event));

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An exception occurred while publishing event of type {EventType}!", typeof(TEvent).Name);

            throw;
        }
    }

    private string Serialize<T>(T value) => JsonSerializer.Serialize(value, jsonSerializerOptions);
}

public static class LoggingEventPipelineExtensions
{
    public static IEventPipeline<TEvent> UseLogging<TEvent>(this IEventPipeline<TEvent> pipeline)
        where TEvent : class
    {
        return pipeline.Use(new EventLoggingMiddleware<TEvent>(pipeline.ServiceProvider.GetRequiredService<ILoggerFactory>(),
                                                               pipeline.ServiceProvider.GetRequiredService<JsonSerializerOptions>()));
    }
}
