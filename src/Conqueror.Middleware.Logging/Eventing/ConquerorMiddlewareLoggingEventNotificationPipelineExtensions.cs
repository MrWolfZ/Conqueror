using System;
using Conqueror.Middleware.Logging.Eventing;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="IEventNotificationPipeline{TEventNotification}" /> to add, configure, or remove logging functionality.
/// </summary>
public static class ConquerorMiddlewareLoggingEventNotificationPipelineExtensions
{
    /// <summary>
    ///     Add logging functionality to a event notification pipeline. By default, the following messages are logged:
    ///     <list type="bullet">
    ///         <item>Before the event notification is executed (including the JSON-serialized payload, if any)</item>
    ///         <item>After the event notification was executed successfully</item>
    ///         <item>If an exception gets thrown during the event notification execution</item>
    ///     </list>
    /// </summary>
    /// <param name="pipeline">The event notification pipeline to add logging to</param>
    /// <param name="configure">
    ///     An optional delegate to configure the logging functionality (see
    ///     <see cref="LoggingEventNotificationMiddlewareConfiguration{TEventNotification}" />
    ///     for the full list of configuration options)
    /// </param>
    /// <returns>The event notification pipeline</returns>
    public static IEventNotificationPipeline<TEventNotification> UseLogging<TEventNotification>(this IEventNotificationPipeline<TEventNotification> pipeline,
                                                                                                Action<LoggingEventNotificationMiddlewareConfiguration<TEventNotification>>? configure = null)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        var configuration = new LoggingEventNotificationMiddlewareConfiguration<TEventNotification>();
        configure?.Invoke(configuration);
        return pipeline.Use(new LoggingEventNotificationMiddleware<TEventNotification> { Configuration = configuration });
    }

    /// <summary>
    ///     Configure the logging middleware added to a event notification pipeline.
    /// </summary>
    /// <param name="pipeline">The event notification pipeline with the logging middleware to configure</param>
    /// <param name="configure">
    ///     The delegate for configuring the logging functionality (see <see cref="LoggingEventNotificationMiddlewareConfiguration{TEventNotification}" />
    ///     for the full list of configuration options)
    /// </param>
    /// <returns>The event notification pipeline</returns>
    public static IEventNotificationPipeline<TEventNotification> ConfigureLogging<TEventNotification>(this IEventNotificationPipeline<TEventNotification> pipeline,
                                                                                                      Action<LoggingEventNotificationMiddlewareConfiguration<TEventNotification>> configure)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return pipeline.Configure<LoggingEventNotificationMiddleware<TEventNotification>>(m => configure(m.Configuration));
    }

    /// <summary>
    ///     Remove the logging middleware from a event notification pipeline.
    /// </summary>
    /// <param name="pipeline">The event notification pipeline with the logging middleware to remove</param>
    /// <returns>The event notification pipeline</returns>
    public static IEventNotificationPipeline<TEventNotification> WithoutLogging<TEventNotification>(this IEventNotificationPipeline<TEventNotification> pipeline)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return pipeline.Without<LoggingEventNotificationMiddleware<TEventNotification>>();
    }
}
