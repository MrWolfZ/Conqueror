using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Signalling;

/// <summary>
///     An signal middleware which adds logging functionality to a pipeline. By default, the following entries are logged:
///     <list type="bullet">
///         <item>Before the signal is executed (including the JSON-serialized payload, if any)</item>
///         <item>After the signal was executed successfully</item>
///         <item>If an exception gets thrown during the signal execution</item>
///     </list>
/// </summary>
/// <typeparam name="TSignal">the type of the signal</typeparam>
internal sealed partial class LoggingSignalMiddleware<TSignal> : ISignalMiddleware<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    public required LoggingSignalMiddlewareConfiguration<TSignal> Configuration { get; init; }

    /// <inheritdoc />
    public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
    {
        var logger = GetLogger(ctx);

        // the signal ID should always be set, so we could also throw in the unexpected case where
        // it would not be set (e.g. because someone is calling this method outside a normal Conqueror
        // execution like during misguided attempt at unit testing); but since this is logging logic it
        // should be as robust as possible to not cause failures that are not due to the actual business
        // logic, and therefore we just fall back to a safe value
        var signalId = ctx.ConquerorContext.GetSignalId() ?? "unknown";
        var traceId = ctx.ConquerorContext.GetTraceId();

        var sw = LoggingStopwatch.StartNew();
        StackTrace? executionStackTrace = null;

        try
        {
            PreExecution(logger, signalId, traceId, ctx);

            // we are aware that capturing the current stack trace like this has a performance impact, but
            // we believe that the trade-off between performance and debuggability is worth it here; if this
            // becomes an issue in the future, we can easily add a configuration option to disable this behavior
            // selectively
            executionStackTrace = new(skipFrames: 1, fNeedFileInfo: true);

            await ctx.Next(ctx.Signal, ctx.CancellationToken).ConfigureAwait(false);

            PostExecution(logger, signalId, traceId, sw.Elapsed, ctx);
        }
        catch (Exception e)
        {
            executionStackTrace ??= new(skipFrames: 1, fNeedFileInfo: true);
            OnException(logger, signalId, traceId, e, executionStackTrace, sw.Elapsed, ctx);

            throw;
        }
    }

    private void PreExecution(ILogger logger,
                              string signalId,
                              string traceId,
                              SignalMiddlewareContext<TSignal> ctx)
    {
        if (Configuration.PreExecutionHook is { } preExecutionHook)
        {
            try
            {
                var preExecutionContext = new LoggingSignalPreExecutionContext<TSignal>
                {
                    Logger = logger,
                    LogLevel = Configuration.PreExecutionLogLevel,
                    SignalId = signalId,
                    TraceId = traceId,
                    Signal = ctx.Signal,
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

        var hasPayload = TSignal.EmptyInstance is null;
        var shouldOmitPayload = Configuration.PayloadLoggingStrategy == PayloadLoggingStrategy.Omit;

        if (shouldOmitPayload || !hasPayload)
        {
            if (ctx.TransportType.IsInProcess() && ctx.TransportType.Role == SignalTransportRole.Receiver)
            {
                logger.LogSignalWithoutPayload(Configuration.PreExecutionLogLevel, signalId, traceId);
                return;
            }

            logger.LogSignalWithoutPayloadForTransport(Configuration.PreExecutionLogLevel,
                                                       ctx.TransportType.Name,
                                                       GetTransportRoleName(ctx.TransportType.Role),
                                                       signalId,
                                                       traceId);
            return;
        }

        if (ctx.TransportType.IsInProcess() && ctx.TransportType.Role == SignalTransportRole.Receiver)
        {
            if (Configuration.PayloadLoggingStrategy == PayloadLoggingStrategy.IndentedJson)
            {
                logger.LogSignalWithPayloadAsIndentedJson(Configuration.PreExecutionLogLevel,
                                                          Serialize(ctx.Signal, Configuration.PayloadLoggingStrategy),
                                                          signalId,
                                                          traceId);

                return;
            }

            logger.LogSignal(Configuration.PreExecutionLogLevel,
                             Serialize(ctx.Signal, Configuration.PayloadLoggingStrategy),
                             signalId,
                             traceId);

            return;
        }

        if (Configuration.PayloadLoggingStrategy == PayloadLoggingStrategy.IndentedJson)
        {
            logger.LogSignalWithPayloadAsIndentedJsonForTransport(Configuration.PreExecutionLogLevel,
                                                                  ctx.TransportType.Name,
                                                                  GetTransportRoleName(ctx.TransportType.Role),
                                                                  Serialize(ctx.Signal, Configuration.PayloadLoggingStrategy),
                                                                  signalId,
                                                                  traceId);

            return;
        }

        logger.LogSignalForTransport(Configuration.PreExecutionLogLevel,
                                     ctx.TransportType.Name,
                                     GetTransportRoleName(ctx.TransportType.Role),
                                     Serialize(ctx.Signal, Configuration.PayloadLoggingStrategy),
                                     signalId,
                                     traceId);
    }

    private void PostExecution(ILogger logger,
                               string signalId,
                               string traceId,
                               TimeSpan elapsedTime,
                               SignalMiddlewareContext<TSignal> ctx)
    {
        if (Configuration.PostExecutionHook is { } postExecutionHook)
        {
            try
            {
                var postExecutionContext = new LoggingSignalPostExecutionContext<TSignal>
                {
                    Logger = logger,
                    LogLevel = Configuration.PostExecutionLogLevel,
                    SignalId = signalId,
                    TraceId = traceId,
                    Signal = ctx.Signal,
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

        if (ctx.TransportType.IsInProcess() && ctx.TransportType.Role == SignalTransportRole.Receiver)
        {
            logger.LogSignalHandled(Configuration.PostExecutionLogLevel,
                                    elapsedTime.TotalMilliseconds,
                                    signalId,
                                    traceId);

            return;
        }

        logger.LogSignalHandledForTransport(Configuration.PostExecutionLogLevel,
                                            ctx.TransportType.Name,
                                            GetTransportRoleName(ctx.TransportType.Role),
                                            elapsedTime.TotalMilliseconds,
                                            signalId,
                                            traceId);
    }

    private void OnException(ILogger logger,
                             string signalId,
                             string traceId,
                             Exception exception,
                             StackTrace executionStackTrace,
                             TimeSpan elapsedTime,
                             SignalMiddlewareContext<TSignal> ctx)
    {
        if (Configuration.ExceptionHook is { } exceptionHook)
        {
            try
            {
                var loggingExceptionContext = new LoggingSignalExceptionContext<TSignal>
                {
                    Logger = logger,
                    LogLevel = Configuration.ExceptionLogLevel,
                    SignalId = signalId,
                    TraceId = traceId,
                    Signal = ctx.Signal,
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

        if (ctx.TransportType.IsInProcess() && ctx.TransportType.Role == SignalTransportRole.Receiver)
        {
            logger.LogSignalException(Configuration.ExceptionLogLevel,
                                      exceptionToLog,
                                      elapsedTime.TotalMilliseconds,
                                      signalId,
                                      traceId);

            return;
        }

        logger.LogSignalExceptionForTransport(Configuration.ExceptionLogLevel,
                                              exceptionToLog,
                                              ctx.TransportType.Name,
                                              GetTransportRoleName(ctx.TransportType.Role),
                                              elapsedTime.TotalMilliseconds,
                                              signalId,
                                              traceId);
    }

    private ILogger GetLogger(SignalMiddlewareContext<TSignal> ctx)
    {
        var loggerFactory = ctx.ServiceProvider.GetRequiredService<ILoggerFactory>();

        if (Configuration.LoggerCategoryFactory?.Invoke(ctx.Signal) is { } loggerName)
        {
            return loggerFactory.CreateLogger(loggerName);
        }

        return loggerFactory.CreateLogger(ctx.Signal.GetType());
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
                                  Justification = "we explicitly fail in AOT scenarios without a serializer context on the signal type")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
                                  Justification = "we explicitly fail in AOT scenarios without a serializer context on the signal type")]
    private static object? Serialize<T>(T value, PayloadLoggingStrategy payloadLoggingStrategy)
    {
        if (payloadLoggingStrategy is PayloadLoggingStrategy.Omit or PayloadLoggingStrategy.Raw)
        {
            return value;
        }

        if (!JsonSerializer.IsReflectionEnabledByDefault && TSignal.JsonSerializerContext is null)
        {
            throw new InvalidOperationException($"when running with AOT the '{typeof(TSignal)}.{nameof(TSignal.JsonSerializerContext)}' property cannot be null");
        }

        var jsonSerializerOptions = TSignal.JsonSerializerContext switch
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

    private static string GetTransportRoleName(SignalTransportRole transportRole) => transportRole switch
    {
        SignalTransportRole.Publisher => "publisher",
        SignalTransportRole.Receiver => "receiver",
        _ => throw new ArgumentOutOfRangeException(nameof(transportRole), transportRole, null),
    };

    [LoggerMessage(
        EventName = "conqueror-signal-logging-hook-exception",
        Message = "An exception occurred while executing logging hook")]
    private static partial void LogHookException(ILogger logger,
                                                 LogLevel logLevel,
                                                 Exception exception);
}
