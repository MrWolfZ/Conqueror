using System;
using Conqueror.Middleware.Polly.Messaging;
using Polly;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

/// <summary>
///     Extension methods for <see cref="IMessagePipeline{TMessage,TResponse}" /> to add, configure, or remove Polly functionality.
/// </summary>
public static class PollyMessageMiddlewarePipelineExtensions
{
    /// <summary>
    ///     Wrap the execution of the rest of the message pipeline in a Polly <see cref="Polly.ResiliencePipeline{TResponse}" />.
    /// </summary>
    /// <param name="pipeline">The message pipeline to add the Polly middleware to</param>
    /// <param name="configureResiliencePipeline">
    ///     Callback for configuring the resilience pipeline to use to wrap the rest of the pipeline
    ///     execution
    /// </param>
    /// <returns>The message pipeline</returns>
    public static IMessagePipeline<TMessage, TResponse> UsePolly<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline,
        Func<ResiliencePipelineBuilder<TResponse>, ResiliencePipelineBuilder<TResponse>>? configureResiliencePipeline = null)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var configuration = new PollyMessageMiddlewareConfiguration<TMessage, TResponse>();

        if (configureResiliencePipeline is not null)
        {
            configuration.ResiliencePipelineBuilder = configureResiliencePipeline(new());
        }

        return pipeline.Use(new PollyMessageMiddleware<TMessage, TResponse> { Configuration = configuration });
    }

    /// <summary>
    ///     Set the <see cref="Polly.ResiliencePipeline{TResponse}" /> to use in the Polly middleware.
    /// </summary>
    /// <param name="pipeline">The message pipeline with the Polly middleware to configure</param>
    /// <param name="configureResiliencePipeline">
    ///     Callback for configuring the resilience pipeline to use to wrap the rest of the pipeline
    ///     execution
    /// </param>
    /// <returns>The message pipeline</returns>
    public static IMessagePipeline<TMessage, TResponse> ConfigurePolly<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline,
        Func<ResiliencePipelineBuilder<TResponse>, ResiliencePipelineBuilder<TResponse>> configureResiliencePipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Configure<PollyMessageMiddleware<TMessage, TResponse>>(m =>
        {
            m.Configuration.ResiliencePipelineBuilder ??= new();
            m.Configuration.ResiliencePipelineBuilder = configureResiliencePipeline(m.Configuration.ResiliencePipelineBuilder);
        });
    }

    /// <summary>
    ///     Remove the Polly middleware from a message pipeline.
    /// </summary>
    /// <param name="pipeline">The message pipeline with the Polly middleware to remove</param>
    /// <returns>The message pipeline</returns>
    public static IMessagePipeline<TMessage, TResponse> WithoutPolly<TMessage, TResponse>(
        this IMessagePipeline<TMessage, TResponse> pipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return pipeline.Without<PollyMessageMiddleware<TMessage, TResponse>>();
    }
}
