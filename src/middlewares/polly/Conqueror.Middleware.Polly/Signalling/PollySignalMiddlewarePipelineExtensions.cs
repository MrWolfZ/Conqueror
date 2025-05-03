using System;
using Conqueror.Middleware.Polly.Signalling;
using Polly;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="ISignalPipeline{TSignal}" /> to add, configure, or remove Polly functionality.
/// </summary>
public static class PollySignalMiddlewarePipelineExtensions
{
    /// <summary>
    ///     Wrap the execution of the rest of the signal pipeline in a Polly <see cref="Polly.ResiliencePipeline{TResponse}" />.
    /// </summary>
    /// <param name="pipeline">The signal pipeline to add the Polly middleware to</param>
    /// <param name="configureResiliencePipeline">
    ///     Callback for configuring the resilience pipeline to use to wrap the rest of the pipeline
    ///     execution
    /// </param>
    /// <returns>The signal pipeline</returns>
    public static ISignalPipeline<TSignal> UsePolly<TSignal>(
        this ISignalPipeline<TSignal> pipeline,
        Func<ResiliencePipelineBuilder, ResiliencePipelineBuilder>? configureResiliencePipeline = null)
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
    /// <param name="pipeline">The signal pipeline with the Polly middleware to configure</param>
    /// <param name="configureResiliencePipeline">
    ///     Callback for configuring the resilience pipeline to use to wrap the rest of the pipeline
    ///     execution
    /// </param>
    /// <returns>The signal pipeline</returns>
    public static ISignalPipeline<TSignal> ConfigurePolly<TSignal>(
        this ISignalPipeline<TSignal> pipeline,
        Func<ResiliencePipelineBuilder, ResiliencePipelineBuilder> configureResiliencePipeline)
        where TSignal : class, ISignal<TSignal>
    {
        return pipeline.Configure<PollySignalMiddleware<TSignal>>(m =>
        {
            m.Configuration.ResiliencePipelineBuilder ??= new();
            m.Configuration.ResiliencePipelineBuilder = configureResiliencePipeline(m.Configuration.ResiliencePipelineBuilder);
        });
    }

    /// <summary>
    ///     Remove the Polly middleware from a signal pipeline.
    /// </summary>
    /// <param name="pipeline">The signal pipeline with the Polly middleware to remove</param>
    /// <returns>The signal pipeline</returns>
    public static ISignalPipeline<TSignal> WithoutPolly<TSignal>(
        this ISignalPipeline<TSignal> pipeline)
        where TSignal : class, ISignal<TSignal>
    {
        return pipeline.Without<PollySignalMiddleware<TSignal>>();
    }
}
