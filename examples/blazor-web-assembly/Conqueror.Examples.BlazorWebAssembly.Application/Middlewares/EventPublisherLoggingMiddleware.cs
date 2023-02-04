namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed class EventPublisherLoggingMiddleware : IEventPublisherMiddleware
{
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly ILoggerFactory loggerFactory;

    public EventPublisherLoggingMiddleware(ILoggerFactory loggerFactory, JsonSerializerOptions jsonSerializerOptions)
    {
        this.loggerFactory = loggerFactory;
        this.jsonSerializerOptions = jsonSerializerOptions;
    }

    public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
        where TEvent : class
    {
        var logger = loggerFactory.CreateLogger($"EventPublisher[{typeof(TEvent).Name}]");

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
