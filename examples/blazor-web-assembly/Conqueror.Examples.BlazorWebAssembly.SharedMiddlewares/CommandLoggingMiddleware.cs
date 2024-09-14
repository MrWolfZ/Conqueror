using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conqueror.Examples.BlazorWebAssembly.SharedMiddlewares;

public sealed record CommandLoggingMiddlewareConfiguration
{
    public bool LogException { get; init; } = true;

    public bool LogCommandPayload { get; init; } = true;

    public bool LogResponsePayload { get; init; } = true;
}

public sealed class CommandLoggingMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
{
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly ILoggerFactory loggerFactory;

    public CommandLoggingMiddleware(ILoggerFactory loggerFactory, JsonSerializerOptions jsonSerializerOptions)
    {
        this.loggerFactory = loggerFactory;
        this.jsonSerializerOptions = jsonSerializerOptions;
    }

    public required CommandLoggingMiddlewareConfiguration Configuration { get; init; }

    public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
    {
        var logger = loggerFactory.CreateLogger($"CommandHandler[{typeof(TCommand).Name},{typeof(TResponse).Name}]");

        try
        {
            if (Configuration.LogCommandPayload)
            {
                logger.LogInformation("Handling command of type {CommandType} with payload {CommandPayload}", typeof(TCommand).Name, Serialize(ctx.Command));
            }
            else
            {
                logger.LogInformation("Handling command of type {CommandType}", typeof(TCommand).Name);
            }

            var response = await ctx.Next(ctx.Command, ctx.CancellationToken);

            if (Configuration.LogResponsePayload && !ctx.HasUnitResponse)
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
            if (Configuration.LogException)
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

public static class LoggingCommandPipelineExtensions
{
    public static ICommandPipeline<TCommand, TResponse> UseLogging<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                        Action<CommandLoggingMiddlewareConfiguration>? configure = null)
        where TCommand : class
    {
        var configuration = new CommandLoggingMiddlewareConfiguration();
        configure?.Invoke(configuration);

        var loggerFactory = pipeline.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var jsonSerializerOptions = pipeline.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
        return pipeline.Use(new CommandLoggingMiddleware<TCommand, TResponse>(loggerFactory, jsonSerializerOptions) { Configuration = configuration });
    }

    public static ICommandPipeline<TCommand, TResponse> ConfigureLogging<TCommand, TResponse>(this ICommandPipeline<TCommand, TResponse> pipeline,
                                                                                              Action<CommandLoggingMiddlewareConfiguration> configure)
        where TCommand : class
    {
        return pipeline.Configure<CommandLoggingMiddleware<TCommand, TResponse>>(m => configure(m.Configuration));
    }
}
