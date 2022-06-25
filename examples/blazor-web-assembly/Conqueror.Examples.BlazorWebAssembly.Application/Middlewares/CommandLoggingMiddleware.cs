namespace Conqueror.Examples.BlazorWebAssembly.Application.Middlewares;

public sealed record CommandLoggingMiddlewareConfiguration
{
    public bool LogException { get; init; } = true;

    public bool LogCommandPayload { get; init; } = true;

    public bool LogResponsePayload { get; init; } = true;
}

public sealed class CommandLoggingMiddleware : ICommandMiddleware<CommandLoggingMiddlewareConfiguration>
{
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly ILoggerFactory loggerFactory;

    public CommandLoggingMiddleware(ILoggerFactory loggerFactory, JsonSerializerOptions jsonSerializerOptions)
    {
        this.loggerFactory = loggerFactory;
        this.jsonSerializerOptions = jsonSerializerOptions;
    }

    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, CommandLoggingMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        var logger = loggerFactory.CreateLogger($"CommandHandler[{typeof(TCommand).Name},{typeof(TResponse).Name}]");

        try
        {
            if (ctx.Configuration.LogCommandPayload)
            {
                logger.LogInformation("Handling command of type {CommandType} with payload {CommandPayload}", typeof(TCommand).Name, Serialize(ctx.Command));
            }
            else
            {
                logger.LogInformation("Handling command of type {CommandType}", typeof(TCommand).Name);
            }

            var response = await ctx.Next(ctx.Command, ctx.CancellationToken);

            if (ctx.Configuration.LogResponsePayload && !ctx.HasUnitResponse)
            {
                logger.LogInformation("Handled command of type {CommandType} and got response {ResponsePayload}", typeof(TCommand).Name, Serialize(response));
            }
            else
            {
                logger.LogInformation("Handled command of type {CommandType}", typeof(TCommand).Name);
            }

            return response;
        }
        catch (Exception e)
        {
            if (ctx.Configuration.LogException)
            {
                logger.LogError(e, "An exception occurred while handling command of type {CommandType}!", typeof(TCommand).Name);
            }
            else
            {
                logger.LogError("An exception occurred while handling command of type {CommandType}!", typeof(TCommand).Name);
            }

            throw;
        }
    }

    private string Serialize<T>(T value) => JsonSerializer.Serialize(value, jsonSerializerOptions);
}

public static class LoggingCommandPipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseLogging(this ICommandPipelineBuilder pipeline,
                                                     Func<CommandLoggingMiddlewareConfiguration, CommandLoggingMiddlewareConfiguration>? configure = null)
    {
        return pipeline.Use<CommandLoggingMiddleware, CommandLoggingMiddlewareConfiguration>(new())
                       .ConfigureLogging(configure ?? (c => c));
    }

    public static ICommandPipelineBuilder ConfigureLogging(this ICommandPipelineBuilder pipeline,
                                                           Func<CommandLoggingMiddlewareConfiguration, CommandLoggingMiddlewareConfiguration> configure)
    {
        return pipeline.Configure<CommandLoggingMiddleware, CommandLoggingMiddlewareConfiguration>(configure);
    }
}
