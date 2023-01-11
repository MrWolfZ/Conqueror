using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging
{
    /// <summary>
    ///     A query middleware which adds logging functionality to a query pipeline. By default the following messages are logged:
    ///     <list type="bullet">
    ///         <item>Before the query is executed (including the JSON-serialized query payload, if any)</item>
    ///         <item>After the query was executed successfully (including the JSON-serialized response payload)</item>
    ///         <item>If an exception gets thrown during query execution</item>
    ///     </list>
    /// </summary>
    public sealed class QueryLoggingMiddleware : IQueryMiddleware<QueryLoggingMiddlewareConfiguration>
    {
        /// <inheritdoc />
        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, QueryLoggingMiddlewareConfiguration> ctx)
            where TQuery : class
        {
            var logger = GetLogger(ctx);

            var queryId = ctx.ServiceProvider.GetRequiredService<IQueryContextAccessor>().QueryContext!.QueryId;
            var traceId = ctx.ServiceProvider.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.TraceId;

            var sw = Stopwatch.StartNew();

            try
            {
                PreExecution(logger, queryId, traceId, ctx);

                var response = await ctx.Next(ctx.Query, ctx.CancellationToken).ConfigureAwait(false);

                PostExecution(logger, queryId, traceId, response, sw.Elapsed, ctx);

                return response;
            }
            catch (Exception e)
            {
                OnException(logger, queryId, traceId, e, sw.Elapsed, ctx);

                throw;
            }
        }

        private static void PreExecution<TQuery, TResponse>(ILogger logger,
                                                            string queryId,
                                                            string traceId,
                                                            QueryMiddlewareContext<TQuery, TResponse, QueryLoggingMiddlewareConfiguration> ctx)
            where TQuery : class
        {
            if (ctx.Configuration.PreExecutionHook is { } preExecutionHook)
            {
                var preExecutionContext = new QueryLoggingPreExecutionContext(logger, ctx.Configuration.PreExecutionLogLevel, queryId, traceId, ctx.Query, ctx.ServiceProvider);

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

            var hasPayload = ctx.Query.GetType().GetProperties().Any();
            var shouldOmitPayload = ctx.Configuration.OmitJsonSerializedQueryPayload;

            if (shouldOmitPayload || !hasPayload)
            {
                logger.Log(ctx.Configuration.PreExecutionLogLevel, "Executing query (Query ID: {QueryId}, Trace ID: {TraceId})", queryId, traceId);
                return;
            }

            logger.Log(ctx.Configuration.PreExecutionLogLevel,
                       "Executing query with payload {QueryPayload} (Query ID: {QueryId}, Trace ID: {TraceId})",
                       Serialize(ctx.Query, ctx.Configuration.JsonSerializerOptions),
                       queryId,
                       traceId);
        }

        private static void PostExecution<TQuery, TResponse>(ILogger logger,
                                                             string queryId,
                                                             string traceId,
                                                             object? response,
                                                             TimeSpan elapsedTime,
                                                             QueryMiddlewareContext<TQuery, TResponse, QueryLoggingMiddlewareConfiguration> ctx)
            where TQuery : class
        {
            if (ctx.Configuration.PostExecutionHook is { } postExecutionHook)
            {
                var postExecutionContext = new QueryLoggingPostExecutionContext(logger,
                                                                                ctx.Configuration.PostExecutionLogLevel,
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
            if (!logger.IsEnabled(ctx.Configuration.PostExecutionLogLevel))
            {
                return;
            }

            var shouldOmitPayload = ctx.Configuration.OmitJsonSerializedResponsePayload;

            if (shouldOmitPayload)
            {
                logger.Log(ctx.Configuration.PostExecutionLogLevel,
                           "Executed query in {ResponseLatency:0.0000}ms (Query ID: {QueryId}, Trace ID: {TraceId})",
                           elapsedTime.TotalMilliseconds,
                           queryId,
                           traceId);

                return;
            }

            logger.Log(ctx.Configuration.PostExecutionLogLevel,
                       "Executed query and got response {ResponsePayload} in {ResponseLatency:0.0000}ms (Query ID: {QueryId}, Trace ID: {TraceId})",
                       Serialize(response, ctx.Configuration.JsonSerializerOptions),
                       elapsedTime.TotalMilliseconds,
                       queryId,
                       traceId);
        }

        private static void OnException<TQuery, TResponse>(ILogger logger,
                                                           string queryId,
                                                           string traceId,
                                                           Exception exception,
                                                           TimeSpan elapsedTime,
                                                           QueryMiddlewareContext<TQuery, TResponse, QueryLoggingMiddlewareConfiguration> ctx)
            where TQuery : class
        {
            if (ctx.Configuration.ExceptionHook is { } exceptionHook)
            {
                var loggingExceptionContext = new QueryLoggingExceptionContext(logger,
                                                                               ctx.Configuration.ExceptionLogLevel,
                                                                               queryId,
                                                                               traceId,
                                                                               ctx.Query,
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
                       $"An exception occurred while executing query after {{ResponseLatency:0.0000}}ms (Query ID: {{QueryId}}, Trace ID: {{TraceId}})\n{exception}",
                       elapsedTime.TotalMilliseconds,
                       queryId,
                       traceId);
        }

        private static ILogger GetLogger<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, QueryLoggingMiddlewareConfiguration> ctx)
            where TQuery : class
        {
            var loggerFactory = ctx.ServiceProvider.GetRequiredService<ILoggerFactory>();

            if (ctx.Configuration.LoggerNameFactory?.Invoke(ctx.Query) is { } loggerName)
            {
                return loggerFactory.CreateLogger(loggerName);
            }

            return loggerFactory.CreateLogger<TQuery>();
        }

        private static string Serialize<T>(T value, JsonSerializerOptions? jsonSerializerOptions) => JsonSerializer.Serialize(value, jsonSerializerOptions);
    }
}
