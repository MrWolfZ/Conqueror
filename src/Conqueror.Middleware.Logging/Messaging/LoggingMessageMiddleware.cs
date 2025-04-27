using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Messaging;

/// <summary>
///     A message middleware which adds logging functionality to a message pipeline. By default, the following entries are logged:
///     <list type="bullet">
///         <item>Before the message is executed (including the JSON-serialized message payload, if any)</item>
///         <item>After the message was executed successfully (including the JSON-serialized response payload, if any)</item>
///         <item>If an exception gets thrown during message execution</item>
///     </list>
/// </summary>
/// <typeparam name="TMessage">the type of the message</typeparam>
/// <typeparam name="TResponse">the type of the response</typeparam>
internal sealed partial class LoggingMessageMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public required LoggingMessageMiddlewareConfiguration<TMessage, TResponse> Configuration { get; init; }

    /// <inheritdoc />
    public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        var logger = GetLogger(ctx);

        // the message ID should always be set, so we could also throw in the unexpected case where it
        // would not be set (e.g. because someone is calling this method outside a normal Conqueror
        // execution like during misguided attempt at unit testing); but since this is logging logic it
        // should be as robust as possible to not cause failures that are not due to the actual business
        // logic, and therefore we just fall back to a safe value
        var messageId = ctx.ConquerorContext.GetMessageId() ?? "unknown";
        var traceId = ctx.ConquerorContext.GetTraceId();

        var sw = LoggingStopwatch.StartNew();
        StackTrace? executionStackTrace = null;

        try
        {
            PreExecution(logger, messageId, traceId, ctx);

            // we are aware that capturing the current stack trace like this has a performance impact, but
            // we believe that the trade-off between performance and debuggability is worth it here; if this
            // becomes an issue in the future, we can easily add a configuration option to disable this behavior
            // selectively
            executionStackTrace = new(skipFrames: 1, fNeedFileInfo: true);

            var response = await ctx.Next(ctx.Message, ctx.CancellationToken).ConfigureAwait(false);

            PostExecution(logger, messageId, traceId, response, sw.Elapsed, ctx);

            return response;
        }
        catch (Exception e)
        {
            executionStackTrace ??= new(skipFrames: 1, fNeedFileInfo: true);
            OnException(logger, messageId, traceId, e, executionStackTrace, sw.Elapsed, ctx);

            throw;
        }
    }

    private void PreExecution(ILogger logger,
                              string messageId,
                              string traceId,
                              MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        if (Configuration.PreExecutionHook is { } preExecutionHook)
        {
            try
            {
                var preExecutionContext = new LoggingMessagePreExecutionContext<TMessage, TResponse>
                {
                    Logger = logger,
                    LogLevel = Configuration.PreExecutionLogLevel,
                    MessageId = messageId,
                    TraceId = traceId,
                    Message = ctx.Message,
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

        var hasPayload = TMessage.EmptyInstance is null;
        var shouldOmitPayload = Configuration.MessagePayloadLoggingStrategy == PayloadLoggingStrategy.Omit;

        if (shouldOmitPayload || !hasPayload)
        {
            if (ctx.TransportType.IsInProcess())
            {
                logger.LogMessageWithoutPayload(Configuration.PreExecutionLogLevel, messageId, traceId);
                return;
            }

            logger.LogMessageWithoutPayloadForTransport(Configuration.PreExecutionLogLevel,
                                                        ctx.TransportType.Name,
                                                        GetTransportRoleName(ctx.TransportType.Role),
                                                        messageId,
                                                        traceId);
            return;
        }

        if (ctx.TransportType.IsInProcess())
        {
            if (Configuration.MessagePayloadLoggingStrategy == PayloadLoggingStrategy.IndentedJson)
            {
                logger.LogMessageWithPayloadAsIndentedJson(Configuration.PreExecutionLogLevel,
                                                           Serialize(ctx.Message, Configuration.MessagePayloadLoggingStrategy),
                                                           messageId,
                                                           traceId);

                return;
            }

            logger.LogMessage(Configuration.PreExecutionLogLevel,
                              Serialize(ctx.Message, Configuration.MessagePayloadLoggingStrategy),
                              messageId,
                              traceId);

            return;
        }

        if (Configuration.MessagePayloadLoggingStrategy == PayloadLoggingStrategy.IndentedJson)
        {
            logger.LogMessageWithPayloadAsIndentedJsonForTransport(Configuration.PreExecutionLogLevel,
                                                                   ctx.TransportType.Name,
                                                                   GetTransportRoleName(ctx.TransportType.Role),
                                                                   Serialize(ctx.Message, Configuration.MessagePayloadLoggingStrategy),
                                                                   messageId,
                                                                   traceId);

            return;
        }

        logger.LogMessageForTransport(Configuration.PreExecutionLogLevel,
                                      ctx.TransportType.Name,
                                      GetTransportRoleName(ctx.TransportType.Role),
                                      Serialize(ctx.Message, Configuration.MessagePayloadLoggingStrategy),
                                      messageId,
                                      traceId);
    }

    private void PostExecution(ILogger logger,
                               string messageId,
                               string traceId,
                               TResponse response,
                               TimeSpan elapsedTime,
                               MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        if (Configuration.PostExecutionHook is { } postExecutionHook)
        {
            try
            {
                var postExecutionContext = new LoggingMessagePostExecutionContext<TMessage, TResponse>
                {
                    Logger = logger,
                    LogLevel = Configuration.PostExecutionLogLevel,
                    MessageId = messageId,
                    TraceId = traceId,
                    Message = ctx.Message,
                    Response = response,
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

        var shouldOmitPayload = Configuration.ResponsePayloadLoggingStrategy == PayloadLoggingStrategy.Omit;

        if (shouldOmitPayload || ctx.HasUnitResponse)
        {
            if (ctx.TransportType.IsInProcess())
            {
                logger.LogMessageResponseWithoutPayload(Configuration.PostExecutionLogLevel,
                                                        elapsedTime.TotalMilliseconds,
                                                        messageId,
                                                        traceId);

                return;
            }

            logger.LogMessageResponseWithoutPayloadForTransport(Configuration.PostExecutionLogLevel,
                                                                ctx.TransportType.Name,
                                                                GetTransportRoleName(ctx.TransportType.Role),
                                                                elapsedTime.TotalMilliseconds,
                                                                messageId,
                                                                traceId);

            return;
        }

        if (ctx.TransportType.IsInProcess())
        {
            if (Configuration.ResponsePayloadLoggingStrategy == PayloadLoggingStrategy.IndentedJson)
            {
                logger.LogMessageResponseWithPayloadAsIndentedJson(Configuration.PostExecutionLogLevel,
                                                                   Serialize(response, Configuration.ResponsePayloadLoggingStrategy),
                                                                   elapsedTime.TotalMilliseconds,
                                                                   messageId,
                                                                   traceId);

                return;
            }

            logger.LogMessageResponse(Configuration.PostExecutionLogLevel,
                                      Serialize(response, Configuration.ResponsePayloadLoggingStrategy),
                                      elapsedTime.TotalMilliseconds,
                                      messageId,
                                      traceId);

            return;
        }

        if (Configuration.ResponsePayloadLoggingStrategy == PayloadLoggingStrategy.IndentedJson)
        {
            logger.LogMessageResponseWithPayloadAsIndentedJsonForTransport(Configuration.PostExecutionLogLevel,
                                                                           ctx.TransportType.Name,
                                                                           GetTransportRoleName(ctx.TransportType.Role),
                                                                           Serialize(response, Configuration.ResponsePayloadLoggingStrategy),
                                                                           elapsedTime.TotalMilliseconds,
                                                                           messageId,
                                                                           traceId);

            return;
        }

        logger.LogMessageResponseForTransport(Configuration.PostExecutionLogLevel,
                                              ctx.TransportType.Name,
                                              GetTransportRoleName(ctx.TransportType.Role),
                                              Serialize(response, Configuration.ResponsePayloadLoggingStrategy),
                                              elapsedTime.TotalMilliseconds,
                                              messageId,
                                              traceId);
    }

    private void OnException(ILogger logger,
                             string messageId,
                             string traceId,
                             Exception exception,
                             StackTrace executionStackTrace,
                             TimeSpan elapsedTime,
                             MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        if (Configuration.ExceptionHook is { } exceptionHook)
        {
            try
            {
                var loggingExceptionContext = new LoggingMessageExceptionContext<TMessage, TResponse>
                {
                    Logger = logger,
                    LogLevel = Configuration.ExceptionLogLevel,
                    MessageId = messageId,
                    TraceId = traceId,
                    Message = ctx.Message,
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
            logger.LogMessageException(Configuration.ExceptionLogLevel,
                                       exceptionToLog,
                                       elapsedTime.TotalMilliseconds,
                                       messageId,
                                       traceId);

            return;
        }

        logger.LogMessageExceptionForTransport(Configuration.ExceptionLogLevel,
                                               exceptionToLog,
                                               ctx.TransportType.Name,
                                               GetTransportRoleName(ctx.TransportType.Role),
                                               elapsedTime.TotalMilliseconds,
                                               messageId,
                                               traceId);
    }

    private ILogger GetLogger(MessageMiddlewareContext<TMessage, TResponse> ctx)
    {
        var loggerFactory = ctx.ServiceProvider.GetRequiredService<ILoggerFactory>();

        if (Configuration.LoggerCategoryFactory?.Invoke(ctx.Message) is { } loggerName)
        {
            return loggerFactory.CreateLogger(loggerName);
        }

        return loggerFactory.CreateLogger(ctx.Message.GetType());
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
                                  Justification = "we explicitly fail in AOT scenarios without a serializer context on the message type")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
                                  Justification = "we explicitly fail in AOT scenarios without a serializer context on the message type")]
    private static object? Serialize<T>(T value, PayloadLoggingStrategy payloadLoggingStrategy)
    {
        if (payloadLoggingStrategy is PayloadLoggingStrategy.Omit or PayloadLoggingStrategy.Raw)
        {
            return value;
        }

        if (!JsonSerializer.IsReflectionEnabledByDefault && TMessage.JsonSerializerContext is null)
        {
            throw new InvalidOperationException($"when running with AOT the '{typeof(TMessage)}.{nameof(TMessage.JsonSerializerContext)}' property cannot be null");
        }

        var jsonSerializerOptions = TMessage.JsonSerializerContext switch
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

    private static string GetTransportRoleName(MessageTransportRole transportRole) => transportRole switch
    {
        MessageTransportRole.Sender => "sender",
        MessageTransportRole.Receiver => "receiver",
        _ => throw new ArgumentOutOfRangeException(nameof(transportRole), transportRole, null),
    };

    [LoggerMessage(
        EventName = "conqueror-message-logging-hook-exception",
        Message = "An exception occurred while executing logging hook")]
    private static partial void LogHookException(ILogger logger,
                                                 LogLevel logLevel,
                                                 Exception exception);
}
