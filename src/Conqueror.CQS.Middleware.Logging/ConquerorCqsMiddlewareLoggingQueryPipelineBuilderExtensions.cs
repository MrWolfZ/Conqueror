using System;
using Conqueror.CQS.Middleware.Logging;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="IQueryPipelineBuilder" /> to add, configure, or remove logging functionality.
/// </summary>
public static class ConquerorCqsMiddlewareLoggingQueryPipelineBuilderExtensions
{
    /// <summary>
    ///     Add logging functionality to a query pipeline. By default the following messages are logged:
    ///     <list type="bullet">
    ///         <item>Before the query is executed (including the JSON-serialized query payload, if any)</item>
    ///         <item>After the query was executed successfully (including the JSON-serialized response payload)</item>
    ///         <item>If an exception gets thrown during query execution</item>
    ///     </list>
    /// </summary>
    /// <param name="pipeline">The query pipeline to add logging to</param>
    /// <param name="configure">
    ///     An optional delegate to configure the logging functionality (see <see cref="LoggingQueryMiddlewareConfiguration" />
    ///     for the full list of configuration options)
    /// </param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipelineBuilder UseLogging(this IQueryPipelineBuilder pipeline, Action<LoggingQueryMiddlewareConfiguration>? configure = null)
    {
        return pipeline.Use<LoggingQueryMiddleware, LoggingQueryMiddlewareConfiguration>(new())
                       .ConfigureLogging(configure ?? (_ => { }));
    }

    /// <summary>
    ///     Configure the logging middleware added to a query pipeline.
    /// </summary>
    /// <param name="pipeline">The query pipeline with the logging middleware to configure</param>
    /// <param name="configure">
    ///     The delegate for configuring the logging functionality (see <see cref="LoggingQueryMiddlewareConfiguration" />
    ///     for the full list of configuration options)
    /// </param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipelineBuilder ConfigureLogging(this IQueryPipelineBuilder pipeline, Action<LoggingQueryMiddlewareConfiguration> configure)
    {
        return pipeline.Configure<LoggingQueryMiddleware, LoggingQueryMiddlewareConfiguration>(configure);
    }

    /// <summary>
    ///     Remove the logging middleware from a query pipeline.
    /// </summary>
    /// <param name="pipeline">The query pipeline with the logging middleware to remove</param>
    /// <returns>The query pipeline</returns>
    public static IQueryPipelineBuilder WithoutLogging(this IQueryPipelineBuilder pipeline)
    {
        return pipeline.Without<LoggingQueryMiddleware, LoggingQueryMiddlewareConfiguration>();
    }
}
