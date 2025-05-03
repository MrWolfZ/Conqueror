using Polly;

namespace Conqueror.Middleware.Polly.Signalling;

/// <summary>
///     The configuration options for <see cref="PollySignalMiddleware{TSignal}" />.
/// </summary>
public sealed class PollySignalMiddlewareConfiguration<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    /// <summary>
    ///     The builder for the resilience pipeline to use to wrap the rest of the pipeline execution.
    /// </summary>
    public ResiliencePipelineBuilder? ResiliencePipelineBuilder { get; set; }
}
