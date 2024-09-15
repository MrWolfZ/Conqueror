using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Conqueror.CQS.CommandHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging;

/// <summary>
///     A command middleware which adds logging functionality to a command pipeline. By default, the following messages are logged:
///     <list type="bullet">
///         <item>Before the command is executed (including the JSON-serialized command payload, if any)</item>
///         <item>After the command was executed successfully (including the JSON-serialized response payload, if any)</item>
///         <item>If an exception gets thrown during command execution</item>
///     </list>
/// </summary>
[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "it is more natural for the log message to be lowercase")]
public sealed class LoggingCommandMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
    where TCommand : class
{
    public required LoggingCommandMiddlewareConfiguration Configuration { get; init; }

    /// <inheritdoc />
    public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
    {
        var logger = GetLogger(ctx);

        var commandId = ctx.ConquerorContext.GetCommandId() ?? "unknown";
        var traceId = ctx.ConquerorContext.GetTraceId();

        var sw = Stopwatch.StartNew();
        StackTrace? executionStackTrace = null;

        try
        {
            PreExecution(logger, commandId, traceId, ctx);

            executionStackTrace = new(true);
            var response = await ctx.Next(ctx.Command, ctx.CancellationToken).ConfigureAwait(false);

            PostExecution(logger, commandId, traceId, response, sw.Elapsed, ctx);

            return response;
        }
        catch (Exception e)
        {
            OnException(logger, commandId, traceId, e, executionStackTrace ?? new(true), sw.Elapsed, ctx);

            throw;
        }
    }

    private void PreExecution(ILogger logger,
                              string commandId,
                              string traceId,
                              CommandMiddlewareContext<TCommand, TResponse> ctx)
    {
        if (Configuration.PreExecutionHook is { } preExecutionHook)
        {
            var preExecutionContext = new LoggingCommandPreExecutionContext(logger, Configuration.PreExecutionLogLevel, commandId, traceId, ctx.Command, ctx.ServiceProvider);

            if (!preExecutionHook(preExecutionContext))
            {
                return;
            }
        }

        // check if the log level is enabled so that we can skip the JSON serialization for performance
        if (!logger.IsEnabled(Configuration.PreExecutionLogLevel))
        {
            return;
        }

        var hasPayload = ctx.Command.GetType().GetProperties().Any();
        var shouldOmitPayload = Configuration.OmitJsonSerializedCommandPayload;

        if (shouldOmitPayload || !hasPayload)
        {
            if (ctx.TransportType.IsInProcess())
            {
                logger.Log(Configuration.PreExecutionLogLevel, "Executing command (Command ID: {CommandId}, Trace ID: {TraceId})", commandId, traceId);
                return;
            }

            logger.Log(Configuration.PreExecutionLogLevel,
                       "Executing {TransportTypeName} command on {TransportRole} (Command ID: {CommandId}, Trace ID: {TraceId})",
                       ctx.TransportType.Name,
                       Enum.GetName(ctx.TransportType.Role)?.ToLowerInvariant(),
                       commandId,
                       traceId);
            return;
        }

        if (ctx.TransportType.IsInProcess())
        {
            logger.Log(Configuration.PreExecutionLogLevel,
                       "Executing command with payload {CommandPayload} (Command ID: {CommandId}, Trace ID: {TraceId})",
                       Serialize(ctx.Command, Configuration.JsonSerializerOptions),
                       commandId,
                       traceId);

            return;
        }

        logger.Log(Configuration.PreExecutionLogLevel,
                   "Executing {TransportTypeName} command on {TransportRole} with payload {CommandPayload} (Command ID: {CommandId}, Trace ID: {TraceId})",
                   ctx.TransportType.Name,
                   Enum.GetName(ctx.TransportType.Role)?.ToLowerInvariant(),
                   Serialize(ctx.Command, Configuration.JsonSerializerOptions),
                   commandId,
                   traceId);
    }

    private void PostExecution(ILogger logger,
                               string commandId,
                               string traceId,
                               object? response,
                               TimeSpan elapsedTime,
                               CommandMiddlewareContext<TCommand, TResponse> ctx)
    {
        if (Configuration.PostExecutionHook is { } postExecutionHook)
        {
            var postExecutionContext = new LoggingCommandPostExecutionContext(logger,
                                                                              Configuration.PostExecutionLogLevel,
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
        if (!logger.IsEnabled(Configuration.PostExecutionLogLevel))
        {
            return;
        }

        var shouldOmitPayload = Configuration.OmitJsonSerializedResponsePayload;

        if (shouldOmitPayload || ctx.HasUnitResponse)
        {
            if (ctx.TransportType.IsInProcess())
            {
                logger.Log(Configuration.PostExecutionLogLevel,
                           "Executed command in {ResponseLatency:0.0000}ms (Command ID: {CommandId}, Trace ID: {TraceId})",
                           elapsedTime.TotalMilliseconds,
                           commandId,
                           traceId);

                return;
            }

            logger.Log(Configuration.PostExecutionLogLevel,
                       "Executed {TransportTypeName} command on {TransportRole} in {ResponseLatency:0.0000}ms (Command ID: {CommandId}, Trace ID: {TraceId})",
                       ctx.TransportType.Name,
                       Enum.GetName(ctx.TransportType.Role)?.ToLowerInvariant(),
                       elapsedTime.TotalMilliseconds,
                       commandId,
                       traceId);

            return;
        }

        if (ctx.TransportType.IsInProcess())
        {
            logger.Log(Configuration.PostExecutionLogLevel,
                       "Executed command and got response {ResponsePayload} in {ResponseLatency:0.0000}ms (Command ID: {CommandId}, Trace ID: {TraceId})",
                       Serialize(response, Configuration.JsonSerializerOptions),
                       elapsedTime.TotalMilliseconds,
                       commandId,
                       traceId);

            return;
        }

        logger.Log(Configuration.PostExecutionLogLevel,
                   "Executed {TransportTypeName} command on {TransportRole} and got response {ResponsePayload} in {ResponseLatency:0.0000}ms (Command ID: {CommandId}, Trace ID: {TraceId})",
                   ctx.TransportType.Name,
                   Enum.GetName(ctx.TransportType.Role)?.ToLowerInvariant(),
                   Serialize(response, Configuration.JsonSerializerOptions),
                   elapsedTime.TotalMilliseconds,
                   commandId,
                   traceId);
    }

    [SuppressMessage("Major Code Smell", "S2629:Logging templates should be constant", Justification = "The exception is already included as metadata, so we just want the message to include the full exception without adding it again as metadata.")]
    private void OnException(ILogger logger,
                             string commandId,
                             string traceId,
                             Exception exception,
                             StackTrace executionStackTrace,
                             TimeSpan elapsedTime,
                             CommandMiddlewareContext<TCommand, TResponse> ctx)
    {
        if (Configuration.ExceptionHook is { } exceptionHook)
        {
            var loggingExceptionContext = new LoggingCommandExceptionContext(logger,
                                                                             Configuration.ExceptionLogLevel,
                                                                             commandId,
                                                                             traceId,
                                                                             ctx.Command,
                                                                             exception,
                                                                             executionStackTrace,
                                                                             elapsedTime,
                                                                             ctx.ServiceProvider);

            if (!exceptionHook(loggingExceptionContext))
            {
                return;
            }
        }

        if (!logger.IsEnabled(Configuration.ExceptionLogLevel))
        {
            return;
        }

        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem (the exception is already included as metadata, so we just want the message
        // to include the full exception without adding it again as metadata)
        logger.Log(Configuration.ExceptionLogLevel,
                   exception,
                   $"An exception occurred while executing command after {{ResponseLatency:0.0000}}ms (Command ID: {{CommandId}}, Trace ID: {{TraceId}})\n{exception}\n{executionStackTrace}",
                   elapsedTime.TotalMilliseconds,
                   commandId,
                   traceId);
    }

    private ILogger GetLogger(CommandMiddlewareContext<TCommand, TResponse> ctx)
    {
        var loggerFactory = ctx.ServiceProvider.GetRequiredService<ILoggerFactory>();

        if (Configuration.LoggerNameFactory?.Invoke(ctx.Command) is { } loggerName)
        {
            return loggerFactory.CreateLogger(loggerName);
        }

        return loggerFactory.CreateLogger<TCommand>();
    }

    private string Serialize<T>(T value, JsonSerializerOptions? jsonSerializerOptions) => JsonSerializer.Serialize(value, jsonSerializerOptions);
}
