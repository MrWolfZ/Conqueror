namespace Conqueror.Middleware.Polly.Signalling;

using global::Polly;

/// <summary>
///     The configuration options for <see cref="PollySignalMiddleware{TSignal}" />.
/// </summary>
/// <typeparam name="TSignal">The signal type</typeparam>
public sealed class PollySignalMiddlewareConfiguration<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    /// <summary>
    ///     The builder for the resilience pipeline to use to wrap the rest of the pipeline execution.
    /// </summary>
    public ResiliencePipelineBuilder? ResiliencePipelineBuilder { get; set; }
}
