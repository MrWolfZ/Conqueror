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
            var loggerFactory = ctx.ServiceProvider.GetRequiredService<ILoggerFactory>();
            ILogger logger;

            if (ctx.Configuration.LoggerNameFactory?.Invoke(ctx.Query) is { } loggerName)
            {
                logger = loggerFactory.CreateLogger(loggerName);
            }
            else
            {
                logger = loggerFactory.CreateLogger<TQuery>();
            }

            var sw = Stopwatch.StartNew();

            try
            {
                var shouldLogPreExecution = true;

                if (ctx.Configuration.PreExecutionHook is { } preExecutionHook)
                {
                    shouldLogPreExecution = preExecutionHook(new(logger, ctx.Configuration.PreExecutionLogLevel, ctx.Query, ctx.ServiceProvider));
                }

                // check if the log level is enabled so that we can skip the JSON serialization for performance
                if (shouldLogPreExecution && logger.IsEnabled(ctx.Configuration.PreExecutionLogLevel))
                {
                    if (ctx.Configuration.OmitJsonSerializedQueryPayload || !ctx.Query.GetType().GetProperties().Any())
                    {
                        logger.Log(ctx.Configuration.PreExecutionLogLevel, "Executing query");
                    }
                    else
                    {
                        logger.Log(ctx.Configuration.PreExecutionLogLevel, "Executing query with payload {QueryPayload}", Serialize(ctx.Query, ctx.Configuration.JsonSerializerOptions));
                    }
                }

                var response = await ctx.Next(ctx.Query, ctx.CancellationToken);

                var elapsedTime = sw.Elapsed;

                var shouldLogPostExecution = true;

                if (ctx.Configuration.PostExecutionHook is { } postExecutionHook)
                {
                    shouldLogPostExecution = postExecutionHook(new(logger, ctx.Configuration.PostExecutionLogLevel, ctx.Query, response, elapsedTime, ctx.ServiceProvider));
                }

                if (shouldLogPostExecution && logger.IsEnabled(ctx.Configuration.PostExecutionLogLevel))
                {
                    if (ctx.Configuration.OmitJsonSerializedResponsePayload)
                    {
                        logger.Log(ctx.Configuration.PostExecutionLogLevel, "Executed query in {ResponseLatency:0.0000}ms", elapsedTime.TotalMilliseconds);
                    }
                    else
                    {
                        logger.Log(ctx.Configuration.PostExecutionLogLevel,
                                   "Executed query and got response {ResponsePayload} in {ResponseLatency:0.0000}ms",
                                   Serialize(response, ctx.Configuration.JsonSerializerOptions),
                                   elapsedTime.TotalMilliseconds);
                    }
                }

                return response;
            }
            catch (Exception e)
            {
                var elapsedTime = sw.Elapsed;

                var shouldLogException = true;

                if (ctx.Configuration.ExceptionHook is { } exceptionHook)
                {
                    shouldLogException = exceptionHook(new(logger, ctx.Configuration.ExceptionLogLevel, ctx.Query, e, elapsedTime, ctx.ServiceProvider));
                }

                if (shouldLogException && logger.IsEnabled(ctx.Configuration.ExceptionLogLevel))
                {
                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem (the exception is already included as metadata, so we just want the message
                    // to include the full exception without adding it again as metadata)
                    logger.Log(ctx.Configuration.ExceptionLogLevel, e, $"An exception occurred while executing query after {{ResponseLatency:0.0000}}ms\n{e}", elapsedTime.TotalMilliseconds);
                }

                throw;
            }
        }

        private static string Serialize<T>(T value, JsonSerializerOptions? jsonSerializerOptions) => JsonSerializer.Serialize(value, jsonSerializerOptions);
    }
}
