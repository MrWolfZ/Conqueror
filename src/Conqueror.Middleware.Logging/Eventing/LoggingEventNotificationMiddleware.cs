using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Eventing;

/// <summary>
///     An event notification middleware which adds logging functionality to a pipeline. By default, the following entries are logged:
///     <list type="bullet">
///         <item>Before the event notification is executed (including the JSON-serialized payload, if any)</item>
///         <item>After the event notification was executed successfully</item>
///         <item>If an exception gets thrown during the event notification execution</item>
///     </list>
/// </summary>
/// <typeparam name="TEventNotification">the type of the event notification</typeparam>
internal sealed partial class LoggingEventNotificationMiddleware<TEventNotification> : IEventNotificationMiddleware<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    public required LoggingEventNotificationMiddlewareConfiguration<TEventNotification> Configuration { get; init; }

    /// <inheritdoc />
    public async Task Execute(EventNotificationMiddlewareContext<TEventNotification> ctx)
    {
        var logger = GetLogger(ctx);

        // the event notification ID should always be set, so we could also throw in the unexpected case where
        // it would not be set (e.g. because someone is calling this method outside a normal Conqueror
        // execution like during misguided attempt at unit testing); but since this is logging logic it
        // should be as robust as possible to not cause failures that are not due to the actual business
        // logic, and therefore we just fall back to a safe value
        var notificationId = ctx.ConquerorContext.GetEventNotificationId() ?? "unknown";
        var traceId = ctx.ConquerorContext.GetTraceId();

        var sw = LoggingStopwatch.StartNew();
        StackTrace? executionStackTrace = null;

        try
        {
            PreExecution(logger, notificationId, traceId, ctx);

            // we are aware that capturing the current stack trace like this has a performance impact, but
            // we believe that the trade-off between performance and debuggability is worth it here; if this
            // becomes an issue in the future, we can easily add a configuration option to disable this behavior
            // selectively
            executionStackTrace = new(skipFrames: 1, fNeedFileInfo: true);

            await ctx.Next(ctx.EventNotification, ctx.CancellationToken).ConfigureAwait(false);

            PostExecution(logger, notificationId, traceId, sw.Elapsed, ctx);
        }
        catch (Exception e)
        {
            executionStackTrace ??= new(skipFrames: 1, fNeedFileInfo: true);
            OnException(logger, notificationId, traceId, e, executionStackTrace, sw.Elapsed, ctx);

            throw;
        }
    }

    private void PreExecution(ILogger logger,
                              string notificationId,
                              string traceId,
                              EventNotificationMiddlewareContext<TEventNotification> ctx)
    {
        if (Configuration.PreExecutionHook is { } preExecutionHook)
        {
            try
            {
                var preExecutionContext = new LoggingEventNotificationPreExecutionContext<TEventNotification>
                {
                    Logger = logger,
                    LogLevel = Configuration.PreExecutionLogLevel,
                    EventNotificationId = notificationId,
                    TraceId = traceId,
                    EventNotification = ctx.EventNotification,
                };

                if (!preExecutionHook(preExecutionContext))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                // do not fail the execution just because logging failed
                LogHookException(logger, Configuration.ExceptionLogLevel, ex);
            }
        }

        // check if the log level is enabled so that we can skip the JSON serialization for performance
        if (!logger.IsEnabled(Configuration.PreExecutionLogLevel))
        {
            return;
        }

        var hasPayload = TEventNotification.EmptyInstance is null;
        var shouldOmitPayload = Configuration.PayloadLoggingStrategy == PayloadLoggingStrategy.Omit;

        if (shouldOmitPayload || !hasPayload)
        {
            if (ctx.TransportType.IsInProcess())
            {
                logger.LogEventNotificationWithoutPayload(Configuration.PreExecutionLogLevel, notificationId, traceId);
                return;
            }

            logger.LogEventNotificationWithoutPayloadForTransport(Configuration.PreExecutionLogLevel,
                                                                  ctx.TransportType.Name,
                                                                  GetTransportRoleName(ctx.TransportType.Role),
                                                                  notificationId,
                                                                  traceId);
            return;
        }

        if (ctx.TransportType.IsInProcess())
        {
            if (Configuration.PayloadLoggingStrategy == PayloadLoggingStrategy.IndentedJson)
            {
                logger.LogEventNotificationWithPayloadAsIndentedJson(Configuration.PreExecutionLogLevel,
                                                                     Serialize(ctx.EventNotification, Configuration.PayloadLoggingStrategy),
                                                                     notificationId,
                                                                     traceId);

                return;
            }

            logger.LogEventNotification(Configuration.PreExecutionLogLevel,
                                        Serialize(ctx.EventNotification, Configuration.PayloadLoggingStrategy),
                                        notificationId,
                                        traceId);

            return;
        }

        if (Configuration.PayloadLoggingStrategy == PayloadLoggingStrategy.IndentedJson)
        {
            logger.LogEventNotificationWithPayloadAsIndentedJsonForTransport(Configuration.PreExecutionLogLevel,
                                                                             ctx.TransportType.Name,
                                                                             GetTransportRoleName(ctx.TransportType.Role),
                                                                             Serialize(ctx.EventNotification, Configuration.PayloadLoggingStrategy),
                                                                             notificationId,
                                                                             traceId);

            return;
        }

        logger.LogEventNotificationForTransport(Configuration.PreExecutionLogLevel,
                                                ctx.TransportType.Name,
                                                GetTransportRoleName(ctx.TransportType.Role),
                                                Serialize(ctx.EventNotification, Configuration.PayloadLoggingStrategy),
                                                notificationId,
                                                traceId);
    }

    private void PostExecution(ILogger logger,
                               string notificationId,
                               string traceId,
                               TimeSpan elapsedTime,
                               EventNotificationMiddlewareContext<TEventNotification> ctx)
    {
        if (Configuration.PostExecutionHook is { } postExecutionHook)
        {
            try
            {
                var postExecutionContext = new LoggingEventNotificationPostExecutionContext<TEventNotification>
                {
                    Logger = logger,
                    LogLevel = Configuration.PostExecutionLogLevel,
                    EventNotificationId = notificationId,
                    TraceId = traceId,
                    EventNotification = ctx.EventNotification,
                    ElapsedTime = elapsedTime,
                };

                if (!postExecutionHook(postExecutionContext))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                // do not fail the execution just because logging failed
                LogHookException(logger, Configuration.ExceptionLogLevel, ex);
            }
        }

        // check if the log level is enabled so that we can skip the JSON serialization for performance
        if (!logger.IsEnabled(Configuration.PostExecutionLogLevel))
        {
            return;
        }

        if (ctx.TransportType.IsInProcess())
        {
            logger.LogEventNotificationHandled(Configuration.PostExecutionLogLevel,
                                               elapsedTime.TotalMilliseconds,
                                               notificationId,
                                               traceId);

            return;
        }

        logger.LogEventNotificationHandledForTransport(Configuration.PostExecutionLogLevel,
                                                       ctx.TransportType.Name,
                                                       GetTransportRoleName(ctx.TransportType.Role),
                                                       elapsedTime.TotalMilliseconds,
                                                       notificationId,
                                                       traceId);
    }

    private void OnException(ILogger logger,
                             string notificationId,
                             string traceId,
                             Exception exception,
                             StackTrace executionStackTrace,
                             TimeSpan elapsedTime,
                             EventNotificationMiddlewareContext<TEventNotification> ctx)
    {
        if (Configuration.ExceptionHook is { } exceptionHook)
        {
            try
            {
                var loggingExceptionContext = new LoggingEventNotificationExceptionContext<TEventNotification>
                {
                    Logger = logger,
                    LogLevel = Configuration.ExceptionLogLevel,
                    EventNotificationId = notificationId,
                    TraceId = traceId,
                    EventNotification = ctx.EventNotification,
                    Exception = exception,
                    ExecutionStackTrace = executionStackTrace,
                    ElapsedTime = elapsedTime,
                };

                if (!exceptionHook(loggingExceptionContext))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                // do not fail the execution just because logging failed
                LogHookException(logger, Configuration.ExceptionLogLevel, ex);
            }
        }

        if (!logger.IsEnabled(Configuration.ExceptionLogLevel))
        {
            return;
        }

        // because we are in the middle of a stack-unwind for the thrown Exception, the stack trace only
        // contains the frames between the location where it was thrown and the invocation of the middleware,
        // but it does not show the full stack trace from where this middleware was called; since that information
        // is important for debugging, we capture the original stack trace, and then wrap the exception in another
        // exception that contains the stack trace from the invocation of the middleware; an alternative might be
        // to do the logging asynchronously so that the unwind of the exception has finished, and it contains the
        // full stack trace, but that could introduce subtle race conditions, so we prefer the former approach
        var exceptionToLog = new WrappingException(exception, executionStackTrace.ToString());

        if (ctx.TransportType.IsInProcess())
        {
            logger.LogEventNotificationException(Configuration.ExceptionLogLevel,
                                                 exceptionToLog,
                                                 elapsedTime.TotalMilliseconds,
                                                 notificationId,
                                                 traceId);

            return;
        }

        logger.LogEventNotificationExceptionForTransport(Configuration.ExceptionLogLevel,
                                                         exceptionToLog,
                                                         ctx.TransportType.Name,
                                                         GetTransportRoleName(ctx.TransportType.Role),
                                                         elapsedTime.TotalMilliseconds,
                                                         notificationId,
                                                         traceId);
    }

    private ILogger GetLogger(EventNotificationMiddlewareContext<TEventNotification> ctx)
    {
        var loggerFactory = ctx.ServiceProvider.GetRequiredService<ILoggerFactory>();

        if (Configuration.LoggerCategoryFactory?.Invoke(ctx.EventNotification) is { } loggerName)
        {
            return loggerFactory.CreateLogger(loggerName);
        }

        return loggerFactory.CreateLogger(ctx.EventNotification.GetType());
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
                                  Justification = "we explicitly fail in AOT scenarios without a serializer context on the event notification type")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
                                  Justification = "we explicitly fail in AOT scenarios without a serializer context on the event notification type")]
    private static object? Serialize<T>(T value, PayloadLoggingStrategy payloadLoggingStrategy)
    {
        if (payloadLoggingStrategy is PayloadLoggingStrategy.Omit or PayloadLoggingStrategy.Raw)
        {
            return value;
        }

        if (!JsonSerializer.IsReflectionEnabledByDefault && TEventNotification.JsonSerializerContext is null)
        {
            throw new InvalidOperationException($"when running with AOT the '{typeof(TEventNotification)}.{nameof(TEventNotification.JsonSerializerContext)}' property cannot be null");
        }

        var jsonSerializerOptions = TEventNotification.JsonSerializerContext switch
        {
            null => JsonSerializerOptions.Default,
            var ctx => ctx.Options,
        };

        if (payloadLoggingStrategy == PayloadLoggingStrategy.IndentedJson)
        {
            if (jsonSerializerOptions.IsReadOnly)
            {
                jsonSerializerOptions = new(jsonSerializerOptions);
            }

            jsonSerializerOptions.WriteIndented = true;
        }

        return JsonSerializer.Serialize(value, value?.GetType() ?? typeof(T), jsonSerializerOptions);
    }

    private static string GetTransportRoleName(EventNotificationTransportRole transportRole) => transportRole switch
    {
        EventNotificationTransportRole.Publisher => "publisher",
        EventNotificationTransportRole.Receiver => "receiver",
        _ => throw new ArgumentOutOfRangeException(nameof(transportRole), transportRole, null),
    };

    [LoggerMessage(
        EventName = "conqueror-event-notification-logging-hook-exception",
        Message = "An exception occurred while executing logging hook")]
    private static partial void LogHookException(ILogger logger,
                                                 LogLevel logLevel,
                                                 Exception exception);
}
