using Polly;

namespace Conqueror.Middleware.Polly.Messaging;

/// <summary>
///     The configuration options for <see cref="PollyMessageMiddleware{TMessage,TResponse}" />.
/// </summary>
public sealed class PollyMessageMiddlewareConfiguration<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    /// <summary>
    ///     The builder for the resilience pipeline to use to wrap the rest of the pipeline execution.
    /// </summary>
    public ResiliencePipelineBuilder<TResponse>? ResiliencePipelineBuilder { get; set; }
}
