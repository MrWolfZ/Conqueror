using System;
using Conqueror.CQS.Middleware.Logging;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="ICommandPipelineBuilder" /> to add, configure, or remove logging functionality.
/// </summary>
public static class ConquerorCqsMiddlewareLoggingCommandPipelineBuilderExtensions
{
    /// <summary>
    ///     Add logging functionality to a command pipeline. By default the following messages are logged:
    ///     <list type="bullet">
    ///         <item>Before the command is executed (including the JSON-serialized command payload, if any)</item>
    ///         <item>After the command was executed successfully (including the JSON-serialized response payload, if any)</item>
    ///         <item>If an exception gets thrown during command execution</item>
    ///     </list>
    /// </summary>
    /// <param name="pipeline">The command pipeline to add logging to</param>
    /// <param name="configure">
    ///     An optional delegate to configure the logging functionality (see <see cref="LoggingCommandMiddlewareConfiguration" />
    ///     for the full list of configuration options)
    /// </param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipelineBuilder UseLogging(this ICommandPipelineBuilder pipeline, Action<LoggingCommandMiddlewareConfiguration>? configure = null)
    {
        return pipeline.Use<LoggingCommandMiddleware, LoggingCommandMiddlewareConfiguration>(new())
                       .ConfigureLogging(configure ?? (_ => { }));
    }

    /// <summary>
    ///     Configure the logging middleware added to a command pipeline.
    /// </summary>
    /// <param name="pipeline">The command pipeline with the logging middleware to configure</param>
    /// <param name="configure">
    ///     The delegate for configuring the logging functionality (see <see cref="LoggingCommandMiddlewareConfiguration" />
    ///     for the full list of configuration options)
    /// </param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipelineBuilder ConfigureLogging(this ICommandPipelineBuilder pipeline, Action<LoggingCommandMiddlewareConfiguration> configure)
    {
        return pipeline.Configure<LoggingCommandMiddleware, LoggingCommandMiddlewareConfiguration>(configure);
    }

    /// <summary>
    ///     Remove the logging middleware from a command pipeline.
    /// </summary>
    /// <param name="pipeline">The command pipeline with the logging middleware to remove</param>
    /// <returns>The command pipeline</returns>
    public static ICommandPipelineBuilder WithoutLogging(this ICommandPipelineBuilder pipeline)
    {
        return pipeline.Without<LoggingCommandMiddleware, LoggingCommandMiddlewareConfiguration>();
    }
}
