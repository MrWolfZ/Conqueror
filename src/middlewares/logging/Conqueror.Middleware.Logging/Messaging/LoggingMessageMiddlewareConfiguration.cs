using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Messaging;

/// <summary>
///     The configuration options for <see cref="LoggingMessageMiddleware{TMessage,TResponse}" />.
/// </summary>
public sealed class LoggingMessageMiddlewareConfiguration<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    internal LoggingMessageMiddlewareConfiguration(Type? handlerType)
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
    ///     The strategy to use for logging the message payload.
    ///     Defaults to <see cref="PayloadLoggingStrategy.MinimalJson" />.<br />
    ///     <br />
    ///     If <see cref="MessagePayloadLoggingStrategyFactory" /> is set,
    ///     this property is ignored.
    /// </summary>
    public PayloadLoggingStrategy MessagePayloadLoggingStrategy { get; set; } = PayloadLoggingStrategy.MinimalJson;

    /// <summary>
    ///     A factory method to determine the strategy to use for logging the message payload.
    ///     If this is not set, the <see cref="MessagePayloadLoggingStrategy" /> is used.
    /// </summary>
    public Func<TMessage, PayloadLoggingStrategy>? MessagePayloadLoggingStrategyFactory { get; set; }

    /// <summary>
    ///     The strategy to use for logging the response payload (if any).
    ///     Defaults to <see cref="PayloadLoggingStrategy.MinimalJson" />.<br />
    ///     <br />
    ///     If <see cref="MessagePayloadLoggingStrategyFactory" /> is set,
    ///     this property is ignored.
    /// </summary>
    public PayloadLoggingStrategy ResponsePayloadLoggingStrategy { get; set; } = PayloadLoggingStrategy.MinimalJson;

    /// <summary>
    ///     A factory method to determine the strategy to use for logging the response payload.
    ///     If this is not set, the <see cref="ResponsePayloadLoggingStrategy" /> is used.
    /// </summary>
    public Func<TMessage, TResponse, PayloadLoggingStrategy>? ResponsePayloadLoggingStrategyFactory { get; set; }

    /// <summary>
    ///     Customize the logger category. The factory is passed the message.
    ///     By default, the logger will use the fully-qualified type name of
    ///     the message type.
    /// </summary>
    public Func<TMessage, string>? LoggerCategoryFactory { get; set; }

    /// <summary>
    ///     A hook that is called just before the pre-execution log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and using the <see cref="LoggingMessagePreExecutionContext{TMessage,TResponse}.Logger" />.
    /// </summary>
    public Func<LoggingMessagePreExecutionContext<TMessage, TResponse>, bool>? PreExecutionHook { get; set; }

    /// <summary>
    ///     A hook that is called just before the post-execution log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and using the <see cref="LoggingMessagePostExecutionContext{TMessage,TResponse}.Logger" />.
    /// </summary>
    public Func<LoggingMessagePostExecutionContext<TMessage, TResponse>, bool>? PostExecutionHook { get; set; }

    /// <summary>
    ///     A hook that is called just before the exception log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and using the <see cref="LoggingMessageExceptionContext{TMessage,TResponse}.Logger" />.<br />
    ///     <br />
    ///     Note that this hook does not influence what happens with the exception
    ///     itself. It will always be re-thrown by the middleware.
    /// </summary>
    public Func<LoggingMessageExceptionContext<TMessage, TResponse>, bool>? ExceptionHook { get; set; }

    internal Type? HandlerType { get; }
}

/// <summary>
///     The context passed to a <see cref="LoggingMessageMiddleware{TMessage,TResponse}" />'s
///     <see cref="LoggingMessageMiddlewareConfiguration{TMessage,TResponse}.PreExecutionHook" />.
/// </summary>
public sealed record LoggingMessagePreExecutionContext<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log the message
    ///     yourself.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    ///     The level at which the message will be logged.
    /// </summary>
    public required LogLevel LogLevel { get; init; }

    /// <summary>
    ///     The ID of the message that is being logged.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    ///     The transport type of the message execution.
    /// </summary>
    public required MessageTransportType TransportType { get; init; }

    /// <summary>
    ///     The message that is being logged.
    /// </summary>
    public required TMessage Message { get; init; }
}

/// <summary>
///     The context passed to a <see cref="LoggingMessageMiddleware{TMessage,TResponse}" />'s
///     <see cref="LoggingMessageMiddlewareConfiguration{TMessage,TResponse}.PostExecutionHook" />.
/// </summary>
public sealed record LoggingMessagePostExecutionContext<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log the message
    ///     yourself.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    ///     The level at which the message will be logged.
    /// </summary>
    public required LogLevel LogLevel { get; init; }

    /// <summary>
    ///     The ID of the message that is being logged.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    ///     The transport type of the message execution.
    /// </summary>
    public required MessageTransportType TransportType { get; init; }

    /// <summary>
    ///     The message that is being logged.
    /// </summary>
    public required TMessage Message { get; init; }

    /// <summary>
    ///     Whether the message has a response.
    /// </summary>
    public required bool HasResponse { get; init; }

    /// <summary>
    ///     The response that is being logged (this is set to an instance of
    ///     <see cref="UnitMessageResponse" /> if the message has no response;
    ///     check <see cref="HasResponse" /> if you need to distinguish between
    ///     the two cases).
    /// </summary>
    public required TResponse Response { get; init; }

    /// <summary>
    ///     The time which has elapsed while executing the message.
    /// </summary>
    public required TimeSpan ElapsedTime { get; init; }
}

/// <summary>
///     The context passed to a <see cref="LoggingMessageMiddleware{TMessage,TResponse}" />'s
///     <see cref="LoggingMessageMiddlewareConfiguration{TMessage,TResponse}.ExceptionHook" />.
/// </summary>
public sealed record LoggingMessageExceptionContext<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log the message
    ///     yourself.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    ///     The level at which the message will be logged.
    /// </summary>
    public required LogLevel LogLevel { get; init; }

    /// <summary>
    ///     The ID of the message that is being logged.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    ///     The transport type of the message execution.
    /// </summary>
    public required MessageTransportType TransportType { get; init; }

    /// <summary>
    ///     The message that is being logged.
    /// </summary>
    public required TMessage Message { get; init; }

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
    ///     The time which has elapsed while executing the message.
    /// </summary>
    public required TimeSpan ElapsedTime { get; init; }
}
