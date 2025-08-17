#pragma warning disable IDE0130 // Namespaces don't match folder structure - we want these extensions to be accessible from client code without an extra import

namespace Conqueror;

using Middleware.Logging.Signalling;

/// <summary>
///     Extension methods for <see cref="ISignalPipeline{TSignal}" /> to add, configure, or remove logging functionality.
/// </summary>
public static class LoggingSignalMiddlewarePipelineExtensions
{
    /// <summary>
    ///     Add logging functionality to a signal pipeline. By default, the following messages are logged:
    ///     <list type="bullet">
    ///         <item>Before the signal is executed (including the JSON-serialized payload, if any)</item>
    ///         <item>After the signal was executed successfully</item>
    ///         <item>If an exception gets thrown during the signal execution</item>
    ///     </list>
    /// </summary>
    /// <typeparam name="TSignal">The signal type</typeparam>
    /// <param name="pipeline">The signal pipeline to add logging to</param>
    /// <param name="configure">
    ///     An optional delegate to configure the logging functionality (see
    ///     <see cref="LoggingSignalMiddlewareConfiguration{TSignal}" />
    ///     for the full list of configuration options)
    /// </param>
    /// <returns>The signal pipeline</returns>
    public static ISignalPipeline<TSignal> UseLogging<TSignal>(
        this ISignalPipeline<TSignal> pipeline,
        Action<LoggingSignalMiddlewareConfiguration<TSignal>>? configure = null
    )
        where TSignal : class, ISignal<TSignal>
    {
        var configuration = new LoggingSignalMiddlewareConfiguration<TSignal>(pipeline.HandlerType);
        configure?.Invoke(configuration);

        return pipeline.Use(new LoggingSignalMiddleware<TSignal> { Configuration = configuration });
    }

    /// <summary>
    ///     Configure the logging middleware added to a signal pipeline.
    /// </summary>
    /// <typeparam name="TSignal">The signal type</typeparam>
    /// <param name="pipeline">The signal pipeline with the logging middleware to configure</param>
    /// <param name="configure">
    ///     The delegate for configuring the logging functionality (see
    ///     <see cref="LoggingSignalMiddlewareConfiguration{TSignal}" />
    ///     for the full list of configuration options)
    /// </param>
    /// <returns>The signal pipeline</returns>
    public static ISignalPipeline<TSignal> ConfigureLogging<TSignal>(
        this ISignalPipeline<TSignal> pipeline,
        Action<LoggingSignalMiddlewareConfiguration<TSignal>> configure
    )
        where TSignal : class, ISignal<TSignal> =>
        pipeline.Configure<LoggingSignalMiddleware<TSignal>>(m => configure(m.Configuration));

    /// <summary>
    ///     Remove the logging middleware from a signal pipeline.
    /// </summary>
    /// <typeparam name="TSignal">The signal type</typeparam>
    /// <param name="pipeline">The signal pipeline with the logging middleware to remove</param>
    /// <returns>The signal pipeline</returns>
    public static ISignalPipeline<TSignal> WithoutLogging<TSignal>(this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal> => pipeline.Without<LoggingSignalMiddleware<TSignal>>();
}
