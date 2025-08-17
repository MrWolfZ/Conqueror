#pragma warning disable IDE0130 // Namespaces don't match folder structure - we want these extensions to be accessible from client code without an extra import

namespace Conqueror;

using Middleware.Logging.Messaging;

/// <summary>
///     Extension methods for <see cref="IMessagePipeline{TMessage,TResponse}" /> to add, configure, or remove logging
///     functionality.
/// </summary>
public static class LoggingMessageMiddlewarePipelineExtensions
{
    /// <summary>
    ///     Add logging functionality to a message pipeline. By default, the following messages are logged:
    ///     <list type="bullet">
    ///         <item>Before the message is executed (including the JSON-serialized message payload, if any)</item>
    ///         <item>After the message was executed successfully (including the JSON-serialized response payload, if any)</item>
    ///         <item>If an exception gets thrown during message execution</item>
    ///     </list>
    /// </summary>
    /// <typeparam name="TMessage">the message type</typeparam>
    /// <typeparam name="TResponse">the response type</typeparam>
    /// <param name="pipeline">The message pipeline to add logging to</param>
    /// <param name="configure">
    ///     An optional delegate to configure the logging functionality (see
    ///     <see cref="LoggingMessageMiddlewareConfiguration{TMessage,TResponse}" />
    ///     for the full list of configuration options)
    /// </param>
    /// <returns>The message pipeline</returns>
    public static IMessagePipeline<TMessage, TResponse> UseLogging<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline,
        Action<LoggingMessageMiddlewareConfiguration<TMessage, TResponse>>? configure = null
    )
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var configuration = new LoggingMessageMiddlewareConfiguration<TMessage, TResponse>(pipeline.HandlerType);
        configure?.Invoke(configuration);

        return pipeline.Use(new LoggingMessageMiddleware<TMessage, TResponse> { Configuration = configuration });
    }

    /// <summary>
    ///     Configure the logging middleware added to a message pipeline.
    /// </summary>
    /// <typeparam name="TMessage">the message type</typeparam>
    /// <typeparam name="TResponse">the response type</typeparam>
    /// <param name="pipeline">The message pipeline with the logging middleware to configure</param>
    /// <param name="configure">
    ///     The delegate for configuring the logging functionality (see
    ///     <see cref="LoggingMessageMiddlewareConfiguration{TMessage,TResponse}" />
    ///     for the full list of configuration options)
    /// </param>
    /// <returns>The message pipeline</returns>
    public static IMessagePipeline<TMessage, TResponse> ConfigureLogging<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline,
        Action<LoggingMessageMiddlewareConfiguration<TMessage, TResponse>> configure
    )
        where TMessage : class, IMessage<TMessage, TResponse> =>
        pipeline.Configure<LoggingMessageMiddleware<TMessage, TResponse>>(m => configure(m.Configuration));

    /// <summary>
    ///     Remove the logging middleware from a message pipeline.
    /// </summary>
    /// <typeparam name="TMessage">the message type</typeparam>
    /// <typeparam name="TResponse">the response type</typeparam>
    /// <param name="pipeline">The message pipeline with the logging middleware to remove</param>
    /// <returns>The message pipeline</returns>
    public static IMessagePipeline<TMessage, TResponse> WithoutLogging<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline
    )
        where TMessage : class, IMessage<TMessage, TResponse> =>
        pipeline.Without<LoggingMessageMiddleware<TMessage, TResponse>>();
}
