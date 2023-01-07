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
    ///     A command middleware which adds logging functionality to a command pipeline. By default the following messages are logged:
    ///     <list type="bullet">
    ///         <item>Before the command is executed (including the JSON-serialized command payload, if any)</item>
    ///         <item>After the command was executed successfully (including the JSON-serialized response payload, if any)</item>
    ///         <item>If an exception gets thrown during command execution</item>
    ///     </list>
    /// </summary>
    public sealed class CommandLoggingMiddleware : ICommandMiddleware<CommandLoggingMiddlewareConfiguration>
    {
        /// <inheritdoc />
        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, CommandLoggingMiddlewareConfiguration> ctx)
            where TCommand : class
        {
            var loggerFactory = ctx.ServiceProvider.GetRequiredService<ILoggerFactory>();
            ILogger logger;

            if (ctx.Configuration.LoggerNameFactory?.Invoke(ctx.Command) is { } loggerName)
            {
                logger = loggerFactory.CreateLogger(loggerName);
            }
            else
            {
                logger = loggerFactory.CreateLogger<TCommand>();
            }

            var sw = Stopwatch.StartNew();

            try
            {
                var shouldLogPreExecution = true;

                if (ctx.Configuration.PreExecutionHook is { } preExecutionHook)
                {
                    shouldLogPreExecution = preExecutionHook(new(logger, ctx.Configuration.PreExecutionLogLevel, ctx.Command, ctx.ServiceProvider));
                }

                // check if the log level is enabled so that we can skip the JSON serialization for performance
                if (shouldLogPreExecution && logger.IsEnabled(ctx.Configuration.PreExecutionLogLevel))
                {
                    if (ctx.Configuration.OmitJsonSerializedCommandPayload || !ctx.Command.GetType().GetProperties().Any())
                    {
                        logger.Log(ctx.Configuration.PreExecutionLogLevel, "Executing command");
                    }
                    else
                    {
                        logger.Log(ctx.Configuration.PreExecutionLogLevel, "Executing command with payload {CommandPayload}", Serialize(ctx.Command, ctx.Configuration.JsonSerializerOptions));
                    }
                }

                var response = await ctx.Next(ctx.Command, ctx.CancellationToken);

                var elapsedTime = sw.Elapsed;

                var shouldLogPostExecution = true;

                if (ctx.Configuration.PostExecutionHook is { } postExecutionHook)
                {
                    shouldLogPostExecution = postExecutionHook(new(logger, ctx.Configuration.PostExecutionLogLevel, ctx.Command, response, elapsedTime, ctx.ServiceProvider));
                }

                if (shouldLogPostExecution && logger.IsEnabled(ctx.Configuration.PostExecutionLogLevel))
                {
                    if (ctx.Configuration.OmitJsonSerializedResponsePayload || ctx.HasUnitResponse)
                    {
                        logger.Log(ctx.Configuration.PostExecutionLogLevel, "Executed command in {ResponseLatency:0.0000}ms", elapsedTime.TotalMilliseconds);
                    }
                    else
                    {
                        logger.Log(ctx.Configuration.PostExecutionLogLevel,
                                   "Executed command and got response {ResponsePayload} in {ResponseLatency:0.0000}ms",
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
                    shouldLogException = exceptionHook(new(logger, ctx.Configuration.ExceptionLogLevel, ctx.Command, e, elapsedTime, ctx.ServiceProvider));
                }

                if (shouldLogException && logger.IsEnabled(ctx.Configuration.ExceptionLogLevel))
                {
                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem (the exception is already included as metadata, so we just want the message
                    // to include the full exception without adding it again as metadata)
                    logger.Log(ctx.Configuration.ExceptionLogLevel, e, $"An exception occurred while executing command after {{ResponseLatency:0.0000}}ms\n{e}", elapsedTime.TotalMilliseconds);
                }

                throw;
            }
        }

        private static string Serialize<T>(T value, JsonSerializerOptions? jsonSerializerOptions) => JsonSerializer.Serialize(value, jsonSerializerOptions);
    }
}
