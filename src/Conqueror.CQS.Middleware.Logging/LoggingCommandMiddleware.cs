using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging;

/// <summary>
///     A command middleware which adds logging functionality to a command pipeline. By default the following messages are logged:
///     <list type="bullet">
///         <item>Before the command is executed (including the JSON-serialized command payload, if any)</item>
///         <item>After the command was executed successfully (including the JSON-serialized response payload, if any)</item>
///         <item>If an exception gets thrown during command execution</item>
///     </list>
/// </summary>
public sealed class LoggingCommandMiddleware : ICommandMiddleware<LoggingCommandMiddlewareConfiguration>
{
    /// <inheritdoc />
    public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, LoggingCommandMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        var logger = GetLogger(ctx);

        var commandId = ctx.ConquerorContext.GetCommandId() ?? "unknown";
        var traceId = ctx.ConquerorContext.TraceId;

        var sw = Stopwatch.StartNew();

        try
        {
            PreExecution(logger, commandId, traceId, ctx);

            var response = await ctx.Next(ctx.Command, ctx.CancellationToken).ConfigureAwait(false);

            PostExecution(logger, commandId, traceId, response, sw.Elapsed, ctx);

            return response;
        }
        catch (Exception e)
        {
            OnException(logger, commandId, traceId, e, sw.Elapsed, ctx);

            throw;
        }
    }

    private static void PreExecution<TCommand, TResponse>(ILogger logger,
                                                          string commandId,
                                                          string traceId,
                                                          CommandMiddlewareContext<TCommand, TResponse, LoggingCommandMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        if (ctx.Configuration.PreExecutionHook is { } preExecutionHook)
        {
            var preExecutionContext = new LoggingCommandPreExecutionContext(logger, ctx.Configuration.PreExecutionLogLevel, commandId, traceId, ctx.Command, ctx.ServiceProvider);

            if (!preExecutionHook(preExecutionContext))
            {
                return;
            }
        }

        // check if the log level is enabled so that we can skip the JSON serialization for performance
        if (!logger.IsEnabled(ctx.Configuration.PreExecutionLogLevel))
        {
            return;
        }

        var hasPayload = ctx.Command.GetType().GetProperties().Any();
        var shouldOmitPayload = ctx.Configuration.OmitJsonSerializedCommandPayload;

        if (shouldOmitPayload || !hasPayload)
        {
            logger.Log(ctx.Configuration.PreExecutionLogLevel, "Executing command (Command ID: {CommandId}, Trace ID: {TraceId})", commandId, traceId);
            return;
        }

        logger.Log(ctx.Configuration.PreExecutionLogLevel,
                   "Executing command with payload {CommandPayload} (Command ID: {CommandId}, Trace ID: {TraceId})",
                   Serialize(ctx.Command, ctx.Configuration.JsonSerializerOptions),
                   commandId,
                   traceId);
    }

    private static void PostExecution<TCommand, TResponse>(ILogger logger,
                                                           string commandId,
                                                           string traceId,
                                                           object? response,
                                                           TimeSpan elapsedTime,
                                                           CommandMiddlewareContext<TCommand, TResponse, LoggingCommandMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        if (ctx.Configuration.PostExecutionHook is { } postExecutionHook)
        {
            var postExecutionContext = new LoggingCommandPostExecutionContext(logger,
                                                                              ctx.Configuration.PostExecutionLogLevel,
                                                                              commandId,
                                                                              traceId,
                                                                              ctx.Command,
                                                                              response,
                                                                              elapsedTime,
                                                                              ctx.ServiceProvider);

            if (!postExecutionHook(postExecutionContext))
            {
                return;
            }
        }

        // check if the log level is enabled so that we can skip the JSON serialization for performance
        if (!logger.IsEnabled(ctx.Configuration.PostExecutionLogLevel))
        {
            return;
        }

        var shouldOmitPayload = ctx.Configuration.OmitJsonSerializedResponsePayload;

        if (shouldOmitPayload || ctx.HasUnitResponse)
        {
            logger.Log(ctx.Configuration.PostExecutionLogLevel,
                       "Executed command in {ResponseLatency:0.0000}ms (Command ID: {CommandId}, Trace ID: {TraceId})",
                       elapsedTime.TotalMilliseconds,
                       commandId,
                       traceId);

            return;
        }

        logger.Log(ctx.Configuration.PostExecutionLogLevel,
                   "Executed command and got response {ResponsePayload} in {ResponseLatency:0.0000}ms (Command ID: {CommandId}, Trace ID: {TraceId})",
                   Serialize(response, ctx.Configuration.JsonSerializerOptions),
                   elapsedTime.TotalMilliseconds,
                   commandId,
                   traceId);
    }

    private static void OnException<TCommand, TResponse>(ILogger logger,
                                                         string commandId,
                                                         string traceId,
                                                         Exception exception,
                                                         TimeSpan elapsedTime,
                                                         CommandMiddlewareContext<TCommand, TResponse, LoggingCommandMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        if (ctx.Configuration.ExceptionHook is { } exceptionHook)
        {
            var loggingExceptionContext = new LoggingCommandExceptionContext(logger,
                                                                             ctx.Configuration.ExceptionLogLevel,
                                                                             commandId,
                                                                             traceId,
                                                                             ctx.Command,
                                                                             exception,
                                                                             elapsedTime,
                                                                             ctx.ServiceProvider);

            if (!exceptionHook(loggingExceptionContext))
            {
                return;
            }
        }

        if (!logger.IsEnabled(ctx.Configuration.ExceptionLogLevel))
        {
            return;
        }

        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem (the exception is already included as metadata, so we just want the message
        // to include the full exception without adding it again as metadata)
        logger.Log(ctx.Configuration.ExceptionLogLevel,
                   exception,
                   $"An exception occurred while executing command after {{ResponseLatency:0.0000}}ms (Command ID: {{CommandId}}, Trace ID: {{TraceId}})\n{exception}",
                   elapsedTime.TotalMilliseconds,
                   commandId,
                   traceId);
    }

    private static ILogger GetLogger<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, LoggingCommandMiddlewareConfiguration> ctx)
        where TCommand : class
    {
        var loggerFactory = ctx.ServiceProvider.GetRequiredService<ILoggerFactory>();

        if (ctx.Configuration.LoggerNameFactory?.Invoke(ctx.Command) is { } loggerName)
        {
            return loggerFactory.CreateLogger(loggerName);
        }

        return loggerFactory.CreateLogger<TCommand>();
    }

    private static string Serialize<T>(T value, JsonSerializerOptions? jsonSerializerOptions) => JsonSerializer.Serialize(value, jsonSerializerOptions);
}
