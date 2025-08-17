namespace Conqueror.Middleware.Polly.Messaging;

using global::Polly;

/// <summary>
///     The configuration options for <see cref="PollyMessageMiddleware{TMessage,TResponse}" />.
/// </summary>
/// <typeparam name="TMessage">The message type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public sealed class PollyMessageMiddlewareConfiguration<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    /// <summary>
    ///     The builder for the resilience pipeline to use to wrap the rest of the pipeline execution.
    /// </summary>
    public ResiliencePipelineBuilder<TResponse>? ResiliencePipelineBuilder { get; set; }
}
