using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Signalling;

/// <summary>
///     The configuration options for <see cref="LoggingSignalMiddleware{TSignal}" />.
/// </summary>
public sealed class LoggingSignalMiddlewareConfiguration<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    internal LoggingSignalMiddlewareConfiguration(Type? handlerType)
    {
        HandlerType = handlerType;
    }

    /// <summary>
    ///     The level at which the pre-execution log message is logged.
    ///     Defaults to <see cref="LogLevel.Information" />.
    /// </summary>
    public LogLevel PreExecutionLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    ///     The level at which the post-execution log message is logged.
    ///     Defaults to <see cref="LogLevel.Information" />.
    /// </summary>
    public LogLevel PostExecutionLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    ///     The level at which the exception log message is logged.
    ///     Defaults to <see cref="LogLevel.Error" />.
    /// </summary>
    public LogLevel ExceptionLogLevel { get; set; } = LogLevel.Error;

    /// <summary>
    ///     The strategy to use for logging the signal payload.
    ///     Defaults to <see cref="PayloadLoggingStrategy.MinimalJson" />.
    /// </summary>
    public PayloadLoggingStrategy PayloadLoggingStrategy { get; set; } = PayloadLoggingStrategy.MinimalJson;

    /// <summary>
    ///     A factory method to determine the strategy to use for logging the signal payload.
    ///     If this is not set, the <see cref="PayloadLoggingStrategy" /> is used.
    /// </summary>
    public Func<TSignal, PayloadLoggingStrategy>? PayloadLoggingStrategyFactory { get; set; }

    /// <summary>
    ///     Customize the logger category. The factory is passed the signal.
    ///     By default, the logger will use the fully-qualified type name of
    ///     the signal type.
    /// </summary>
    public Func<TSignal, string>? LoggerCategoryFactory { get; set; }

    /// <summary>
    ///     A hook that is called just before the pre-execution log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and using the <see cref="LoggingSignalPreExecutionContext{TSignal}.Logger" />.
    /// </summary>
    public Func<LoggingSignalPreExecutionContext<TSignal>, bool>? PreExecutionHook { get; set; }

    /// <summary>
    ///     A hook that is called just before the post-execution log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and using the <see cref="LoggingSignalPostExecutionContext{TSignal}.Logger" />.
    /// </summary>
    public Func<LoggingSignalPostExecutionContext<TSignal>, bool>? PostExecutionHook { get; set; }

    /// <summary>
    ///     A hook that is called just before the exception log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and using the <see cref="LoggingSignalExceptionContext{TSignal}.Logger" />.<br />
    ///     <br />
    ///     Note that this hook does not influence what happens with the exception
    ///     itself. It will always be re-thrown by the middleware.
    /// </summary>
    public Func<LoggingSignalExceptionContext<TSignal>, bool>? ExceptionHook { get; set; }

    /// <summary>
    ///     By default, the logging middleware will capture the calling stack trace on every execution
    ///     so that it can build an exception with the full stack trace when an exception is thrown.
    ///     Without this explicit capture the stack trace of the exception would only contain the stack
    ///     frames between the location of the exception and the middleware execution, so we would not
    ///     see where the call originated. However, while this is very convenient for debugging, it has
    ///     a significant impact on performance. We believe that in most cases the improved debuggability
    ///     is worth the trade-off for worse performance. However, for performance-critical applications
    ///     or specific handlers, you can use this property to disable the stack trace capture.
    /// </summary>
    public bool StackTraceCaptureIsDisabled { get; set; }

    internal Type? HandlerType { get; }
}

/// <summary>
///     The context passed to a <see cref="LoggingSignalMiddleware{TSignal}" />'s
///     <see cref="LoggingSignalMiddlewareConfiguration{TSignal}.PreExecutionHook" />.
/// </summary>
public readonly record struct LoggingSignalPreExecutionContext<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log the signal
    ///     yourself.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    ///     The level at which the log message will be logged.
    /// </summary>
    public required LogLevel LogLevel { get; init; }

    /// <summary>
    ///     The ID of the signal that is being logged.
    /// </summary>
    public required string SignalId { get; init; }

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    ///     The transport type of the signal execution.
    /// </summary>
    public required SignalTransportType TransportType { get; init; }

    /// <summary>
    ///     The signal that is being logged.
    /// </summary>
    public required TSignal Signal { get; init; }
}

/// <summary>
///     The context passed to a <see cref="LoggingSignalMiddleware{TSignal}" />'s
///     <see cref="LoggingSignalMiddlewareConfiguration{TSignal}.PostExecutionHook" />.
/// </summary>
public readonly record struct LoggingSignalPostExecutionContext<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log a message
    ///     yourself.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    ///     The level at which the log message will be logged.
    /// </summary>
    public required LogLevel LogLevel { get; init; }

    /// <summary>
    ///     The ID of the signal that is being logged.
    /// </summary>
    public required string SignalId { get; init; }

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    ///     The transport type of the signal execution.
    /// </summary>
    public required SignalTransportType TransportType { get; init; }

    /// <summary>
    ///     The signal that is being logged.
    /// </summary>
    public required TSignal Signal { get; init; }

    /// <summary>
    ///     The time which has elapsed while executing the signal.
    /// </summary>
    public required TimeSpan ElapsedTime { get; init; }
}

/// <summary>
///     The context passed to a <see cref="LoggingSignalMiddleware{TSignal}" />'s
///     <see cref="LoggingSignalMiddlewareConfiguration{TSignal}.ExceptionHook" />.
/// </summary>
public readonly record struct LoggingSignalExceptionContext<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log a message
    ///     yourself.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    ///     The level at which the log message will be logged.
    /// </summary>
    public required LogLevel LogLevel { get; init; }

    /// <summary>
    ///     The ID of the signal that is being logged.
    /// </summary>
    public required string SignalId { get; init; }

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    ///     The transport type of the signal execution.
    /// </summary>
    public required SignalTransportType TransportType { get; init; }

    /// <summary>
    ///     The signal that is being logged.
    /// </summary>
    public required TSignal Signal { get; init; }

    /// <summary>
    ///     The exception which occurred.
    /// </summary>
    public required Exception Exception { get; init; }

    /// <summary>
    ///     The stack trace of the current middleware execution. This property
    ///     can be combined with <see cref="Exception.StackTrace" /> to get the
    ///     full stack trace of the exception, since the exception's stack trace
    ///     only contains the stack frames from the middleware execution to the
    ///     handler. Is <c>null</c> when capturing of the stack trace is disabled.
    /// </summary>
    public required StackTrace? ExecutionStackTrace { get; init; }

    /// <summary>
    ///     The time which has elapsed while executing the signal.
    /// </summary>
    public required TimeSpan ElapsedTime { get; init; }
}
