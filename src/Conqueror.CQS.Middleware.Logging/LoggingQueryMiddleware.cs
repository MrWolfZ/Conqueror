using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging;

/// <summary>
///     A query middleware which adds logging functionality to a query pipeline. By default, the following messages are logged:
///     <list type="bullet">
///         <item>Before the query is executed (including the JSON-serialized query payload, if any)</item>
///         <item>After the query was executed successfully (including the JSON-serialized response payload)</item>
///         <item>If an exception gets thrown during query execution</item>
///     </list>
/// </summary>
public sealed class LoggingQueryMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
{
    public required LoggingQueryMiddlewareConfiguration Configuration { get; init; }

    /// <inheritdoc />
    public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        var logger = GetLogger(ctx);

        var queryId = ctx.ConquerorContext.GetQueryId() ?? "unknown";
        var traceId = ctx.ConquerorContext.GetTraceId();

        var sw = Stopwatch.StartNew();
        StackTrace? executionStackTrace = null;

        try
        {
            PreExecution(logger, queryId, traceId, ctx);

            executionStackTrace = new(3, true);
            var response = await ctx.Next(ctx.Query, ctx.CancellationToken).ConfigureAwait(false);

            PostExecution(logger, queryId, traceId, response, sw.Elapsed, ctx);

            return response;
        }
        catch (Exception e)
        {
            OnException(logger, queryId, traceId, e, executionStackTrace ?? new(true), sw.Elapsed, ctx);

            throw;
        }
    }

    private void PreExecution(ILogger logger,
                              string queryId,
                              string traceId,
                              QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        if (Configuration.PreExecutionHook is { } preExecutionHook)
        {
            var preExecutionContext = new LoggingQueryPreExecutionContext(logger, Configuration.PreExecutionLogLevel, queryId, traceId, ctx.Query, ctx.ServiceProvider);

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

        var hasPayload = ctx.Query.GetType().GetProperties().Any();
        var shouldOmitPayload = Configuration.OmitJsonSerializedQueryPayload;

        if (shouldOmitPayload || !hasPayload)
        {
            logger.Log(Configuration.PreExecutionLogLevel, "Executing query (Query ID: {QueryId}, Trace ID: {TraceId})", queryId, traceId);
            return;
        }

        logger.Log(Configuration.PreExecutionLogLevel,
                   "Executing query with payload {QueryPayload} (Query ID: {QueryId}, Trace ID: {TraceId})",
                   Serialize(ctx.Query, Configuration.JsonSerializerOptions),
                   queryId,
                   traceId);
    }

    private void PostExecution(ILogger logger,
                               string queryId,
                               string traceId,
                               object? response,
                               TimeSpan elapsedTime,
                               QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        if (Configuration.PostExecutionHook is { } postExecutionHook)
        {
            var postExecutionContext = new LoggingQueryPostExecutionContext(logger,
                                                                            Configuration.PostExecutionLogLevel,
                                                                            queryId,
                                                                            traceId,
                                                                            ctx.Query,
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

        if (shouldOmitPayload)
        {
            logger.Log(Configuration.PostExecutionLogLevel,
                       "Executed query in {ResponseLatency:0.0000}ms (Query ID: {QueryId}, Trace ID: {TraceId})",
                       elapsedTime.TotalMilliseconds,
                       queryId,
                       traceId);

            return;
        }

        logger.Log(Configuration.PostExecutionLogLevel,
                   "Executed query and got response {ResponsePayload} in {ResponseLatency:0.0000}ms (Query ID: {QueryId}, Trace ID: {TraceId})",
                   Serialize(response, Configuration.JsonSerializerOptions),
                   elapsedTime.TotalMilliseconds,
                   queryId,
                   traceId);
    }

    [SuppressMessage("Major Code Smell", "S2629:Logging templates should be constant", Justification = "The exception is already included as metadata, so we just want the message to include the full exception without adding it again as metadata.")]
    private void OnException(ILogger logger,
                             string queryId,
                             string traceId,
                             Exception exception,
                             StackTrace executionStackTrace,
                             TimeSpan elapsedTime,
                             QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        if (Configuration.ExceptionHook is { } exceptionHook)
        {
            var loggingExceptionContext = new LoggingQueryExceptionContext(logger,
                                                                           Configuration.ExceptionLogLevel,
                                                                           queryId,
                                                                           traceId,
                                                                           ctx.Query,
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
                   $"An exception occurred while executing query after {{ResponseLatency:0.0000}}ms (Query ID: {{QueryId}}, Trace ID: {{TraceId}})\n{exception}\n{executionStackTrace}",
                   elapsedTime.TotalMilliseconds,
                   queryId,
                   traceId);
    }

    private ILogger GetLogger(QueryMiddlewareContext<TQuery, TResponse> ctx)
    {
        var loggerFactory = ctx.ServiceProvider.GetRequiredService<ILoggerFactory>();

        if (Configuration.LoggerNameFactory?.Invoke(ctx.Query) is { } loggerName)
        {
            return loggerFactory.CreateLogger(loggerName);
        }

        return loggerFactory.CreateLogger<TQuery>();
    }

    private static string Serialize<T>(T value, JsonSerializerOptions? jsonSerializerOptions) => JsonSerializer.Serialize(value, jsonSerializerOptions);
}
