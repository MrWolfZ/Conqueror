#pragma warning disable IDE0130 // Namespaces don't match folder structure - we want these extensions to be accessible from client registration code without an extra import

namespace Conqueror;

using Middleware.Polly.Signalling;
using Polly;

/// <summary>
///     Extension methods for <see cref="ISignalPipeline{TSignal}" /> to add, configure, or remove Polly functionality.
/// </summary>
public static class PollySignalMiddlewarePipelineExtensions
{
    /// <summary>
    ///     Wrap the execution of the rest of the signal pipeline in a Polly <see cref="Polly.ResiliencePipeline{TResponse}" />
    ///     .
    /// </summary>
    /// <typeparam name="TSignal">The signal type</typeparam>
    /// <param name="pipeline">The signal pipeline to add the Polly middleware to</param>
    /// <param name="configureResiliencePipeline">
    ///     Callback for configuring the resilience pipeline to use to wrap the rest of the pipeline
    ///     execution
    /// </param>
    /// <returns>The signal pipeline</returns>
    public static ISignalPipeline<TSignal> UsePolly<TSignal>(
        this ISignalPipeline<TSignal> pipeline,
        Func<ResiliencePipelineBuilder, ResiliencePipelineBuilder>? configureResiliencePipeline = null
    )
        where TSignal : class, ISignal<TSignal>
    {
        var configuration = new PollySignalMiddlewareConfiguration<TSignal>();

        if (configureResiliencePipeline is not null)
        {
            configuration.ResiliencePipelineBuilder = configureResiliencePipeline(new());
        }

        return pipeline.Use(new PollySignalMiddleware<TSignal> { Configuration = configuration });
    }

    /// <summary>
    ///     Set the <see cref="Polly.ResiliencePipeline{TResponse}" /> to use in the Polly middleware.
    /// </summary>
    /// <typeparam name="TSignal">The signal type</typeparam>
    /// <param name="pipeline">The signal pipeline with the Polly middleware to configure</param>
    /// <param name="configureResiliencePipeline">
    ///     Callback for configuring the resilience pipeline to use to wrap the rest of the pipeline
    ///     execution
    /// </param>
    /// <returns>The signal pipeline</returns>
    public static ISignalPipeline<TSignal> ConfigurePolly<TSignal>(
        this ISignalPipeline<TSignal> pipeline,
        Func<ResiliencePipelineBuilder, ResiliencePipelineBuilder> configureResiliencePipeline
    )
        where TSignal : class, ISignal<TSignal>
    {
        return pipeline.Configure<PollySignalMiddleware<TSignal>>(m =>
        {
            m.Configuration.ResiliencePipelineBuilder ??= new ResiliencePipelineBuilder();
            m.Configuration.ResiliencePipelineBuilder = configureResiliencePipeline(
                m.Configuration.ResiliencePipelineBuilder
            );
        });
    }

    /// <summary>
    ///     Remove the Polly middleware from a signal pipeline.
    /// </summary>
    /// <typeparam name="TSignal">The signal type</typeparam>
    /// <param name="pipeline">The signal pipeline with the Polly middleware to remove</param>
    /// <returns>The signal pipeline</returns>
    public static ISignalPipeline<TSignal> WithoutPolly<TSignal>(this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal> => pipeline.Without<PollySignalMiddleware<TSignal>>();
}
