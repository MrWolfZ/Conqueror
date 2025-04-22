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
}

/// <summary>
///     The context passed to a <see cref="LoggingSignalMiddleware{TSignal}" />'s
///     <see cref="LoggingSignalMiddlewareConfiguration{TSignal}.PreExecutionHook" />.
/// </summary>
public sealed record LoggingSignalPreExecutionContext<TSignal>
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
    ///     The signal that is being logged.
    /// </summary>
    public required TSignal Signal { get; init; }
}

/// <summary>
///     The context passed to a <see cref="LoggingSignalMiddleware{TSignal}" />'s
///     <see cref="LoggingSignalMiddlewareConfiguration{TSignal}.PostExecutionHook" />.
/// </summary>
public sealed record LoggingSignalPostExecutionContext<TSignal>
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
public sealed record LoggingSignalExceptionContext<TSignal>
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
    ///     handler.
    /// </summary>
    public required StackTrace ExecutionStackTrace { get; init; }

    /// <summary>
    ///     The time which has elapsed while executing the signal.
    /// </summary>
    public required TimeSpan ElapsedTime { get; init; }
}
